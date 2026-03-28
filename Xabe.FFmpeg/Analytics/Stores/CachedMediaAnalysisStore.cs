using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator.Analytics.Stores
{
    internal sealed class CachedMediaAnalysisStore : IMediaAnalysisStore
    {
        private sealed class CacheEntry
        {
            public MediaAnalysisRecord Record { get; set; }

            public bool Dirty { get; set; }
        }

        private readonly IMediaAnalysisStore _persistentStore;
        private readonly TimeSpan _flushDelay;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);
        private readonly SemaphoreSlim _flushGate = new SemaphoreSlim(1, 1);
        private readonly object _scheduleSync = new object();

        private Task _scheduledFlushTask;

        public CachedMediaAnalysisStore(IMediaAnalysisStore persistentStore, TimeSpan? flushDelay = null)
        {
            _persistentStore = persistentStore ?? throw new ArgumentNullException(nameof(persistentStore));
            _flushDelay = flushDelay ?? TimeSpan.FromSeconds(2);
        }

        public async Task<MediaAnalysisRecord> GetAsync(string analysisKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(analysisKey))
            {
                return null;
            }

            if (_cache.TryGetValue(analysisKey, out var cached))
            {
                return cached.Record;
            }

            var record = await _persistentStore.GetAsync(analysisKey, cancellationToken).ConfigureAwait(false);
            if (record != null)
            {
                _cache[analysisKey] = new CacheEntry
                {
                    Record = record,
                    Dirty = false
                };
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

            foreach (var pair in _cache)
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

            _cache.AddOrUpdate(
                record.AnalysisKey,
                _ => new CacheEntry
                {
                    Record = record,
                    Dirty = true
                },
                (_, existing) =>
                {
                    existing.Record = record;
                    existing.Dirty = true;
                    return existing;
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
            await FlushDirtyEntriesAsync(cancellationToken).ConfigureAwait(false);
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
                    catch
                    {
                        // Lazy persistence must not break foreground analytics flow.
                    }
                });
            }
        }

        private async Task FlushDirtyEntriesAsync(CancellationToken cancellationToken)
        {
            await _flushGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                List<MediaAnalysisRecord> dirtyRecords = _cache
                    .Where(pair => pair.Value.Dirty && pair.Value.Record != null)
                    .Select(pair => pair.Value.Record)
                    .ToList();

                foreach (var record in dirtyRecords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _persistentStore.SaveAsync(record, cancellationToken).ConfigureAwait(false);
                    if (_cache.TryGetValue(record.AnalysisKey, out var entry))
                    {
                        entry.Dirty = false;
                    }
                }
            }
            finally
            {
                _flushGate.Release();
            }
        }
    }
}
