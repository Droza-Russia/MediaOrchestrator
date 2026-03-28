using System;
using System.Collections.Generic;
using System.Linq;

namespace Xabe.FFmpeg.Analytics.Models
{
    public sealed class MediaProcessingPlan
    {
        public MediaProcessingPlan(
            ProcessingScenario scenario,
            MediaProcessingStrategy strategy,
            Format targetContainer,
            bool useHardwareAcceleration,
            IEnumerable<ProcessingDecisionReason> reasons)
        {
            Scenario = scenario;
            Strategy = strategy;
            TargetContainer = targetContainer;
            UseHardwareAcceleration = useHardwareAcceleration;
            Reasons = (reasons ?? Array.Empty<ProcessingDecisionReason>()).ToArray();
        }

        public ProcessingScenario Scenario { get; }

        public MediaProcessingStrategy Strategy { get; }

        public Format TargetContainer { get; }

        public bool UseHardwareAcceleration { get; }

        public IReadOnlyCollection<ProcessingDecisionReason> Reasons { get; }

        public bool RequiresAudioNormalization => Strategy == MediaProcessingStrategy.NormalizeAudio;

        public bool RequiresTranscode => Strategy == MediaProcessingStrategy.PartialTranscode || Strategy == MediaProcessingStrategy.FullTranscode || Strategy == MediaProcessingStrategy.NormalizeAudio;

        public bool IsRemux => Strategy == MediaProcessingStrategy.Remux;
    }
}
