using System;

namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Базовое исключение библиотеки Xabe.FFmpeg.
    /// </summary>
    public class XabeFFmpegException : Exception
    {
        public XabeFFmpegException(string message) : base(message)
        {
        }

        public XabeFFmpegException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
