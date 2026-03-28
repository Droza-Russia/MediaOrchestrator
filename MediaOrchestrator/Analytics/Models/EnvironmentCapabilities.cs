namespace MediaOrchestrator.Analytics.Models
{
    public sealed class EnvironmentCapabilities
    {
        public static EnvironmentCapabilities DetectFromCurrentProcess()
        {
            return new EnvironmentCapabilities
            {
                IsHardwareAccelerationDetected = MediaOrchestrator.IsHardwareAccelerationProfileDetected,
                DetectedHardwareAccelerator = MediaOrchestrator.DetectedHardwareAcceleratorName
            };
        }

        public bool IsHardwareAccelerationDetected { get; set; }

        public string DetectedHardwareAccelerator { get; set; }
    }
}
