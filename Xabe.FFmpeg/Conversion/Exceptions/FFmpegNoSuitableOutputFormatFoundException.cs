namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда FFmpeg не может подобрать подходящий выходной формат.
    /// </summary>
    public class FFmpegNoSuitableOutputFormatFoundException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда FFmpeg не может подобрать подходящий выходной формат.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки FFmpeg.</param>
        /// <param name="inputParameters">Входные параметры FFmpeg.</param>
        internal FFmpegNoSuitableOutputFormatFoundException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
