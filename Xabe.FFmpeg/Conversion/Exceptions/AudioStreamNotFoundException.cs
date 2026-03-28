namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл не содержит аудиопоток.
    /// </summary>
    public class AudioStreamNotFoundException : InvalidInputException
    {
        public AudioStreamNotFoundException(string message, string paramName) : base(message)
        {
            ParamName = paramName;
        }

        public string ParamName { get; }
    }
}
