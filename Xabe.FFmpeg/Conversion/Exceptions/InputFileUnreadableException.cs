namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл не может быть прочитан.
    /// </summary>
    public class InputFileUnreadableException : InvalidInputException
    {
        public InputFileUnreadableException(string message) : base(message)
        {
        }
    }
}
