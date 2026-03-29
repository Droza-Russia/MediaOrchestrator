using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator.Analytics.Stores
{
    internal sealed class CachedMediaAnalysisStore : IMediaAnalysisStore, IDisposable
    {
        private sealed class CacheEntry
        {
            public MediaAnalysisRecord Record { get; set; }

            public bool Dirty { get; set; }
        }

        private readonly IMediaAnalysisStore _persistentStore;
        private readonly TimeSpan _flushDelay;
        private readonly LruCache<string, CacheEntry> _cache;
        private readonly SemaphoreSlim _flushGate = new SemaphoreSlim(1, 1);
        private readonly object _scheduleSync = new object();
        private volatile bool _isDisposed;

        private Task _scheduledFlushTask;

        public CachedMediaAnalysisStore(IMediaAnalysisStore persistentStore, TimeSpan? flushDelay = null, int? cacheCapacity = null, TimeSpan? cacheTtl = null)
        {
            _persistentStore = persistentStore ?? throw new ArgumentNullException(nameof(persistentStore));
            _flushDelay = flushDelay ?? TimeSpan.FromSeconds(2);
            _cache = new LruCache<string, CacheEntry>(cacheCapacity ?? 1000, cacheTtl);
        }

        public async Task<MediaAnalysisRecord> GetAsync(string analysisKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(analysisKey))
            {
                return null;
            }

            if (_cache.TryGet(analysisKey, out var cached))
            {
                return cached.Record;
            }

            var record = await _persistentStore.GetAsync(analysisKey, cancellationToken).ConfigureAwait(false);
            if (record != null)
            {
                _cache.Put(analysisKey, new CacheEntry
                {
                    Record = record,
                    Dirty = false
                });
            }

            return record;
        }

        public async Task<IReadOnlyCollection<MediaAnalysisRecord>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var persisted = await _persistentStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var merged = new Dictionary<string, MediaAnalysisRecord>(StringComparer.Ordinal);

            foreach (var record in persisted)
            {
                if (record != null && !string.IsNullOrWhiteSpace(record.AnalysisKey))
                {
                    merged[record.AnalysisKey] = record;
                }
            }

            foreach (var pair in _cache.GetAll())
            {
                if (pair.Value?.Record != null && !string.IsNullOrWhiteSpace(pair.Key))
                {
                    merged[pair.Key] = pair.Value.Record;
                }
            }

            return merged.Values.ToList();
        }

        public Task SaveAsync(MediaAnalysisRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.AnalysisKey))
            {
                return Task.CompletedTask;
            }

            _cache.Put(record.AnalysisKey, new CacheEntry
            {
                Record = record,
                Dirty = true
            });

            ScheduleLazyFlush();
            return Task.CompletedTask;
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _cache.Clear();
            await _persistentStore.ClearAsync(cancellationToken).ConfigureAwait(false);
        }

        internal async Task FlushPendingAsync(CancellationToken cancellationToken = default)
        {
            bool acquired = false;
            try
            {
                acquired = await _flushGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!acquired)
                {
                    return;
                }

                var dirtyRecords = new List<MediaAnalysisRecord>();
                foreach (var pair in _cache.GetAll())
                {
                    if (pair.Value?.Dirty == true && pair.Value?.Record != null)
                    {
                        dirtyRecords.Add(pair.Value.Record);
                    }
                }

                foreach (var record in dirtyRecords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _persistentStore.SaveAsync(record, cancellationToken).ConfigureAwait(false);
                    if (_cache.TryGet(record.AnalysisKey, out var cachedEntry))
                    {
                        cachedEntry.Dirty = false;
                        _cache.Put(record.AnalysisKey, cachedEntry);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Trace.TraceWarning("FlushPendingAsync cancelled");
            }
            catch (Exception ex)
            {
                Trace.TraceError("FlushPendingAsync failed: {0}", ex.Message);
            }
            finally
            {
                if (acquired)
                {
                    _flushGate.Release();
                }
            }
        }

        private void ScheduleLazyFlush()
        {
            lock (_scheduleSync)
            {
                if (_scheduledFlushTask != null && !_scheduledFlushTask.IsCompleted)
                {
                    return;
                }

                _scheduledFlushTask = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_flushDelay).ConfigureAwait(false);
                        await FlushDirtyEntriesAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Scheduled flush failed (ignored): {0}", ex.Message);
                    }
                });
            }
        }

        private async Task FlushDirtyEntriesAsync(CancellationToken cancellationToken)
        {
            bool acquired = false;
            try
            {
                acquired = await _flushGate.WaitAsync(0, cancellationToken).ConfigureAwait(false);
                if (!acquired)
                {
                    return;
                }

                var dirtyRecords = new List<MediaAnalysisRecord>();
                foreach (var pair in _cache.GetAll())
                {
                    if (pair.Value?.Dirty == true && pair.Value?.Record != null)
                    {
                        dirtyRecords.Add(pair.Value.Record);
                    }
                }

                foreach (var record in dirtyRecords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _persistentStore.SaveAsync(record, cancellationToken).ConfigureAwait(false);
                    if (_cache.TryGet(record.AnalysisKey, out var cachedEntry))
                    {
                        cachedEntry.Dirty = false;
                        _cache.Put(record.AnalysisKey, cachedEntry);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Trace.TraceWarning("CachedMediaAnalysisStore flush cancelled");
            }
            catch (Exception ex)
            {
                Trace.TraceError("CachedMediaAnalysisStore flush failed: {0}", ex.Message);
            }
            finally
            {
                if (acquired)
                {
                    _flushGate.Release();
                }
            }
        }

        public int Count => _cache.Count;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _flushGate.Dispose();

            if (_persistentStore is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
