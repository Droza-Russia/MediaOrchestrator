namespace MediaOrchestrator.Analytics.Reports
{
    public sealed class MediaAnalyticsBreakdownItem
    {
        public string Key { get; set; }

        public int Attempts { get; set; }

        public int Successes { get; set; }

        public int Failures { get; set; }

        public int HardwareAcceleratedRuns { get; set; }

        public double AverageProcessingMilliseconds { get; set; }

        public double AverageSpeedFactor { get; set; }

        public double AveragePeakWorkingSetBytes { get; set; }

        public long PeakWorkingSetBytes { get; set; }

        public double AverageCpuUsagePercent { get; set; }

        public double PeakCpuUsagePercent { get; set; }

        public double AverageLogicalCoreCount { get; set; }

        public double AverageAcceleratorUsagePercent { get; set; }

        public double PeakAcceleratorUsagePercent { get; set; }

        public double TotalInputDurationSeconds { get; set; }

        public long TotalInputSizeBytes { get; set; }
    }
}
