namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда найденный исполняемый файл не соответствует ожидаемой сигнатуре платформы.
    /// </summary>
    public class ExecutableSignatureMismatchException : ToolchainNotFoundException
    {
        internal ExecutableSignatureMismatchException(string message) : base(message)
        {
        }
    }
}
