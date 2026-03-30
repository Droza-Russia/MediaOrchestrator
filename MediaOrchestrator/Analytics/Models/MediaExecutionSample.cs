using System;

namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class MediaExecutionSample
    {
        public DateTimeOffset TimestampUtc { get; set; }

        public MediaProcessingStrategy Strategy { get; set; }

        public bool Succeeded { get; set; }

        public double ProcessingMilliseconds { get; set; }

        public double InputDurationSeconds { get; set; }

        public double SpeedFactor { get; set; }

        public bool UsedHardwareAcceleration { get; set; }

        public long PeakWorkingSetBytes { get; set; }

        public double AverageCpuUsagePercent { get; set; }

        public double PeakCpuUsagePercent { get; set; }

        public int LogicalCoreCount { get; set; }

        public double AverageAcceleratorUsagePercent { get; set; }

        public double PeakAcceleratorUsagePercent { get; set; }

        public string FailureType { get; set; }

        public string FailureCategory { get; set; }

        public string ErrorCode { get; set; }

        public string Arguments { get; set; }
    }
}
