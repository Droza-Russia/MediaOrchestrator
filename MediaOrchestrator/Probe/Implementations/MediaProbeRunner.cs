using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Get information about media file
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    internal sealed class MediaProbeRunner : MediaOrchestrator
    {
        internal static Func<MediaProbeRunner, string, CancellationToken, Task<string>> ProbeCommandExecutor { get; set; } =
            (wrapper, args, cancellationToken) => wrapper.RunProbeProcessAsync(args, cancellationToken);

        private readonly JsonSerializerOptions _defaultSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            WriteIndented = true
        };

        private async Task<ProbeModel> GetProbeData(string videoPath, CancellationToken cancellationToken)
        {
            var stringResult = await Start(
                $"-v panic -print_format json=c=1 -show_streams -show_entries format=format_name,size,duration,bit_rate:format_tags {videoPath}",
                cancellationToken);
            if (string.IsNullOrEmpty(stringResult))
            {
                return new ProbeModel
                {
                    Streams = Array.Empty<ProbeModel.Stream>()
                };
            }

            return JsonSerializer.Deserialize<ProbeModel>(stringResult, _defaultSerializerOptions) ?? new ProbeModel
            {
                Streams = Array.Empty<ProbeModel.Stream>()
            };
        }

        private double GetVideoFramerate(ProbeModel.Stream vid)
        {
            var frameCount = GetFrameCount(vid);
            var duration = vid.Duration;
            var fr = vid.RFrameRate.Split('/');

            if (frameCount > 0)
            {
                return Math.Round(frameCount / duration, 3);
            }
            else
            {
                return Math.Round(double.Parse(fr[0]) / double.Parse(fr[1]), 3);
            }
        }

        private long GetFrameCount(ProbeModel.Stream vid)
        {
            return long.TryParse(vid.NbFrames, out var frameCount) ? frameCount : 0;
        }

        private string GetVideoAspectRatio(int width, int height)
        {
            var cd = GetGcd(width, height);
            if (cd <= 0)
            {
                return "0:0";
            }

            return (width / cd) + ":" + (height / cd);
        }

        private TimeSpan GetAudioDuration(ProbeModel.Stream audio)
        {
            var duration = audio.Duration;
            var audioDuration = TimeSpan.FromSeconds(duration);
            return audioDuration;
        }

        private TimeSpan GetVideoDuration(ProbeModel.Stream video, ProbeModel.ProbeFormat format)
        {
            var duration = video.Duration > 0.01 ? video.Duration : (format?.Duration ?? 0);
            var videoDuration = TimeSpan.FromSeconds(duration);
            return videoDuration;
        }

        private int GetGcd(int width, int height)
        {
            width = Math.Abs(width);
            height = Math.Abs(height);
            while (height != 0)
            {
                var remainder = width % height;
                width = height;
                height = remainder;
            }

            return width;
        }

        public Task<string> Start(string args, CancellationToken cancellationToken = default)
        {
            return ProbeCommandExecutor(this, args, cancellationToken);
        }

        private async Task<string> RunProbeProcessAsync(string args, CancellationToken cancellationToken)
        {
            using (Process process = RunProcess(args, FFprobePath, null, standardOutput: true))
            {
                var processExited = false;
                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!processExited && !process.HasExited)
                        {
                            process.CloseMainWindow();
                            process.Kill();
                        }
                    }
                    catch
                    {
                    }
                }))
                {
                    var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    process.WaitForExit();
                    processExited = true;
                    cancellationToken.ThrowIfCancellationRequested();
                    return output;
                }
            }
        }

        /// <summary>
        ///     Get proporties from media file
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="mediaInfo">Empty media info</param>
        /// <returns>Properties</returns>
        internal async Task<MediaInfo> SetProperties(MediaInfo mediaInfo, CancellationToken cancellationToken)
        {
            var path = mediaInfo.Path.Escape();
            ProbeModel probeData = await GetProbeData(path, cancellationToken).ConfigureAwait(false);
            ProbeModel.Stream[] streams = probeData.Streams ?? Array.Empty<ProbeModel.Stream>();
            if (!streams.Any())
            {
                throw new ArgumentException(string.Format(ErrorMessages.InvalidFileUnableToLoad, path));
            }

            var format = probeData.Format;
            MediaFileSignatureValidator.ValidateDeclaredFormatOrThrow(mediaInfo.Path, format?.FormatName);
            if (format?.Size != null)
            {
                mediaInfo.Size = long.Parse(format.Size);
            }

            mediaInfo.FormatName = format?.FormatName ?? string.Empty;
            mediaInfo.Bitrate = Math.Abs(format?.BitRate ?? 0);
            mediaInfo.Metadata = BuildContainerMetadata(format?.Tags);

            if (!string.IsNullOrWhiteSpace(format?.Tags?.CreationTime) && DateTimeOffset.TryParse(format.Tags.CreationTime, out var creationdate))
            {
                mediaInfo.CreationTime = creationdate.UtcDateTime;
            }

            mediaInfo.VideoStreams = PrepareVideoStreams(path, streams.Where(x => x.CodecType == "video"), format);
            mediaInfo.AudioStreams = PrepareAudioStreams(path, streams.Where(x => x.CodecType == "audio"));
            mediaInfo.SubtitleStreams = PrepareSubtitleStreams(path, streams.Where(x => x.CodecType == "subtitle"));

            mediaInfo.Duration = CalculateDuration(mediaInfo.VideoStreams, mediaInfo.AudioStreams);
            return mediaInfo;
        }

        private static TimeSpan CalculateDuration(IEnumerable<IVideoStream> videoStreams, IEnumerable<IAudioStream> audioStreams)
        {
            var audioMax = audioStreams.Any() ? audioStreams.Max(x => x.Duration.TotalSeconds) : 0;
            var videoMax = videoStreams.Any() ? videoStreams.Max(x => x.Duration.TotalSeconds) : 0;

            return TimeSpan.FromSeconds(Math.Max(audioMax, videoMax));
        }

        private IEnumerable<IAudioStream> PrepareAudioStreams(string path, IEnumerable<ProbeModel.Stream> audioStreamModels)
        {
            return audioStreamModels.Select(model => new AudioStream()
            {
                Codec = model.CodecName,
                Duration = GetAudioDuration(model),
                Path = path,
                Index = model.Index,
                Bitrate = Math.Abs(model.BitRate),
                Channels = model.Channels,
                SampleRate = model.SampleRate,
                Language = model.Tags?.Language,
                Default = model.Disposition?.Default,
                Title = model.Tags?.Title,
                Forced = model.Disposition?.Forced,
            });
        }

        private static IEnumerable<ISubtitleStream> PrepareSubtitleStreams(string path, IEnumerable<ProbeModel.Stream> subtitleStreamModels)
        {
            return subtitleStreamModels.Select(model => new SubtitleStream()
            {
                Codec = model.CodecName,
                Path = path,
                Index = model.Index,
                Language = model.Tags?.Language,
                Title = model.Tags?.Title,
                Default = model.Disposition?.Default,
                Forced = model.Disposition?.Forced,
            });
        }

        private IEnumerable<IVideoStream> PrepareVideoStreams(string path, IEnumerable<ProbeModel.Stream> videoStreamModels, ProbeModel.ProbeFormat format)
        {
            return videoStreamModels.Select(model => new VideoStream()
            {
                Codec = model.CodecName,
                Duration = GetVideoDuration(model, format),
                Width = model.Width,
                Height = model.Height,
                Framerate = GetVideoFramerate(model),
                Ratio = GetVideoAspectRatio(model.Width, model.Height),
                Path = path,
                Index = model.Index,
                Bitrate = Math.Abs(model.BitRate) > 0.01 ? model.BitRate : (format?.BitRate ?? 0),
                PixelFormat = model.PixFmt,
                Default = model.Disposition?.Default,
                Title = model.Tags?.Title,
                Forced = model.Disposition?.Forced,
                Rotation = model.Tags?.Rotate
            });
        }

        private static IReadOnlyDictionary<string, string> BuildContainerMetadata(ProbeModel.FormatTags tags)
        {
            if (tags?.AdditionalTags == null || tags.AdditionalTags.Count == 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return tags.AdditionalTags
                .Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ValueKind == JsonValueKind.String ? pair.Value.GetString() : pair.Value.ToString()))
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
