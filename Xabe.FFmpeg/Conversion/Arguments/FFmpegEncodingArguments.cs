using System;

namespace MediaOrchestrator
{
    internal static class FFmpegEncodingArguments
    {
        internal static string SetPreset(ConversionPreset preset)
        {
            return $"-preset {preset.ToString().ToLowerInvariant()}";
        }

        internal static string SetTune(ConversionTune tune)
        {
            return $"-tune {ToTuneValue(tune)}";
        }

        private static string ToTuneValue(ConversionTune tune)
        {
            switch (tune)
            {
                case ConversionTune.StillImage:
                    return "stillimage";
                case ConversionTune.FastDecode:
                    return "fastdecode";
                case ConversionTune.ZeroLatency:
                    return "zerolatency";
                case ConversionTune.Psnr:
                    return "psnr";
                case ConversionTune.Ssim:
                    return "ssim";
                case ConversionTune.Film:
                    return "film";
                case ConversionTune.Animation:
                    return "animation";
                case ConversionTune.Grain:
                    return "grain";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tune), tune, null);
            }
        }
    }
}
