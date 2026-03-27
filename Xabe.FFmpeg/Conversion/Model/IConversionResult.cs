using System;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Информация о конвертации.
    /// </summary>
    public interface IConversionResult
    {
        /// <summary>
        ///     Дата и время начала конвертации.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        ///     Дата и время завершения конвертации.
        /// </summary>
        DateTime EndTime { get; }

        /// <summary>
        ///     Длительность конвертации.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        ///     Аргументы, переданные в ffmpeg.
        /// </summary>
        string Arguments { get; }
    }
}
