using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class MediaAnalysisRecord
    {
        public string AnalysisKey { get; set; }

        public int ModelVersion { get; set; }

        public ProcessingScenario Scenario { get; set; }

        public MediaProbeSnapshot ProbeSnapshot { get; set; }

        public string PreferredContainer { get; set; }

        public bool AllowRemux { get; set; }

        public bool AllowTranscode { get; set; }

        public bool AllowHardwareAcceleration { get; set; }

        public string DetectedHardwareAccelerator { get; set; }

        public DateTimeOffset CreatedUtc { get; set; }

        public DateTimeOffset UpdatedUtc { get; set; }

        public MediaProcessingPlanSnapshot LastPlan { get; set; }

        public List<StrategyExecutionStatistics> StrategyStatistics { get; set; } = new List<StrategyExecutionStatistics>();

        public List<MediaExecutionSample> RecentExecutions { get; set; } = new List<MediaExecutionSample>();

        public StrategyExecutionStatistics GetStrategyStatistics(MediaProcessingStrategy strategy)
        {
            var statistics = StrategyStatistics.FirstOrDefault(item => item.Strategy == strategy);
            if (statistics != null)
            {
                return statistics;
            }

            statistics = new StrategyExecutionStatistics
            {
                Strategy = strategy
            };
            StrategyStatistics.Add(statistics);
            return statistics;
        }

        public static MediaAnalysisRecord Create(
            string analysisKey,
            ProcessingScenario scenario,
            MediaProbeSnapshot probeSnapshot,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities)
        {
            return new MediaAnalysisRecord
            {
                AnalysisKey = analysisKey,
                ModelVersion = MediaProcessingAnalytics.ModelVersion,
                Scenario = scenario,
                ProbeSnapshot = probeSnapshot,
                PreferredContainer = constraints?.PreferredContainer?.ToString() ?? string.Empty,
                AllowRemux = constraints?.AllowRemux ?? true,
                AllowTranscode = constraints?.AllowTranscode ?? true,
                AllowHardwareAcceleration = constraints?.AllowHardwareAcceleration ?? true,
                DetectedHardwareAccelerator = capabilities?.DetectedHardwareAccelerator ?? string.Empty,
                CreatedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
