namespace Xabe.FFmpeg
{
    internal static class FFmpegContainerArguments
    {
        internal const string CopyAllCodecsValue = "-c copy";
        internal const string MapAllStreamsValue = "-map 0";

        internal static string SetOutputFormat(Format format)
        {
            return $"-f {format}";
        }
    }
}
