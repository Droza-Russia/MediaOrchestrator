namespace MediaOrchestrator.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда MediaOrchestrator не может подобрать подходящий выходной формат.
    /// </summary>
    public class NoSuitableOutputFormatException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда MediaOrchestrator не может подобрать подходящий выходной формат.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки MediaOrchestrator.</param>
        /// <param name="inputParameters">Входные параметры MediaOrchestrator.</param>
        internal NoSuitableOutputFormatException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
