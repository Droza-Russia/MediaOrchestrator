using System;

namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Ошибка скачивания видео с видеохостинга.
    /// </summary>
    public class HostedVideoDownloadException : XabeFFmpegException
    {
        public HostedVideoDownloadException(string message, string sourceUrl, string outputPath, string downloaderPath, string rawOutput = null, Exception innerException = null)
            : base(message, innerException)
        {
            SourceUrl = sourceUrl;
            OutputPath = outputPath;
            DownloaderPath = downloaderPath;
            RawDownloaderOutput = rawOutput;
        }

        public string SourceUrl { get; }

        public string OutputPath { get; }

        public string DownloaderPath { get; }

        public string RawDownloaderOutput { get; }
    }
}
