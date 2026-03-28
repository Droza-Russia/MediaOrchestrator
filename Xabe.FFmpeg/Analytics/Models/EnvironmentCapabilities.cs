namespace Xabe.FFmpeg.Analytics.Models
{
    public sealed class EnvironmentCapabilities
    {
        public static EnvironmentCapabilities DetectFromCurrentProcess()
        {
            return new EnvironmentCapabilities
            {
                IsHardwareAccelerationDetected = FFmpeg.IsHardwareAccelerationProfileDetected,
                DetectedHardwareAccelerator = FFmpeg.DetectedHardwareAcceleratorName
            };
        }

        public bool IsHardwareAccelerationDetected { get; set; }

        public string DetectedHardwareAccelerator { get; set; }
    }
}
