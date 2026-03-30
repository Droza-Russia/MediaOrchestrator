namespace MediaOrchestrator
{
    /// <summary>
    ///     Видео-настройки конвертации.
    /// </summary>
    public interface IVideoConversionSettings
    {
        /// <summary>
        ///     Устанавливает частоту кадров выходного видео.
        /// </summary>
        /// <param name="frameRate">Частота кадров.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetFrameRate(double frameRate);

        /// <summary>
        ///     Устанавливает битрейт видео.
        /// </summary>
        /// <param name="bitrate">Битрейт в битах.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetBitrate(long bitrate);

        /// <summary>
        ///     Устанавливает формат пикселей видео строковым значением.
        /// </summary>
        /// <param name="pixelFormat">Формат пикселей.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetPixelFormat(string pixelFormat);

        /// <summary>
        ///     Устанавливает формат пикселей видео из перечисления.
        /// </summary>
        /// <param name="pixelFormat">Формат пикселей.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetPixelFormat(PixelFormat pixelFormat);
    }
}
