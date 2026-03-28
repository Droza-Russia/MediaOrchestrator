namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда найденный исполняемый файл не соответствует ожидаемой сигнатуре платформы.
    /// </summary>
    public class ExecutableSignatureMismatchException : FFmpegNotFoundException
    {
        internal ExecutableSignatureMismatchException(string message) : base(message)
        {
        }
    }
}
