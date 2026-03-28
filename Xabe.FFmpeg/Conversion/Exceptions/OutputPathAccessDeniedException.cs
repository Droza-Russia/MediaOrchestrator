using System;

namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда выходной путь недоступен.
    /// </summary>
    public class OutputPathAccessDeniedException : XabeFFmpegException
    {
        public OutputPathAccessDeniedException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
