using System;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    internal static class FFmpegInputArguments
    {
        internal const string ListDevicesValue = "-list_devices true";
        internal const string LavfiFormat = "lavfi";

        internal static string SetInputFormat(string format)
        {
            return $"-f {format}";
        }

        internal static string AddInput(string inputPath)
        {
            return $"-i {inputPath.Escape()}";
        }
    }
}
