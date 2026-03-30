using System;

namespace MediaOrchestrator.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда процесс MediaOrchestrator завершился с ошибкой.
    /// </summary>
    public class ConversionException : MediaOrchestratorException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда процесс MediaOrchestrator завершился с ошибкой.
        /// </summary>
        /// <param name="message">Текст ошибки MediaOrchestrator.</param>
        /// <param name="inputParameters">Входные параметры MediaOrchestrator.</param>
        /// <param name="innerException">Внутреннее исключение.</param>
        public ConversionException(string message, Exception innerException, string inputParameters) : base(message, innerException)
        {
            InputParameters = inputParameters;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда процесс MediaOrchestrator завершился с ошибкой.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки MediaOrchestrator.</param>
        /// <param name="inputParameters">Входные параметры MediaOrchestrator.</param>
        internal ConversionException(string errorMessage, string inputParameters) : base(errorMessage)
        {
            InputParameters = inputParameters;
        }

        /// <summary>
        ///     Входные параметры MediaOrchestrator.
        /// </summary>
        public string InputParameters { get; }
    }
}
