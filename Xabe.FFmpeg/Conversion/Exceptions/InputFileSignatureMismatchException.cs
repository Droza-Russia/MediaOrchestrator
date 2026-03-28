namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда сигнатура входного медиафайла не соответствует заявленному типу.
    /// </summary>
    public class InputFileSignatureMismatchException : InvalidInputException
    {
        internal InputFileSignatureMismatchException(string message) : base(message)
        {
        }
    }
}
