namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл продолжает изменяться и не стабилизируется.
    /// </summary>
    public class InputFileStillBeingWrittenException : InvalidInputException
    {
        public InputFileStillBeingWrittenException(string message) : base(message)
        {
        }
    }
}
