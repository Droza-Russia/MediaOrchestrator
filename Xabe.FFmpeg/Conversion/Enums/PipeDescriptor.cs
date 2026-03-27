namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Доступные дескрипторы для перенаправления потоков ввода/вывода.
    /// </summary>
    public enum PipeDescriptor
    {
        /// <summary>
        ///     Поток стандартного ввода.
        /// </summary>
        stdin = 0,

        /// <summary>
        ///     Стандартный поток вывода.
        /// </summary>
        stdout = 1,

        /// <summary>
        ///     Стандартный поток ошибок.
        /// </summary>
        stderr = 2
    }
}
