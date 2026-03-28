using System;

namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class StrategyExecutionStatistics
    {
        public MediaProcessingStrategy Strategy { get; set; }

        public int Attempts { get; set; }

        public int Successes { get; set; }

        public int Failures { get; set; }

        public double TotalProcessingMilliseconds { get; set; }

        public int ProcessingSamples { get; set; }

        public double TotalSpeedFactor { get; set; }

        public int SpeedFactorSamples { get; set; }

        public double TotalPeakWorkingSetBytes { get; set; }

        public int PeakWorkingSetSamples { get; set; }

        public long MaxPeakWorkingSetBytes { get; set; }

        public double TotalAverageCpuUsagePercent { get; set; }

        public int AverageCpuUsageSamples { get; set; }

        public double MaxPeakCpuUsagePercent { get; set; }

        public int TotalLogicalCoreCount { get; set; }

        public int LogicalCoreSamples { get; set; }

        public double TotalAverageAcceleratorUsagePercent { get; set; }

        public int AverageAcceleratorUsageSamples { get; set; }

        public double MaxPeakAcceleratorUsagePercent { get; set; }

        public DateTimeOffset LastUpdatedUtc { get; set; }

        public double SuccessRate => Attempts == 0 ? 0 : (double)Successes / Attempts;

        public double FailureRate => Attempts == 0 ? 0 : (double)Failures / Attempts;

        public double AverageProcessingMilliseconds => ProcessingSamples == 0 ? 0 : TotalProcessingMilliseconds / ProcessingSamples;

        public double AverageSpeedFactor => SpeedFactorSamples == 0 ? 0 : TotalSpeedFactor / SpeedFactorSamples;

        public double AveragePeakWorkingSetBytes => PeakWorkingSetSamples == 0 ? 0 : TotalPeakWorkingSetBytes / PeakWorkingSetSamples;

        public double AverageCpuUsagePercent => AverageCpuUsageSamples == 0 ? 0 : TotalAverageCpuUsagePercent / AverageCpuUsageSamples;

        public double AverageLogicalCoreCount => LogicalCoreSamples == 0 ? 0 : (double)TotalLogicalCoreCount / LogicalCoreSamples;

        public double AverageAcceleratorUsagePercent => AverageAcceleratorUsageSamples == 0 ? 0 : TotalAverageAcceleratorUsagePercent / AverageAcceleratorUsageSamples;
    }
}
