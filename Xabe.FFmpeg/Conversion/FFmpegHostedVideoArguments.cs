namespace Xabe.FFmpeg
{
    internal static class FFmpegHostedVideoArguments
    {
        internal const string DefaultFormat = "bestvideo+bestaudio/best";
        internal const string DownloaderExecutable = "yt-dlp";
        internal const string NoConfigFlag = "--no-config";
        internal const string NoProgressFlag = "--no-progress";
        internal const string NoContinueFlag = "--no-continue";
        internal const string NoPartFlag = "--no-part";
        internal const string NoPlaylistFlag = "--no-playlist";
        internal const string FormatOption = "-f";
        internal const string MergeOutputFormatOption = "--merge-output-format";
        internal const string FfmpegLocationOption = "--ffmpeg-location";
        internal const string OutputOption = "-o";
    }
}
