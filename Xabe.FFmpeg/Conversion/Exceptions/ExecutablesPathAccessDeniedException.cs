namespace Xabe.FFmpeg.Exceptions
{
    /// <summary>
    ///     Исключение, выбрасываемое, когда каталог с бинарными файлами FFmpeg недоступен по правам доступа.
    /// </summary>
    public class ExecutablesPathAccessDeniedException : FFmpegNotFoundException
    {
        internal ExecutablesPathAccessDeniedException(string message) : base(message)
        {
        }
    }
}
