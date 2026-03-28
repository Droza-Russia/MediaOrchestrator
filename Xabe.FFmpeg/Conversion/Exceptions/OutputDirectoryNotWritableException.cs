using System;

namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда выходная директория недоступна для записи.
    /// </summary>
    public class OutputDirectoryNotWritableException : XabeFFmpegException
    {
        public OutputDirectoryNotWritableException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
