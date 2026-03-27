namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Пресет конвертации: более высокая скорость снижает оптимизацию и качество.
    /// </summary>
    public enum ConversionPreset
    {
        /// <summary>
        ///     Очень медленный
        /// </summary>
        VerySlow,

        /// <summary>
        ///     Медленнее
        /// </summary>
        Slower,

        /// <summary>
        ///     Медленный
        /// </summary>
        Slow,

        /// <summary>
        ///     Средний
        /// </summary>
        Medium,

        /// <summary>
        ///     Быстрый
        /// </summary>
        Fast,

        /// <summary>
        ///     Быстрее
        /// </summary>
        Faster,

        /// <summary>
        ///     Очень быстрый
        /// </summary>
        VeryFast,

        /// <summary>
        ///     Супер быстрый
        /// </summary>
        SuperFast,

        /// <summary>
        ///     Ультра быстрый
        /// </summary>
        UltraFast
    }
}
