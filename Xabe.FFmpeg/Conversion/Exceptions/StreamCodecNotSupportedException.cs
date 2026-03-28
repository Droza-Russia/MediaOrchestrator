namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда кодек потока не поддерживается для выбранной операции или контейнера.
    /// </summary>
    public class StreamCodecNotSupportedException : ConversionException
    {
        public string RawFfmpegOutput { get; }

        internal StreamCodecNotSupportedException(string localizedMessage, string rawFfmpegOutput, string inputParameters)
            : base(localizedMessage, inputParameters)
        {
            RawFfmpegOutput = rawFfmpegOutput;
        }
    }
}
