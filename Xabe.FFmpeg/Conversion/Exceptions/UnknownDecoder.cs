namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///      Исключение, выбрасываемое, когда FFmpeg не может подобрать декодер для входного файла.
    /// </summary>
    public class UnknownDecoderException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда FFmpeg не может найти кодек для декодирования файла.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки FFmpeg.</param>
        /// <param name="inputParameters">Входные параметры FFmpeg.</param>
        internal UnknownDecoderException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
