namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда каталог с бинарными файлами MediaOrchestrator недоступен по правам доступа.
    /// </summary>
    public class ExecutablesPathAccessDeniedException : ToolchainNotFoundException
    {
        internal ExecutablesPathAccessDeniedException(string message) : base(message)
        {
        }
    }
}
