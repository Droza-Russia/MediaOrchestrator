using System;

namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда выходной путь недоступен.
    /// </summary>
    public class OutputPathAccessDeniedException : MediaOrchestratorException
    {
        public OutputPathAccessDeniedException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
