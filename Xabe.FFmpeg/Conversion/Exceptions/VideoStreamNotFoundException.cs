namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл не содержит видеопоток.
    /// </summary>
    public class VideoStreamNotFoundException : InvalidInputException
    {
        public VideoStreamNotFoundException(string message, string paramName) : base(message)
        {
            ParamName = paramName;
        }

        public string ParamName { get; }
    }
}
