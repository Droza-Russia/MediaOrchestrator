namespace MediaOrchestrator.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда исполняемые файлы MediaOrchestrator не найдены.
    /// </summary>
    public class ToolchainNotFoundException : MediaOrchestratorException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Исключение, выбрасываемое, когда исполняемые файлы MediaOrchestrator не найдены.
        /// </summary>
        /// <param name="errorMessage">Текст ошибки.</param>
        internal ToolchainNotFoundException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
