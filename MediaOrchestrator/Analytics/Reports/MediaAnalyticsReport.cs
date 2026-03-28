using System;
using System.Collections.Generic;

namespace MediaOrchestrator.Analytics.Reports
{
    public sealed class MediaAnalyticsReport
    {
        public DateTimeOffset GeneratedAtUtc { get; set; }

        public DateTimeOffset? FromUtc { get; set; }

        public DateTimeOffset? ToUtc { get; set; }

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

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByScenario { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByStrategy { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByFileType { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> BySizeBucket { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByDurationBucket { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByHardwareAccelerator { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByErrorCode { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByFailureType { get; set; }

        public IReadOnlyCollection<MediaAnalyticsBreakdownItem> ByFailureCategory { get; set; }

        public IReadOnlyCollection<MediaAnalyticsTimelinePoint> Timeline { get; set; }
    }
}
