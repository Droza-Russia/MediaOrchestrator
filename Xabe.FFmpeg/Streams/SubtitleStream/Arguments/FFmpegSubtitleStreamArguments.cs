namespace Xabe.FFmpeg
{
    internal static class FFmpegSubtitleStreamArguments
    {
        internal static string SetLanguage(int index, string language)
        {
            return $"-metadata:s:s:{index} language={language}";
        }

        internal static string SetCodec(string codec)
        {
            return $"-c:s {codec}";
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
