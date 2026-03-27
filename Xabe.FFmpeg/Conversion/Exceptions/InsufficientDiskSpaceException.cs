namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Нехватка места на диске при записи выходного файла (распознаётся по журналу FFmpeg).
    /// </summary>
    public class InsufficientDiskSpaceException : ConversionException
    {
        /// <summary>
        ///     Исходный вывод stderr FFmpeg (для диагностики).
        /// </summary>
        public string RawFfmpegOutput { get; }

        /// <inheritdoc />
        internal InsufficientDiskSpaceException(string localizedMessage, string rawFfmpegOutput, string inputParameters)
            : base(localizedMessage, inputParameters)
        {
            RawFfmpegOutput = rawFfmpegOutput;
        }
    }
}
