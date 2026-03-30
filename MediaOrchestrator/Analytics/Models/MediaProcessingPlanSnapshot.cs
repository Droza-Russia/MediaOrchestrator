using System;
using System.Linq;

namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class MediaProcessingPlanSnapshot
    {
        public ProcessingScenario Scenario { get; set; }

        public MediaProcessingStrategy Strategy { get; set; }

        public Format TargetContainer { get; set; }

        public bool UseHardwareAcceleration { get; set; }

        public ProcessingDecisionReason[] Reasons { get; set; }

        public DateTimeOffset RecordedAtUtc { get; set; }

        public static MediaProcessingPlanSnapshot Create(MediaProcessingPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return new MediaProcessingPlanSnapshot
            {
                Scenario = plan.Scenario,
                Strategy = plan.Strategy,
                TargetContainer = plan.TargetContainer,
                UseHardwareAcceleration = plan.UseHardwareAcceleration,
                Reasons = plan.Reasons.ToArray(),
                RecordedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
