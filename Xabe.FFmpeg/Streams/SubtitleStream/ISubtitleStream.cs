using Xabe.FFmpeg.Streams.SubtitleStream;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Поток субтитров.
    /// </summary>
    public interface ISubtitleStream : IStream
    {
        /// <summary>
        ///     Язык субтитров.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// По умолчанию.
        /// </summary>
        int? Default { get; }

        /// <summary>
        /// Принудительно.
        /// </summary>
        int? Forced { get; }

        /// <summary>
        /// Заголовок.
        /// </summary>
        string Title { get; }

        /// <summary>
        ///     Устанавливает язык субтитров.
        /// </summary>
        /// <param name="language">Язык.</param>
        /// <returns>Объект ISubtitleStream.</returns>
        ISubtitleStream SetLanguage(string language);

        /// <summary>
        ///     Устанавливает кодек субтитров.
        /// </summary>
        /// <param name="codec">Кодек субтитров.</param>
        /// <returns>Объект ISubtitleStream.</returns>
        ISubtitleStream SetCodec(SubtitleCodec codec);

        /// <summary>
        ///     Устанавливает кодек субтитров.
        /// </summary>
        /// <param name="codec">Кодек субтитров.</param>
        /// <returns>Объект ISubtitleStream.</returns>
        ISubtitleStream SetCodec(string codec);

        /// <summary>
        ///     Параметр "-re". Читает входные данные с нативной частотой кадров.
        ///     Обычно используется для имитации устройства захвата или live-потока (например, при чтении из файла).
        ///     По умолчанию ffmpeg читает входные данные максимально быстро; эта опция замедляет чтение до нативной частоты.
        /// </summary>
        /// <param name="readInputAtNativeFrameRate">Читать вход с нативной частотой кадров. При False используется значение по умолчанию.</param>
        /// <returns>Объект ISubtitleStream.</returns>
        ISubtitleStream UseNativeInputRead(bool readInputAtNativeFrameRate);

        /// <summary>
        ///     Параметр "-stream_loop". Устанавливает количество повторов входного потока.
        /// </summary>
        /// <param name="loopCount">0 - без повтора, -1 - бесконечный повтор.</param>
        /// <returns>Объект ISubtitleStream.</returns>
        ISubtitleStream SetStreamLoop(int loopCount);
    }
}
