namespace MediaOrchestrator.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда MediaOrchestrator не может применить указанный bitstream filter.
    /// </summary>
    public class InvalidBitstreamFilterException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда MediaOrchestrator не может применить указанный bitstream filter.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки MediaOrchestrator.</param>
        /// <param name="inputParameters">Входные параметры MediaOrchestrator.</param>
        internal InvalidBitstreamFilterException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
