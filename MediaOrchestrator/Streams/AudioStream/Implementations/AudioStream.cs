using System;
using System.Collections.Generic;
using System.Linq;
using MediaOrchestrator.Streams;

namespace MediaOrchestrator
{
    /// <inheritdoc cref="IAudioStream" />
    public sealed class AudioStream : IAudioStream, IFilterable
    {
        private readonly ParametersList<ConversionParameter> _parameters = new ParametersList<ConversionParameter>();
        private readonly Dictionary<StreamFilterName, string> _audioFilters = new Dictionary<StreamFilterName, string>();

        internal AudioStream()
        {

        }

        /// <inheritdoc />
        public IAudioStream Reverse()
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetAudioFilter(
                FFmpegAudioFilterExpressions.Reverse())));
            return this;
        }

        /// <inheritdoc />
        public string BuildParameters(ParameterPosition forPosition)
        {
            IEnumerable<ConversionParameter> parameters = _parameters?.Where(x => x.Position == forPosition);
            if (parameters != null &&
                parameters.Any())
            {
                return string.Join(string.Empty, parameters.Select(x => x.Parameter));
            }
            else
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public IAudioStream Split(TimeSpan startTime, TimeSpan duration)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetSeek(startTime)));
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetDuration(duration)));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream CopyStream()
        {
            return SetCodec(AudioCodec.copy);
        }

        /// <inheritdoc />
        public StreamType StreamType => StreamType.Audio;

        /// <inheritdoc />
        public IAudioStream SetChannels(int channels)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetChannels(Index, channels)));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetBitstreamFilter(BitstreamFilter filter)
        {
            return SetBitstreamFilter($"{filter}");
        }

        /// <inheritdoc />
        public IAudioStream SetBitstreamFilter(string filter)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetBitstreamFilter(filter)));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetBitrate(long bitRate)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetBitrate(Index, bitRate)));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetBitrate(long minBitrate, long maxBitrate, long bufferSize)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetBitrate(Index, minBitrate)));
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetMaxRate(maxBitrate)));
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetBufferSize(bufferSize)));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetSampleRate(int sampleRate)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetSampleRate(Index, sampleRate)));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream ChangeSpeed(double multiplication)
        {
            _audioFilters[StreamFilterName.Atempo] = FFmpegAudioFilterExpressions.ChangeTempo(multiplication);
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetCodec(AudioCodec codec)
        {
            var input = codec.ToString();
            if (codec == AudioCodec._4gv)
            {
                input = "4gv";
            }
            else if (codec == AudioCodec._8svx_exp)
            {
                input = "8svx_exp";
            }
            else if (codec == AudioCodec._8svx_fib)
            {
                input = "8svx_fib";
            }

            return SetCodec($"{input}");
        }

        /// <inheritdoc />
        public IAudioStream SetCodec(string codec)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetCodec(codec)));
            return this;
        }

        /// <inheritdoc />
        public int Index { get; internal set; }

        /// <inheritdoc />
        public TimeSpan Duration { get; internal set; }

        /// <inheritdoc />
        public string Codec { get; internal set; }

        /// <inheritdoc />
        public long Bitrate { get; internal set; }

        /// <inheritdoc />
        public int Channels { get; internal set; }

        /// <inheritdoc />
        public int SampleRate { get; internal set; }

        /// <inheritdoc />
        public string Language { get; internal set; }

        /// <inheritdoc />
        public string Title { get; internal set; }

        /// <inheritdoc />
        public int? Default { get; internal set; }

        /// <inheritdoc />
        public int? Forced { get; internal set; }

        /// <inheritdoc />
        public IEnumerable<string> GetSource()
        {
            return new[] { Path };
        }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public IAudioStream SetSeek(TimeSpan? seek)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetSeek(seek.Value), ParameterPosition.PreInput));
            return this;
        }

        /// <inheritdoc />
        IEnumerable<IFilterConfiguration> IFilterable.GetFilters()
        {
            if (_audioFilters.Any())
            {
                yield return new FilterConfiguration
                {
                    FilterBlockType = FilterBlockType.AudioFilter,
                    StreamNumber = Index,
                    TypedFilters = _audioFilters
                };
            }
        }

        /// <inheritdoc />
        public IAudioStream SetInputFormat(string inputFormat)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetInputFormat(inputFormat), ParameterPosition.PreInput));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetInputFormat(Format inputFormat)
        {
            return SetInputFormat(inputFormat.ToString());
        }

        /// <inheritdoc />
        public IAudioStream UseNativeInputRead(bool readInputAtNativeFrameRate)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.UseNativeInputRead(), ParameterPosition.PreInput));
            return this;
        }

        /// <inheritdoc />
        public IAudioStream SetStreamLoop(int loopCount)
        {
            _parameters.Add(new ConversionParameter(FFmpegAudioStreamArguments.SetStreamLoop(loopCount), ParameterPosition.PreInput));
            return this;
        }
    }
}
