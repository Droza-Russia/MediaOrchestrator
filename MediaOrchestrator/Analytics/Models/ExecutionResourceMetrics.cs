namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class ExecutionResourceMetrics
    {
        public long PeakWorkingSetBytes { get; set; }

        public double AverageCpuUsagePercent { get; set; }

        public double PeakCpuUsagePercent { get; set; }

        public int LogicalCoreCount { get; set; }

        public double AverageAcceleratorUsagePercent { get; set; }

        public double PeakAcceleratorUsagePercent { get; set; }
    }
}
