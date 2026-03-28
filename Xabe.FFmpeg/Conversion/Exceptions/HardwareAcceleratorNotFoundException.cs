namespace MediaOrchestrator.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///      Исключение, выбрасываемое, когда MediaOrchestrator не может найти указанный аппаратный ускоритель.
    /// </summary>
    public class HardwareAcceleratorNotFoundException : ConversionException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда MediaOrchestrator не может найти указанный аппаратный ускоритель.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки MediaOrchestrator.</param>
        /// <param name="inputParameters">Входные параметры MediaOrchestrator.</param>
        internal HardwareAcceleratorNotFoundException(string errorMessage, string inputParameters) : base(errorMessage, inputParameters)
        {
        }
    }
}
