using System;
using System.Diagnostics;

namespace MediaOrchestrator.Analytics
{
    /// <summary>
    ///     Provides metrics and health status for MediaOrchestrator runtime.
    /// </summary>
    public sealed class MediaOrchestratorMetrics
    {
        public static MediaOrchestratorMetrics Instance { get; } = new MediaOrchestratorMetrics();

        private MediaOrchestratorMetrics() { }

        /// <summary>
        ///     Current state of the ffmpeg circuit breaker.
        /// </summary>
        public string FfmpegCircuitBreakerState =>
            MediaOrchestrator.IsFfmpegOperationAllowed ? "Closed" : "Open";

        /// <summary>
        ///     Number of entries in the media analysis store cache.
        /// </summary>
        public int MediaAnalysisCacheCount
        {
            get
            {
                try
                {
                    var store = MediaOrchestrator.MediaAnalysisStore as Analytics.Stores.CachedMediaAnalysisStore;
                    return store?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("MediaAnalysisCacheCount failed: {0}", ex.Message);
                    return 0;
                }
            }
        }

        /// <summary>
        ///     Number of entries in the operation duration LRU cache.
        /// </summary>
        public int OperationDurationCacheCount =>
            MediaOrchestrator.GetOperationDurationCacheCount();

        /// <summary>
        ///     Current media info cache lifetime in minutes.
        /// </summary>
        public double MediaInfoCacheLifetimeMinutes =>
            MediaOrchestrator.MediaInfoCacheLifetime.TotalMinutes;

        /// <summary>
        ///     Whether analytics learning is enabled.
        /// </summary>
        public bool IsAnalyticsLearningEnabled =>
            MediaOrchestrator.MediaAnalysisLearningEnabled;

        /// <summary>
        ///     Whether compression is enabled for analytics store.
        /// </summary>
        public bool IsAnalyticsCompressionEnabled =>
            MediaOrchestrator.MediaAnalysisStoreCompressionEnabled;

        /// <summary>
        ///     Gets a snapshot of all metrics.
        /// </summary>
        public MediaOrchestratorMetricsSnapshot GetSnapshot()
        {
            return new MediaOrchestratorMetricsSnapshot
            {
                FfmpegCircuitBreakerState = FfmpegCircuitBreakerState,
                MediaAnalysisCacheCount = MediaAnalysisCacheCount,
                OperationDurationCacheCount = OperationDurationCacheCount,
                MediaInfoCacheLifetimeMinutes = MediaInfoCacheLifetimeMinutes,
                IsAnalyticsLearningEnabled = IsAnalyticsLearningEnabled,
                IsAnalyticsCompressionEnabled = IsAnalyticsCompressionEnabled,
                SnapshotTimeUtc = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    ///     Snapshot of MediaOrchestrator metrics at a point in time.
    /// </summary>
    public sealed class MediaOrchestratorMetricsSnapshot
    {
        public string FfmpegCircuitBreakerState { get; set; }
        public int MediaAnalysisCacheCount { get; set; }
        public int OperationDurationCacheCount { get; set; }
        public double MediaInfoCacheLifetimeMinutes { get; set; }
        public bool IsAnalyticsLearningEnabled { get; set; }
        public bool IsAnalyticsCompressionEnabled { get; set; }
        public DateTimeOffset SnapshotTimeUtc { get; set; }

        public override string ToString()
        {
            return $"[Metrics @{SnapshotTimeUtc:HH:mm:ss}] " +
                   $"CircuitBreaker={FfmpegCircuitBreakerState}, " +
                   $"Cache(Analysis={MediaAnalysisCacheCount}, Duration={OperationDurationCacheCount}), " +
                   $"Learning={IsAnalyticsLearningEnabled}, " +
                   $"Compression={IsAnalyticsCompressionEnabled}";
        }
    }

    /// <summary>
    ///     Provides health check functionality.
    /// </summary>
    public static class MediaOrchestratorHealth
    {
        /// <summary>
        ///     Checks if the system is healthy and ready for operations.
        /// </summary>
        public static HealthCheckResult Check()
        {
            var issues = new System.Collections.Generic.List<string>();

            try
            {
                if (!MediaOrchestrator.IsFfmpegOperationAllowed)
                {
                    issues.Add("Circuit breaker is Open - ffmpeg operations temporarily blocked");
                }

                var path = MediaOrchestrator.FFprobeExecutablePath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    issues.Add("FFprobe path not configured");
                }
                else if (!System.IO.File.Exists(path))
                {
                    issues.Add($"FFprobe not found at: {path}");
                }

                path = MediaOrchestrator.FFmpegExecutablePath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    issues.Add("FFmpeg path not configured");
                }
                else if (!System.IO.File.Exists(path))
                {
                    issues.Add($"FFmpeg not found at: {path}");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Health check error: {ex.Message}");
            }

            return new HealthCheckResult
            {
                IsHealthy = issues.Count == 0,
                Issues = issues,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    ///     Result of a health check operation.
    /// </summary>
    public sealed class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public System.Collections.Generic.List<string> Issues { get; set; } =
            new System.Collections.Generic.List<string>();
        public DateTimeOffset CheckedAt { get; set; }

        public override string ToString()
        {
            if (IsHealthy)
            {
                return "[Health] OK";
            }
            return "[Health] UNHEALTHY: " + string.Join("; ", Issues);
        }
    }
}