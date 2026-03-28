namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл не содержит поток субтитров.
    /// </summary>
    public class SubtitleStreamNotFoundException : InvalidInputException
    {
        public SubtitleStreamNotFoundException(string message, string paramName) : base(message)
        {
            ParamName = paramName;
        }

        public string ParamName { get; }
    }
}
