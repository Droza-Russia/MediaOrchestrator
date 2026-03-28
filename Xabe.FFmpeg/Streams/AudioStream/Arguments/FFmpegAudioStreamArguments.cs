using System;

namespace MediaOrchestrator
{
    internal static class FFmpegAudioStreamArguments
    {
        internal static string SetAudioFilter(string expression)
        {
            return $"-af {expression}";
        }

        internal static string SetSeek(System.TimeSpan seek)
        {
            return $"-ss {seek.ToFFmpeg()}";
        }

        internal static string SetDuration(System.TimeSpan duration)
        {
            return $"-t {duration.ToFFmpeg()}";
        }

        internal static string SetChannels(int index, int channels)
        {
            return $"-ac:{index} {channels}";
        }

        internal static string SetBitstreamFilter(string filter)
        {
            return $"-bsf:a {filter}";
        }

        internal static string SetBitrate(int index, long bitRate)
        {
            return $"-b:a:{index} {bitRate}";
        }

        internal static string SetMaxRate(long bitRate)
        {
            return $"-maxrate {bitRate}";
        }

        internal static string SetBufferSize(long bufferSize)
        {
            return $"-bufsize {bufferSize}";
        }

        internal static string SetSampleRate(int index, int sampleRate)
        {
            return $"-ar:{index} {sampleRate}";
        }

        internal static string SetCodec(string codec)
        {
            return $"-c:a {codec} ";
        }

        internal static string SetInputFormat(string inputFormat)
        {
            return FFmpegInputArguments.SetInputFormat(inputFormat);
        }

        internal static string UseNativeInputRead()
        {
            return FFmpegVideoStreamArguments.UseNativeInputRead();
        }

        internal static string SetStreamLoop(int loopCount)
        {
            return FFmpegVideoStreamArguments.SetStreamLoop(loopCount);
        }
    }
}
