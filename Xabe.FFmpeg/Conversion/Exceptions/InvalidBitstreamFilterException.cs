namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда FFmpeg не может применить указанный bitstream filter.
    /// </summary>
    public class InvalidBitstreamFilterException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда FFmpeg не может применить указанный bitstream filter.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки FFmpeg.</param>
        /// <param name="inputParameters">Входные параметры FFmpeg.</param>
        internal InvalidBitstreamFilterException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
