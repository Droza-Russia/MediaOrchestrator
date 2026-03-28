using System;

namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда выходная директория недоступна для записи.
    /// </summary>
    public class OutputDirectoryNotWritableException : MediaOrchestratorException
    {
        public OutputDirectoryNotWritableException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
