namespace MediaOrchestrator
{
    /// <summary>
    ///     Профиль настройки кодировщика MediaOrchestrator (`-tune`).
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
