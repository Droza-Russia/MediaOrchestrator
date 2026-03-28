namespace MediaOrchestrator
{
    internal static class FFmpegHardwareAccelerationArguments
    {
        internal static string SetAnalysisDuration(long microseconds)
        {
            return $"-analyzeduration {microseconds}";
        }

        internal static string SetHardwareAcceleration(string hardwareAccelerator)
        {
            return $"-hwaccel {hardwareAccelerator}";
        }

        internal static string SetHardwareAccelerationDevice(int device)
        {
            return $"-hwaccel_device {device}";
        }

        internal static string SetVideoDecoder(string decoder)
        {
            return $"-c:v {decoder}";
        }

        internal static string SetVideoEncoder(string encoder)
        {
            return $"-c:v {encoder}";
        }
    }
}
