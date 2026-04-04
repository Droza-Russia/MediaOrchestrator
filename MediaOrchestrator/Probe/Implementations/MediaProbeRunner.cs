using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Get information about media file
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    internal sealed class MediaProbeRunner : MediaOrchestrator
    {
        private const string VideoCodecType = "video";
        private const string AudioCodecType = "audio";
        private const string SubtitleCodecType = "subtitle";

        internal static Func<MediaProbeRunner, string, CancellationToken, Task<string>> ProbeCommandExecutor { get; set; } =
            (probeRunner, args, cancellationToken) => probeRunner.RunProbeProcessAsync(args, cancellationToken);

        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);

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

            if (string.IsNullOrWhiteSpace(stringResult))
            {
                return new ProbeModel
                {
                    Streams = Array.Empty<ProbeModel.Stream>()
                };
            }

            // Sanitize: trim whitespace and BOM, extract JSON block, remove trailing commas
            stringResult = SanitizeFfprobeOutput(stringResult);

            try
            {
                var probeData = JsonSerializer.Deserialize<ProbeModel>(stringResult, _defaultSerializerOptions);
                return probeData ?? new ProbeModel
                {
                    Streams = Array.Empty<ProbeModel.Stream>()
                };
            }
            catch (JsonException ex)
            {
                Debug.WriteLine(string.Format(ErrorMessages.FfprobeJsonParsingError, ex.Message));
                throw new InvalidOperationException(string.Format(ErrorMessages.FfprobeOutputParseFailed, videoPath.Unescape()), ex);
            }
        }

        private static string SanitizeFfprobeOutput(string output)
        {
            // Trim whitespace and UTF-8 BOM if present
            output = output.Trim().TrimStart('\uFEFF');

            // Extract JSON block (first '{' to last '}')
            int firstBrace = output.IndexOf('{');
            int lastBrace = output.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                output = output.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            // Remove trailing commas before closing brackets/braces
            // Only matches commas OUTSIDE of quoted strings to avoid corrupting metadata values
            output = RemoveTrailingCommas(output);

            return output;
        }

        private static string RemoveTrailingCommas(string json)
        {
            var sb = new System.Text.StringBuilder(json.Length);
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (!inString && c == ',')
                {
                    // Look ahead for closing bracket/brace, skipping whitespace
                    int j = i + 1;
                    while (j < json.Length && char.IsWhiteSpace(json[j]))
                        j++;

                    if (j < json.Length && (json[j] == '}' || json[j] == ']'))
                    {
                        // Skip the trailing comma
                        continue;
                    }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private double GetVideoFramerate(ProbeModel.Stream vid)
        {
            var frameCount = GetFrameCount(vid);
            var duration = vid.Duration;

            if (string.IsNullOrWhiteSpace(vid.RFrameRate))
            {
                return frameCount > 0 && duration > 0 ? Math.Round(frameCount / duration, 3) : 0;
            }

            var fr = vid.RFrameRate.Split('/');

            if (frameCount > 0)
            {
                return Math.Round(frameCount / duration, 3);
            }
            else if (fr.Length == 2 &&
                     double.TryParse(fr[0], out var nume) &&
                     double.TryParse(fr[1], out var deno) &&
                     deno > 0)
            {
                return Math.Round(nume / deno, 3);
            }

            return 0;
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

        private static int GetGcd(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0) (a, b) = (b, a % b);
            return a;
        }

        private static bool IsUriPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var schemeEnd = path.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd > 0 && schemeEnd < 10)
            {
                var scheme = path.Substring(0, schemeEnd);
                foreach (var c in scheme)
                {
                    if (!char.IsLetter(c) && c != '+' && c != '-' && c != '.')
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public Task<string> Start(string args, CancellationToken cancellationToken = default)
        {
            return ProbeCommandExecutor(this, args, cancellationToken);
        }

        private async Task<string> RunProbeProcessAsync(string args, CancellationToken cancellationToken)
        {
            using (Process process = RunProcess(args, FFprobePath, null, standardOutput: true, standardError: true))
            {
                var processExited = false;
                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        if (processExited || process.HasExited) return;
                        process.CloseMainWindow();
                        process.Kill();
                    }
                    catch
                    {
                        // ignored
                    }
                }))
                {
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    // Use CancellationToken.None so that caller cancellation
                    // throws OperationCanceledException, not TimeoutException.
                    var timeoutTask = Task.Delay(ProbeTimeout, CancellationToken.None);

                    var completedTask = await Task.WhenAny(outputTask, timeoutTask).ConfigureAwait(false);

                    if (completedTask == timeoutTask)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.CloseMainWindow();
                                process.Kill();
                            }
                        }
                        catch
                        {
                            // ignored
                        }

                        throw new TimeoutException(string.Format(ErrorMessages.FfprobeTimeout, ProbeTimeout.TotalSeconds));
                    }

                    var output = await outputTask.ConfigureAwait(false);
                    var error = await errorTask.ConfigureAwait(false);

                    await Task.Run(process.WaitForExit, cancellationToken).ConfigureAwait(false);
                    processExited = true;
                    cancellationToken.ThrowIfCancellationRequested();

                    if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
                    {
                        Debug.WriteLine(string.Format(ErrorMessages.FfprobeProcessError, process.ExitCode, error));
                    }

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
            var unescapedPath = mediaInfo.Path.Unescape();
            // Only check local file existence — skip for URIs (http, rtsp, rtmp, etc.)
            if (!IsUriPath(unescapedPath) && !File.Exists(unescapedPath))
            {
                throw new FileNotFoundException(string.Format(ErrorMessages.InvalidFileUnableToLoad, unescapedPath), unescapedPath);
            }

            var path = mediaInfo.Path.Escape();
            ProbeModel probeData = await GetProbeData(path, cancellationToken).ConfigureAwait(false);
            ProbeModel.Stream[] streams = probeData.Streams ?? Array.Empty<ProbeModel.Stream>();
            if (!streams.Any())
            {
                throw new ArgumentException(string.Format(ErrorMessages.InvalidFileUnableToLoad, path));
            }

            var format = probeData.Format;
            MediaFileSignatureValidator.ValidateDeclaredFormatOrThrow(mediaInfo.Path, format?.FormatName);
            if (format?.Size != null && long.TryParse(format.Size, out var size))
            {
                mediaInfo.Size = size;
            }

            mediaInfo.FormatName = format?.FormatName ?? string.Empty;
            mediaInfo.Bitrate = Math.Abs(format?.BitRate ?? 0);
            mediaInfo.Metadata = BuildContainerMetadata(format?.Tags);

            if (!string.IsNullOrWhiteSpace(format?.Tags?.CreationTime) && DateTimeOffset.TryParse(format.Tags.CreationTime, out var creationdate))
            {
                mediaInfo.CreationTime = creationdate.UtcDateTime;
            }

            mediaInfo.VideoStreams = PrepareVideoStreams(path, streams.Where(x => x.CodecType == VideoCodecType), format);
            mediaInfo.AudioStreams = PrepareAudioStreams(path, streams.Where(x => x.CodecType == AudioCodecType));
            mediaInfo.SubtitleStreams = PrepareSubtitleStreams(path, streams.Where(x => x.CodecType == SubtitleCodecType));

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
