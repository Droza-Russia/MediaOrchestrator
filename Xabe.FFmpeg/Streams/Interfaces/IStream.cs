using System.Collections.Generic;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Базовый интерфейс потока.
    /// </summary>
    public interface IStream
    {
        /// <summary>
        ///     Источник потока (файл, pipe, etc.).
        /// </summary>
        string Path { get; }

        /// <summary>
        ///     Индекс потока.
        /// </summary>
        int Index { get; }

        /// <summary>
        ///     Используемый кодек/формат.
        /// </summary>
        string Codec { get; }

        /// <summary>
        ///     Составляет аргументы FFmpeg для указанной позиции.
        /// </summary>
        /// <returns>Строка аргументов.</returns>
        string BuildParameters(ParameterPosition forPosition);

        /// <summary>
        ///     Получает исходные файлы/пути для потока.
        /// </summary>
        /// <returns>Набор путей источников.</returns>
        IEnumerable<string> GetSource();

        /// <summary>
        ///     Тип потока (видео, аудио и т.д.).
        /// </summary>
        StreamType StreamType { get; }
    }
}
