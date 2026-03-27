using System;
using System.Collections.Generic;
using System.Linq;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Настройки скачивания видео с видеохостинга через yt-dlp/youtube-dl.
    /// </summary>
    public sealed class HostedVideoDownloadSettings
    {
        /// <summary>
        ///     Путь к исполняемому файлу yt-dlp/youtube-dl (по умолчанию пытается разрешить `YT_DLP_PATH` и потом `yt-dlp`).
        /// </summary>
        public string DownloaderPath { get; set; }

        /// <summary>
        ///     Выражение формата ffmpeg (например, `bestvideo+bestaudio/best`).
        /// </summary>
        public string Format { get; set; } = "bestvideo+bestaudio/best";

        /// <summary>
        ///     Формат, в который yt-dlp будет мерджить отдельные дорожки (если нужно). По умолчанию mp4.
        /// </summary>
        public string MergeOutputFormat { get; set; } = "mp4";

        /// <summary>
        ///     Прочие аргументы, которые необходимо передать yt-dlp.
        /// </summary>
        public IEnumerable<string> AdditionalArguments { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     Если true — добавляет `--no-playlist`.
        /// </summary>
        public bool NoPlaylist { get; set; } = true;

        /// <summary>
        ///     Если true — добавляет `--no-continue`.
        /// </summary>
        public bool NoContinue { get; set; } = true;

        /// <summary>
        ///     Если true — добавляет `--no-part`.
        /// </summary>
        public bool NoPart { get; set; } = true;

        /// <summary>
        ///     Если true — добавляет `--no-progress`.
        /// </summary>
        public bool NoProgress { get; set; } = true;
    }
}
