using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MediaOrchestrator.Streams;

namespace MediaOrchestrator
{
    /// <inheritdoc cref="IVideoStream" />
    public sealed class VideoStream : IVideoStream, IFilterable
    {
        private readonly ParametersList<ConversionParameter> _parameters = new ParametersList<ConversionParameter>();
        private readonly Dictionary<StreamFilterName, string> _videoFilters = new Dictionary<StreamFilterName, string>();
        private string _watermarkSource;
        private bool _outputUsesCopyCodec;
        private string _selectedOutputCodec;

        internal VideoStream()
        {

        }

        /// <inheritdoc />
        IEnumerable<IFilterConfiguration> IFilterable.GetFilters()
        {
            if (_videoFilters.Any())
            {
                yield return new FilterConfiguration
                {
                    FilterBlockType = FilterBlockType.ComplexFilter,
                    StreamNumber = Index,
                    TypedFilters = _videoFilters
                };
            }
        }

        /// <inheritdoc />
        public int Width { get; internal set; }

        /// <inheritdoc />
        public int Height { get; internal set; }

        /// <inheritdoc />
        public double Framerate { get; internal set; }

        /// <inheritdoc />
        public string Ratio { get; internal set; }

        /// <inheritdoc />
        public TimeSpan Duration { get; internal set; }

        /// <inheritdoc />
        public string Codec { get; internal set; }

        /// <inheritdoc />
        public int Index { get; internal set; }

        /// <inheritdoc />
        public string Path { get; internal set; }

        /// <inheritdoc />
        public int? Default { get; internal set; }

        /// <inheritdoc />
        public int? Forced { get; internal set; }

        /// <inheritdoc />
        public string PixelFormat { get; internal set; }

        /// <inheritdoc />
        public int? Rotation { get; internal set; }

        /// <summary>
        ///     Создает строку параметров
        /// </summary>
        /// <param name="forPosition">Позиция для параметров</param>
        /// <returns>Параметры</returns>
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
        public IVideoStream ChangeSpeed(double multiplication)
        {
            _videoFilters[StreamFilterName.SetPts] = GetVideoSpeedFilter(multiplication);
            return this;
        }

