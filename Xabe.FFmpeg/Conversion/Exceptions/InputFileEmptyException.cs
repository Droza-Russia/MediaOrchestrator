namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл пуст.
    /// </summary>
    public class InputFileEmptyException : InvalidInputException
    {
        public InputFileEmptyException(string message) : base(message)
        {
        }
    }
}
