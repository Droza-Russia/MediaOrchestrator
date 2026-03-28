namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class MediaAnalysisSession
    {
        public string AnalysisKey { get; set; }

        public ProcessingScenario Scenario { get; set; }

        public ProcessingConstraints Constraints { get; set; }

        public EnvironmentCapabilities Capabilities { get; set; }

        public MediaProbeSnapshot ProbeSnapshot { get; set; }

        public MediaProcessingPlan Plan { get; set; }
    }
}
