namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной путь недоступен по правам доступа.
    /// </summary>
    public class InputPathAccessDeniedException : InputFileUnreadableException
    {
        public InputPathAccessDeniedException(string message) : base(message)
        {
        }
    }
}
