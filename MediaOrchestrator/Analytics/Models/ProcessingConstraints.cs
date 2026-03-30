using System;

namespace MediaOrchestrator.Analytics.Models
{
    public sealed class ProcessingConstraints
    {
        public static ProcessingConstraints Default { get; } = new ProcessingConstraints();

        public TimeSpan? MaxDuration { get; set; }

        public long? MaxOutputSizeBytes { get; set; }

        public Format? PreferredContainer { get; set; }

        public bool AllowRemux { get; set; } = true;

        public bool AllowTranscode { get; set; } = true;

        public bool AllowHardwareAcceleration { get; set; } = true;
    }
}