        private string GetVideoSpeedFilter(double multiplication)
        {
            if (multiplication < 0.5 || multiplication > 2.0)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplication), ErrorMessages.SpeedOutOfRange);
            }

            var videoMultiplicator = multiplication >= 1 ? 1 - ((multiplication - 1) / 2) : 1 + ((multiplication - 1) * -2);
            return $"{videoMultiplicator.ToFFmpegFormat()}*PTS ";
        }

        /// <inheritdoc />
        public IVideoStream Rotate(RotateDegrees rotateDegrees)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetVideoFilter(
                FFmpegVideoFilterExpressions.Rotate(rotateDegrees))));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream Pad(int width, int height)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetVideoFilter(
                FFmpegVideoFilterExpressions.PadToFit(width, height))));
            return this;
        }

        /// <inheritdoc />
        public StreamType StreamType => StreamType.Video;

        /// <inheritdoc />
        public long Bitrate { get; internal set; }

        /// <inheritdoc />
        public string Title { get; internal set; }

        /// <inheritdoc />
        public IVideoStream CopyStream()
        {
            _outputUsesCopyCodec = true;
            return SetCodec(VideoCodec.copy);
        }

        internal bool IsOutputCodecCopy => _outputUsesCopyCodec;

        internal string SelectedOutputCodec => _selectedOutputCodec;

        /// <inheritdoc />
        public IVideoStream SetLoop(int count, int delay)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetLoop(count)));
            if (delay > 0)
            {
                _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetFinalDelay(delay / 100)));
            }

            return this;
        }

        /// <inheritdoc />
        public IVideoStream AddSubtitles(string subtitlePath, VideoSize originalSize, string encode, string style)
        {
            return BuildSubtitleFilter(subtitlePath, originalSize, encode, style);
        }

        /// <inheritdoc />
        public IVideoStream AddSubtitles(string subtitlePath, string encode, string style)
        {
            return BuildSubtitleFilter(subtitlePath, null, encode, style);
        }

        private IVideoStream BuildSubtitleFilter(string subtitlePath, VideoSize? originalSize, string encode, string style)
        {
            _videoFilters[StreamFilterName.Subtitles] = FFmpegVideoFilterExpressions.BuildSubtitles(subtitlePath, originalSize, encode, style);
            return this;
        }

        /// <inheritdoc />
        public IVideoStream Reverse()
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetVideoFilter(
                FFmpegVideoFilterExpressions.Reverse())));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetBitrate(long bitrate)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetBitrate(bitrate)));
            return this;
        }

        public IVideoStream SetBitrate(long minBitrate, long maxBitrate, long bufferSize)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetBitrate(minBitrate)));
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetMaxRate(maxBitrate)));
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetBufferSize(bufferSize)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetFlags(params Flag[] flags)
        {
            return SetFlags(flags.Select(x => x.ToString()).ToArray());
        }

        /// <inheritdoc />
        public IVideoStream SetFlags(params string[] flags)
        {
            var input = string.Join("+", flags);
            if (input[0] != '+')
            {
                input = "+" + input;
            }

            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetFlags(input)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetFramerate(double framerate)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetFrameRate(framerate)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetSize(VideoSize size)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetSize(size)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetSize(int width, int height)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetSize(width, height)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetCodec(VideoCodec codec)
        {
            var input = codec.ToString();
            if (codec == VideoCodec._8bps)
            {
                input = "8bps";
            }
            else if (codec == VideoCodec._4xm)
            {
                input = "4xm";
            }
            else if (codec == VideoCodec._012v)
            {
                input = "012v";
            }

            return SetCodec($"{input}");
        }

        /// <inheritdoc />
        public IVideoStream SetCodec(string codec)
        {
            _outputUsesCopyCodec = string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase);
            _selectedOutputCodec = codec;
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetCodec(codec)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetBitstreamFilter(BitstreamFilter filter)
        {
            return SetBitstreamFilter($"{filter}");
        }

        /// <inheritdoc />
        public IVideoStream SetBitstreamFilter(string filter)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetBitstreamFilter(filter)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetSeek(TimeSpan seek)
        {
            if (seek != null)
            {
                if (seek > Duration)
                {
                    throw new ArgumentException(string.Format(ErrorMessages.SeekCannotExceedDuration, seek.TotalSeconds, Duration.TotalSeconds));
                }

                _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetSeek(seek), ParameterPosition.PreInput));
            }

            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetOutputFramesCount(int number)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetOutputFramesCount(number)));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetWatermark(string imagePath, Position position)
        {
            _watermarkSource = imagePath;
            _videoFilters[StreamFilterName.Overlay] = FFmpegVideoFilterExpressions.BuildOverlayPosition(position);
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetRightSideDrawText(
            string text,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var textClause = FFmpegVideoFilterExpressions.BuildDrawTextClause(text);
            _videoFilters[StreamFilterName.DrawText] = FFmpegVideoFilterExpressions.BuildDrawText(
                textClause,
                fontColor,
                fontSize,
                marginRight,
                marginY,
                verticalAlign,
                fontFilePath);
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetRightSidePtsTimeOverlay(
            string prefix = null,
            string suffix = null,
            bool useLocalWallClock = false,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null)
        {
            var textClause = FFmpegVideoFilterExpressions.BuildPtsTimeClause(prefix, suffix, useLocalWallClock);
            _videoFilters[StreamFilterName.DrawText] = FFmpegVideoFilterExpressions.BuildDrawText(
                textClause,
                fontColor,
                fontSize,
                marginRight,
                marginY,
                verticalAlign,
                fontFilePath);
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetRightSideSmpteTimecodeOverlay(
            string startTimecode = "00:00:00:00",
            double frameRate = 25,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null)
        {
            var timecodeClause = FFmpegVideoFilterExpressions.BuildSmpteTimecodeClause(startTimecode, frameRate);
            _videoFilters[StreamFilterName.DrawText] = FFmpegVideoFilterExpressions.BuildDrawText(
                timecodeClause,
                fontColor,
                fontSize,
                marginRight,
                marginY,
                verticalAlign,
                fontFilePath);
            return this;
        }

        /// <inheritdoc />
        public IVideoStream Split(TimeSpan startTime, TimeSpan duration)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetSeek(startTime)));
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetDuration(duration)));
            return this;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSource()
        {
            if (!string.IsNullOrWhiteSpace(_watermarkSource))
            {
                return new[] { Path, _watermarkSource };
            }

            return new[] { Path };
        }

        /// <inheritdoc />
        public IVideoStream SetInputFormat(Format inputFormat)
        {
            var format = inputFormat.ToString();
            switch (inputFormat)
            {
                case Format._3dostr:
                    format = "3dostr";
                    break;
                case Format._3g2:
                    format = "3g2";
                    break;
                case Format._3gp:
                    format = "3gp";
                    break;
                case Format._4xm:
                    format = "4xm";
                    break;
            }

            return SetInputFormat(format);
        }

        /// <inheritdoc />
        public IVideoStream SetInputFormat(string format)
        {
            if (format != null)
            {
                _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetInputFormat(format), ParameterPosition.PreInput));
            }

            return this;
        }

        /// <inheritdoc />
        public IVideoStream AddParameter(string parameter, ParameterPosition parameterPosition = ParameterPosition.PostInput)
        {
            _parameters.Add(new ConversionParameter(parameter, parameterPosition));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream UseNativeInputRead(bool readInputAtNativeFrameRate)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.UseNativeInputRead(), ParameterPosition.PreInput));
            return this;
        }

        /// <inheritdoc />
        public IVideoStream SetStreamLoop(int loopCount)
        {
            _parameters.Add(new ConversionParameter(FFmpegVideoStreamArguments.SetStreamLoop(loopCount), ParameterPosition.PreInput));
            return this;
        }
    }
}
