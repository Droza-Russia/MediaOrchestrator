using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    internal static class FFmpegFilters
    {
        internal static FFmpegFilter ShowFreqs(
            VisualisationMode mode,
            FrequencyScale frequencyScale,
            AmplitudeScale amplitudeScale)
        {
            return new FFmpegFilter(
                "showfreqs",
                FFmpegFilterArgument.Named("mode", mode.ToString()),
                FFmpegFilterArgument.Named("fscale", frequencyScale.ToString()),
                FFmpegFilterArgument.Named("ascale", amplitudeScale.ToString()));
        }

        internal static FFmpegFilter Format(PixelFormat pixelFormat)
        {
            return new FFmpegFilter("format", FFmpegFilterArgument.Positional(pixelFormat.ToString()));
        }

        internal static FFmpegFilter Scale(VideoSize size)
        {
            return new FFmpegFilter("scale", FFmpegFilterArgument.Positional(size.ToFFmpegFormat()));
        }

        internal static FFmpegFilter Scale(int width, int height)
        {
            return new FFmpegFilter("scale", FFmpegFilterArgument.Positional($"{width}:{height}"));
        }

        internal static FFmpegFilter SetDisplayAspectRatio(string ratio)
        {
            return new FFmpegFilter("setdar", FFmpegFilterArgument.Positional(ratio));
        }

        internal static FFmpegFilter ResetPresentationTimestamps()
        {
            return new FFmpegFilter("setpts", FFmpegFilterArgument.Positional("PTS-STARTPTS"));
        }

        internal static FFmpegFilter Concat(int inputCount, int videoOutputCount, int audioOutputCount)
        {
            return new FFmpegFilter(
                "concat",
                FFmpegFilterArgument.Named("n", inputCount.ToString()),
                FFmpegFilterArgument.Named("v", videoOutputCount.ToString()),
                FFmpegFilterArgument.Named("a", audioOutputCount.ToString()));
        }

        internal static FFmpegFilter Amix(int inputCount, AudioMixDurationMode durationMode, bool normalize)
        {
            return new FFmpegFilter(
                "amix",
                FFmpegFilterArgument.Named("inputs", inputCount.ToString()),
                FFmpegFilterArgument.Named("duration", durationMode.ToString().ToLowerInvariant()),
                FFmpegFilterArgument.Named("normalize", normalize ? "1" : "0"));
        }
    }
}
