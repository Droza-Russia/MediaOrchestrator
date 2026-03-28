namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Параметры аппаратного ускорения для декода/кодека H.264 и HEVC (подобраны под конкретный hwaccel из вывода ffmpeg -hwaccels).
    /// </summary>
    internal sealed class HardwareAccelerationProfile
    {
        public HardwareAccelerationProfile(string hwaccel, string inputDecoderCodec, string h264Encoder, string hevcEncoder)
        {
            Hwaccel = hwaccel;
            InputDecoderCodec = inputDecoderCodec;
            H264Encoder = h264Encoder;
            HevcEncoder = hevcEncoder;
        }

        /// <summary>Имя для опции -hwaccel.</summary>
        public string Hwaccel { get; }

        /// <summary>Значение для -c:v до входа (подсказка декодера).</summary>
        public string InputDecoderCodec { get; }

        public string H264Encoder { get; }

        public string HevcEncoder { get; }
    }
}
