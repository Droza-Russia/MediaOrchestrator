using System;

namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда процесс FFmpeg завершился с ошибкой.
    /// </summary>
    public class ConversionException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда процесс FFmpeg завершился с ошибкой.
        /// </summary>
        /// <param name="message">Текст ошибки FFmpeg.</param>
        /// <param name="inputParameters">Входные параметры FFmpeg.</param>
        /// <param name="innerException">Внутреннее исключение.</param>
        public ConversionException(string message, Exception innerException, string inputParameters) : base(message, innerException)
        {
            InputParameters = inputParameters;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда процесс FFmpeg завершился с ошибкой.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки FFmpeg.</param>
        /// <param name="inputParameters">Входные параметры FFmpeg.</param>
        internal ConversionException(string errorMessage, string inputParameters) : base(errorMessage)
        {
            InputParameters = inputParameters;
        }

        /// <summary>
        ///     Входные параметры FFmpeg.
        /// </summary>
        public string InputParameters { get; }
    }
}
