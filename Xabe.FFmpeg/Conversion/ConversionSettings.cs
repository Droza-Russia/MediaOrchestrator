using System;

namespace Xabe.FFmpeg
{
    internal sealed class AudioConversionSettings : IAudioConversionSettings
    {
        private readonly IConversion _conversion;

        public AudioConversionSettings(IConversion conversion)
        {
            _conversion = conversion;
        }

        public IConversion SetMaxFrequency(int maxFrequency)
        {
            if (maxFrequency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFrequency), ErrorMessages.FrequencyMustBeGreaterThanZero);
            }

            return _conversion.AddParameter($"-af \"lowpass=f={maxFrequency}\"");
        }

        public IConversion SetMinFrequency(int minFrequency)
        {
            if (minFrequency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minFrequency), ErrorMessages.FrequencyMustBeGreaterThanZero);
            }

            return _conversion.AddParameter($"-af \"highpass=f={minFrequency}\"");
        }

        public IConversion SetSampleRate(int sampleRate)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), ErrorMessages.SampleRateMustBeGreaterThanZeroForSettings);
            }

            return _conversion.AddParameter($"-ar {sampleRate}");
        }

        public IConversion SetChannels(int channels)
        {
            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), ErrorMessages.ChannelsMustBeGreaterThanZero);
            }

            return _conversion.AddParameter($"-ac {channels}");
        }

        public IConversion SetBitrate(long bitrate)
        {
            return _conversion.SetAudioBitrate(bitrate);
        }
    }

    internal sealed class VideoConversionSettings : IVideoConversionSettings
    {
        private readonly IConversion _conversion;

        public VideoConversionSettings(IConversion conversion)
        {
            _conversion = conversion;
        }

        public IConversion SetFrameRate(double frameRate)
        {
            return _conversion.SetFrameRate(frameRate);
        }

        public IConversion SetBitrate(long bitrate)
        {
            return _conversion.SetVideoBitrate(bitrate);
        }

        public IConversion SetPixelFormat(string pixelFormat)
        {
            return _conversion.SetPixelFormat(pixelFormat);
        }

        public IConversion SetPixelFormat(PixelFormat pixelFormat)
        {
            return _conversion.SetPixelFormat(pixelFormat);
        }
    }
}
