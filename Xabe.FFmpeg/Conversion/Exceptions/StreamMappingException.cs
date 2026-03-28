namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда FFmpeg не может сопоставить запрошенные потоки.
    /// </summary>
    public class StreamMappingException : ConversionException
    {
        public string RawFfmpegOutput { get; }

        internal StreamMappingException(string localizedMessage, string rawFfmpegOutput, string inputParameters)
            : base(localizedMessage, inputParameters)
        {
            RawFfmpegOutput = rawFfmpegOutput;
        }
    }
}
