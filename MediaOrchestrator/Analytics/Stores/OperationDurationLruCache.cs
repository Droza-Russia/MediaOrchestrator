using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator.Analytics.Stores
{
    /// <summary>
    /// Thread-safe LRU cache for storing and retrieving adaptive operation timeout based on historical execution data.
    /// Uses existing analytics data to calculate optimal CancellationToken timeout for different operation types.
    /// </summary>
    internal sealed class OperationDurationLruCache
    {
        private sealed class DurationEntry
        {
            public string OperationKey { get; set; }
            public TimeSpan AverageDuration { get; set; }
            public int SampleCount { get; set; }
            public DateTimeOffset LastUpdatedUtc { get; set; }
            public double SuccessRate { get; set; }
            public DurationEntry Prev { get; set; }
            public DurationEntry Next { get; set; }
        }

        private readonly int _capacity;
        private readonly double _safetyFactor;
        private readonly ConcurrentDictionary<string, DurationEntry> _map;
        private DurationEntry _head;
        private DurationEntry _tail;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public OperationDurationLruCache(int capacity = 1000, double safetyFactor = 2.0)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            if (safetyFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(safetyFactor), "Safety factor must be greater than zero.");

            _capacity = capacity;
            _safetyFactor = safetyFactor;
            _map = new ConcurrentDictionary<string, DurationEntry>();
        }

        public bool TryGetAdaptiveTimeout(string operationKey, out TimeSpan timeout)
        {
            timeout = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(operationKey))
                return false;

            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_map.TryGetValue(operationKey, out var entry))
                {
                    if (entry.SampleCount > 0)
                    {
                        timeout = CalculateAdaptiveTimeout(entry.AverageDuration, entry.SuccessRate, entry.SampleCount);
                        _lock.EnterWriteLock();
                        try
                        {
                            MoveToHead(entry);
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private TimeSpan CalculateAdaptiveTimeout(TimeSpan averageDuration, double successRate, int sampleCount)
        {
            var baseTimeout = averageDuration;

            var confidenceFactor = Math.Min(1.0, sampleCount / 10.0);

            var safetyMargin = _safetyFactor;
            if (successRate < 0.8)
                safetyMargin *= 1.5;

            var calculatedTimeout = TimeSpan.FromMilliseconds(baseTimeout.TotalMilliseconds * safetyMargin);

            var minTimeout = TimeSpan.FromSeconds(30);
            var maxTimeout = TimeSpan.FromMinutes(30);

            return TimeSpan.FromMilliseconds(
                Math.Max(minTimeout.TotalMilliseconds,
                Math.Min(maxTimeout.TotalMilliseconds, calculatedTimeout.TotalMilliseconds)));
        }

        public void RecordDuration(string operationKey, TimeSpan actualDuration, bool succeeded)
        {
            if (string.IsNullOrWhiteSpace(operationKey))
                return;

            _lock.EnterWriteLock();
            try
            {
                var entry = _map.GetOrAdd(operationKey, key => new DurationEntry
                {
                    OperationKey = key,
                    AverageDuration = actualDuration,
                    SampleCount = 1,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                    SuccessRate = succeeded ? 1.0 : 0.0
                });

                var oldSampleCount = entry.SampleCount;
                var oldTotalDuration = entry.AverageDuration.TotalMilliseconds * oldSampleCount;
                var newSampleCount = oldSampleCount + 1;

                var newTotalDuration = oldTotalDuration + (succeeded ? actualDuration.TotalMilliseconds : actualDuration.TotalMilliseconds * 1.5);

                entry.AverageDuration = TimeSpan.FromMilliseconds(newTotalDuration / newSampleCount);
                entry.SampleCount = newSampleCount;
                entry.LastUpdatedUtc = DateTimeOffset.UtcNow;

                var oldSuccessCount = entry.SuccessRate * oldSampleCount;
                var newSuccessCount = succeeded ? oldSuccessCount + 1 : oldSuccessCount;
                entry.SuccessRate = newSuccessCount / newSampleCount;

                MoveToHead(entry);

                if (_map.Count > _capacity)
                {
                    RemoveTail();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public TimeSpan GetEstimatedTimeout(string operationKey, TimeSpan defaultTimeout)
        {
            if (TryGetAdaptiveTimeout(operationKey, out var adaptiveTimeout))
                return adaptiveTimeout;

            return defaultTimeout;
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _map.Clear();
                _head = null;
                _tail = null;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _map.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        private void AddToHead(DurationEntry entry)
        {
            entry.Next = _head;
            entry.Prev = null;

            if (_head != null)
                _head.Prev = entry;

            _head = entry;

            if (_tail == null)
                _tail = entry;
        }

        private void RemoveNode(DurationEntry entry)
        {
            if (entry.Prev != null)
                entry.Prev.Next = entry.Next;
            else
                _head = entry.Next;

            if (entry.Next != null)
                entry.Next.Prev = entry.Prev;
            else
                _tail = entry.Prev;

            entry.Prev = null;
            entry.Next = null;
        }

        private void MoveToHead(DurationEntry entry)
        {
            if (entry == _head)
                return;

            RemoveNode(entry);
            AddToHead(entry);
        }

        private void RemoveTail()
        {
            if (_tail != null)
            {
                var tailKey = _tail.OperationKey;
                RemoveNode(_tail);
                _map.TryRemove(tailKey, out _);
            }
        }

        public static string BuildOperationKey(
            string inputPath,
            string outputPath,
            ProcessingScenario scenario,
            MediaProcessingStrategy strategy,
            string videoCodec = null,
            string audioCodec = null,
            double? videoDurationSeconds = null,
            bool usesHardwareAcceleration = false)
        {
            var pathHash = (inputPath ?? string.Empty).GetHashCode() ^ (outputPath ?? string.Empty).GetHashCode();

            return string.Join("|", new[]
            {
                "op:" + pathHash.GetHashCode(),
                "scenario:" + scenario,
                "strategy:" + strategy,
                "vcodec:" + (videoCodec ?? "none"),
                "acodec:" + (audioCodec ?? "none"),
                "duration:" + (videoDurationSeconds?.ToString("F1") ?? "unknown"),
                "hw:" + usesHardwareAcceleration
            });
        }

        public IDictionary<string, TimeSpan> GetAllEstimatedTimeouts()
        {
            _lock.EnterReadLock();
            try
            {
                var result = new Dictionary<string, TimeSpan>();
                var current = _head;
                while (current != null)
                {
                    if (current.SampleCount > 0)
                    {
                        result[current.OperationKey] = CalculateAdaptiveTimeout(
                            current.AverageDuration,
                            current.SuccessRate,
                            current.SampleCount);
                    }
                    current = current.Next;
                }
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}