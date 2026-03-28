using System;

namespace MediaOrchestrator.Analytics.Reports
{
    public sealed class MediaAnalyticsQuery
    {
        public DateTimeOffset? FromUtc { get; set; }

        public DateTimeOffset? ToUtc { get; set; }

        public MediaAnalyticsTimeBucket TimelineBucket { get; set; } = MediaAnalyticsTimeBucket.Day;
    }
}
