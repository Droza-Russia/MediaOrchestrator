using System;
using System.Globalization;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    internal static class FFmpegVideoStreamArguments
    {
        internal static string SetVideoFilter(string expression)
        {
            return $"-vf \"{expression}\"";
        }

        internal static string SetInputFormat(string format)
        {
            return FFmpegInputArguments.SetInputFormat(format);
        }

        internal static string UseNativeInputRead()
        {
            return "-re";
        }

        internal static string SetStreamLoop(int loopCount)
        {
            return $"-stream_loop {loopCount}";
        }

        internal static string SetLoop(int count)
        {
            return $"-loop {count}";
        }

        internal static string SetFinalDelay(int delay)
        {
            return $"-final_delay {delay}";
        }

        internal static string SetBitrate(long bitrate)
        {
            return $"-b:v {bitrate}";
        }

        internal static string SetMaxRate(long bitrate)
        {
            return $"-maxrate {bitrate}";
        }

        internal static string SetBufferSize(long bufferSize)
        {
            return $"-bufsize {bufferSize}";
        }

        internal static string SetFlags(string flags)
        {
            return $"-flags {flags}";
        }

        internal static string SetFrameRate(double framerate)
        {
            return $"-r {framerate.ToFFmpegFormat(3)}";
        }

        internal static string SetSize(VideoSize size)
        {
            return $"-s {size.ToFFmpegFormat()}";
        }

        internal static string SetSize(int width, int height)
        {
            return $"-s {width}x{height}";
        }

        internal static string SetCodec(string codec)
        {
            return $"-c:v {codec}";
        }

        internal static string SetBitstreamFilter(string filter)
        {
            return $"-bsf:v {filter}";
        }

        internal static string SetSeek(TimeSpan seek)
        {
            return $"-ss {seek.ToFFmpeg()}";
        }

        internal static string SetOutputFramesCount(int number)
        {
            return $"-frames:v {number}";
        }

        internal static string SetDuration(TimeSpan duration)
        {
            return $"-t {duration.ToFFmpeg()}";
        }
    }
}
