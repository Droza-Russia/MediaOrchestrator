namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда запрошен недопустимый индекс потока.
    /// </summary>
    public class StreamIndexOutOfRangeException : InvalidInputException
    {
        public StreamIndexOutOfRangeException(string paramName, string message) : base(message)
        {
            ParamName = paramName;
        }

        public string ParamName { get; }
    }
}
