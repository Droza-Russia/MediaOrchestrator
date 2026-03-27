namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Вертикальное выравнивание подписи у правого края кадра.
    /// </summary>
    public enum DrawTextVerticalAlign
    {
        /// <summary>
        ///     Сверху (отступ от верхнего края задаётся параметром marginY).
        /// </summary>
        Top,

        /// <summary>
        ///     По центру по вертикали.
        /// </summary>
        Center,

        /// <summary>
        ///     Снизу (отступ от нижнего края задаётся параметром marginY).
        /// </summary>
        Bottom
    }
}
