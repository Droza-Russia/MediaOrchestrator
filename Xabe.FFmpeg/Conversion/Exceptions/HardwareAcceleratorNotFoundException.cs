namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///      Исключение, выбрасываемое, когда FFmpeg не может найти указанный аппаратный ускоритель.
    /// </summary>
    public class HardwareAcceleratorNotFoundException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда FFmpeg не может найти указанный аппаратный ускоритель.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки FFmpeg.</param>
        /// <param name="inputParameters">Входные параметры FFmpeg.</param>
        internal HardwareAcceleratorNotFoundException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
