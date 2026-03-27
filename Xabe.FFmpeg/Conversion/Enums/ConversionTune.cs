namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Профиль настройки кодировщика FFmpeg (`-tune`).
    /// </summary>
    public enum ConversionTune
    {
        Film,
        Animation,
        Grain,
        StillImage,
        FastDecode,
        ZeroLatency,
        Psnr,
        Ssim
    }
}
