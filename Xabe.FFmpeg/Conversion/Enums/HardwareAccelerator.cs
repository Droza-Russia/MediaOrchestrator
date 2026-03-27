namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Аппаратные ускорители (посмотреть `ffmpeg -hwaccels`).
    /// </summary>
    public enum HardwareAccelerator
    {
        /// <summary>
        ///     Включает ускорение через d3d11va.
        /// </summary>
        d3d11va,

        /// <summary>
        ///     Автоматический выбор доступного ускорителя.
        /// </summary>
        auto,

        /// <summary>
        ///     Использует DXVA2 (DirectX Video Acceleration).
        /// </summary>
        dxva2,

        /// <summary>
        ///     Активирует Intel QuickSync Video для перекодировки.
        /// </summary>
        qsv,

        /// <summary>
        ///     NVIDIA NVDEC / CUDA (см. <c>ffmpeg -hwaccels</c>).
        /// </summary>
        cuda,

        /// <summary>
        ///     Apple Video Toolbox.
        /// </summary>
        videotoolbox,

        /// <summary>
        ///     Использует cuvid.
        /// </summary>
        cuvid,

        /// <summary>
        ///     Применяет VDPAU (API декодирования/вывода для Unix).
        /// </summary>
        vdpau,

        /// <summary>
        ///     Использует VAAPI (Video Acceleration API).
        /// </summary>
        vaapi,

        /// <summary>
        ///     Включает ускорение через libmfx.
        /// </summary>
        libmfx
    }
}
