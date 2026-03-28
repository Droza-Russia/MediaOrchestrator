namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда исполняемые файлы FFmpeg не найдены.
    /// </summary>
    public class FFmpegNotFoundException : XabeFFmpegException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда исполняемые файлы FFmpeg не найдены.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки.</param>
        internal FFmpegNotFoundException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
