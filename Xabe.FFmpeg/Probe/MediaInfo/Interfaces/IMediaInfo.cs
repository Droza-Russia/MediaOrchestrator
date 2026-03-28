using System;
using System.Collections.Generic;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Информация о медиафайле.
    /// </summary>
    public interface IMediaInfo
    {
        /// <summary>
        ///     Все потоки файла.
        /// </summary>
        IEnumerable<IStream> Streams { get; }

        /// <summary>
        ///     Сведения об источнике.
        /// </summary>
        string Path { get; }

        /// <summary>
        ///     Длительность медиа.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        ///     Дата и время создания медиа.
        /// </summary>
        DateTime? CreationTime { get; }

        /// <summary>
        ///     Размер файла.
        /// </summary>
        long Size { get; }

        /// <summary>
        ///     Видеопотоки.
        /// </summary>
        IEnumerable<IVideoStream> VideoStreams { get; }

        /// <summary>
        ///     Аудиопотоки.
        /// </summary>
        IEnumerable<IAudioStream> AudioStreams { get; }

        /// <summary>
        ///     Потоки субтитров.
        /// </summary>
        IEnumerable<ISubtitleStream> SubtitleStreams { get; }
    }
}
