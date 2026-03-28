namespace MediaOrchestrator.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///      Исключение, выбрасываемое, когда MediaOrchestrator не может подобрать декодер для входного файла.
    /// </summary>
    public class UnknownDecoderException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда MediaOrchestrator не может найти кодек для декодирования файла.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки MediaOrchestrator.</param>
        /// <param name="inputParameters">Входные параметры MediaOrchestrator.</param>
        internal UnknownDecoderException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
