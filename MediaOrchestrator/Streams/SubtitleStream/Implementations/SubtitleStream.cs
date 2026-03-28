using System.Collections.Generic;
using System.Linq;
using MediaOrchestrator.Streams;
using MediaOrchestrator.Streams.SubtitleStream;

namespace MediaOrchestrator
{
    /// <inheritdoc />
    public sealed class SubtitleStream : ISubtitleStream
    {
        private readonly ParametersList<ConversionParameter> _parameters = new ParametersList<ConversionParameter>();

        /// <inheritdoc />
        public string Codec { get; internal set; }

        /// <inheritdoc />
        public string Path { get; internal set; }

        internal SubtitleStream()
        {

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
        public int Index { get; internal set; }

        /// <inheritdoc />
        public string Language { get; internal set; }

        /// <inheritdoc />
        public int? Default { get; internal set; }

        /// <inheritdoc />
        public int? Forced { get; internal set; }

        /// <inheritdoc />
        public string Title { get; internal set; }

        /// <inheritdoc />
        public StreamType StreamType => StreamType.Subtitle;

        /// <inheritdoc />
        public ISubtitleStream SetLanguage(string lang)
        {
            var language = !string.IsNullOrEmpty(lang) ? lang : Language;
            if (!string.IsNullOrEmpty(language))
            {
                _parameters.Add(new ConversionParameter(FFmpegSubtitleStreamArguments.SetLanguage(Index, language)));
            }

            return this;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSource()
        {
            return new[] { Path };
        }

        /// <inheritdoc />
        public ISubtitleStream SetCodec(SubtitleCodec codec)
        {
            return SetCodec(codec.ToString());
        }

        /// <inheritdoc />
        public ISubtitleStream SetCodec(string codec)
        {
            _parameters.Add(new ConversionParameter(FFmpegSubtitleStreamArguments.SetCodec(codec)));
            return this;
        }

        /// <inheritdoc />
        public ISubtitleStream UseNativeInputRead(bool readInputAtNativeFrameRate)
        {
            _parameters.Add(new ConversionParameter(FFmpegSubtitleStreamArguments.UseNativeInputRead(), ParameterPosition.PreInput));
            return this;
        }

        /// <inheritdoc />
        public ISubtitleStream SetStreamLoop(int loopCount)
        {
            _parameters.Add(new ConversionParameter(FFmpegSubtitleStreamArguments.SetStreamLoop(loopCount), ParameterPosition.PreInput));
            return this;
        }
    }
}
