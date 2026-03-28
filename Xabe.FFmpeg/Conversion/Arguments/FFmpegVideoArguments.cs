using System.Globalization;

namespace MediaOrchestrator
{
    internal static class FFmpegVideoArguments
    {
        internal const string DisableOutputFlag = "-vn";

        internal static string SetFrameRate(double frameRate)
        {
            return $"-r {frameRate.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
