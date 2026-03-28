namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл заблокирован другим процессом.
    /// </summary>
    public class InputFileLockedException : InvalidInputException
    {
        public InputFileLockedException(string message) : base(message)
        {
        }
    }
}
