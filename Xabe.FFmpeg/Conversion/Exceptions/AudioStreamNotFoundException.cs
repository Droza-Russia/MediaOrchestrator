using System;

namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл не содержит аудиопоток.
    /// </summary>
    public class AudioStreamNotFoundException : ArgumentException
    {
        public AudioStreamNotFoundException(string message, string paramName) : base(message, paramName)
        {
        }
    }
}
