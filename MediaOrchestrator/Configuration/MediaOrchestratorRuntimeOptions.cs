using System;

namespace MediaOrchestrator.Configuration
{
    /// <summary>
    ///     Runtime limits for MediaOrchestrator hardening and diagnostics.
    /// </summary>
    public sealed class MediaOrchestratorRuntimeOptions
    {
        private const int DefaultMaxProcessOutputLogLines = 512;
        private const int DefaultMaxAnalyticsHashCacheSize = 10000;

        /// <summary>
        ///     Maximum number of stderr lines retained per process for diagnostics.
        /// </summary>
        public int MaxProcessOutputLogLines { get; set; } = DefaultMaxProcessOutputLogLines;

        /// <summary>
        ///     Maximum number of cached analysis-key hashes kept in memory.
        /// </summary>
        public int MaxAnalyticsHashCacheSize { get; set; } = DefaultMaxAnalyticsHashCacheSize;

        internal static MediaOrchestratorRuntimeOptions CreateDefault()
        {
            return new MediaOrchestratorRuntimeOptions
            {
                MaxProcessOutputLogLines = ResolveInt32("MEDIA_ORCHESTRATOR_MAX_PROCESS_OUTPUT_LOG_LINES", DefaultMaxProcessOutputLogLines),
                MaxAnalyticsHashCacheSize = ResolveInt32("MEDIA_ORCHESTRATOR_MAX_ANALYTICS_HASH_CACHE_SIZE", DefaultMaxAnalyticsHashCacheSize)
            };
        }

        internal MediaOrchestratorRuntimeOptions Clone()
        {
            return new MediaOrchestratorRuntimeOptions
            {
                MaxProcessOutputLogLines = MaxProcessOutputLogLines,
                MaxAnalyticsHashCacheSize = MaxAnalyticsHashCacheSize
            };
        }

        internal void Normalize()
        {
            if (MaxProcessOutputLogLines <= 0)
            {
                MaxProcessOutputLogLines = DefaultMaxProcessOutputLogLines;
            }

            if (MaxAnalyticsHashCacheSize <= 0)
            {
                MaxAnalyticsHashCacheSize = DefaultMaxAnalyticsHashCacheSize;
            }
        }

        private static int ResolveInt32(string variableName, int fallback)
        {
            string rawValue = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return fallback;
            }

            return int.TryParse(rawValue, out var parsedValue) && parsedValue > 0
                ? parsedValue
                : fallback;
        }
    }
}
