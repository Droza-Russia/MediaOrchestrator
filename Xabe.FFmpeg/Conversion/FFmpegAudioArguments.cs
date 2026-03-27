using System.Globalization;

namespace Xabe.FFmpeg
{
    internal static class FFmpegAudioArguments
    {
        internal const string CopyCodecValue = "-c:a copy";
        internal const string MapStreamsValue = "-map 0:a?";

        internal static string SetCodec(AudioCodec codec)
        {
            return $"-c:a {FFmpeg.ResolveTranscodeAudioCodecToString(codec)}";
        }

        internal static string SetSampleRate(int sampleRate)
        {
            return $"-ar {sampleRate.ToString(CultureInfo.InvariantCulture)}";
        }

        internal static string SetChannels(int channels)
        {
            return $"-ac {channels.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
