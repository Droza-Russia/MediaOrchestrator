using System;

namespace Xabe.FFmpeg
{
    internal static class FFmpegExecutionArguments
    {
        internal static string SetHashFormat(string hashFormat)
        {
            return $"-hash {hashFormat}";
        }

        internal static string SetSeek(TimeSpan seek)
        {
            return $"-ss {seek.ToFFmpeg()}";
        }

        internal static string SetDuration(TimeSpan duration)
        {
            return $"-t {duration.ToFFmpeg()}";
        }

        internal static string SetThreads(int threadsCount)
        {
            return $"-threads {threadsCount}";
        }

        internal static string SetVideoBitrate(long bitrate)
        {
            return $"-b:v {bitrate}";
        }

        internal static string SetMinRate(long bitrate)
        {
            return $"-minrate {bitrate}";
        }

        internal static string SetMaxRate(long bitrate)
        {
            return $"-maxrate {bitrate}";
        }

        internal static string SetBufferSize(long bufferSize)
        {
            return $"-bufsize {bufferSize}";
        }

        internal static string SetX264OptionsForCbr()
        {
            return "-x264opts nal-hrd=cbr:force-cfr=1";
        }

        internal static string SetAudioBitrate(long bitrate)
        {
            return $"-b:a {bitrate}";
        }

        internal static string UseShortest()
        {
            return "-shortest";
        }

        internal static string SelectEveryNthFrame(int frameNo)
        {
            return $"-vf select='not(mod(n\\,{frameNo}))'";
        }

        internal static string SelectNthFrame(int frameNo)
        {
            return $"-vf select='eq(n\\,{frameNo})'";
        }

        internal static string SetStartNumber(int startNumber)
        {
            return $"-start_number {startNumber}";
        }

        internal static string SetFrameRate(double frameRate, int precision = 3)
        {
            return $"-framerate {frameRate.ToFFmpegFormat(precision)}";
        }

        internal static string SetOutputFrameRate(double frameRate, int precision = 3)
        {
            return $"-r {frameRate.ToFFmpegFormat(precision)}";
        }

        internal static string SetOutputFormat(string format)
        {
            return $"-f {format}";
        }

        internal static string SetPixelFormat(string pixelFormat)
        {
            return $"-pix_fmt {pixelFormat}";
        }

        internal static string SetVideoSyncAuto()
        {
            return "-vsync -1";
        }

        internal static string SetVideoSync(VideoSyncMethod method)
        {
            return $"-vsync {method}";
        }

        internal static string SetDesktopOffsetX(int xOffset)
        {
            return $"-offset_x {xOffset}";
        }

        internal static string SetDesktopOffsetY(int yOffset)
        {
            return $"-offset_y {yOffset}";
        }

        internal static string SetDesktopVideoSize(string videoSize)
        {
            return $"-video_size {videoSize}";
        }

        internal static string OverwriteOutput()
        {
            return "-y";
        }

        internal static string PreserveOutput()
        {
            return "-n";
        }
    }
}
