namespace MediaOrchestrator
{
    /// <summary>
    ///     Аудио-настройки конвертации.
    /// </summary>
    public interface IAudioConversionSettings
    {
        /// <summary>
        ///     Устанавливает верхнюю граничную частоту аудио (low-pass).
        /// </summary>
        /// <param name="maxFrequency">Максимальная частота в Гц.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetMaxFrequency(int maxFrequency);

        /// <summary>
        ///     Устанавливает нижнюю граничную частоту аудио (high-pass).
        /// </summary>
        /// <param name="minFrequency">Минимальная частота в Гц.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetMinFrequency(int minFrequency);

        /// <summary>
        ///     Устанавливает частоту дискретизации аудио.
        /// </summary>
        /// <param name="sampleRate">Частота дискретизации в Гц.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetSampleRate(int sampleRate);

        /// <summary>
        ///     Устанавливает количество аудиоканалов.
        /// </summary>
        /// <param name="channels">Количество каналов.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetChannels(int channels);

        /// <summary>
        ///     Устанавливает битрейт аудио.
        /// </summary>
        /// <param name="bitrate">Битрейт в битах.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetBitrate(long bitrate);
    }
}
