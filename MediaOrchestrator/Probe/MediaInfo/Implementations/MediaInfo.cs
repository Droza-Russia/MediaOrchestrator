using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    /// <inheritdoc cref="IMediaInfo" />
    internal class MediaInfo : IMediaInfo
    {
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private static readonly ConcurrentDictionary<string, Lazy<Task<IMediaInfo>>> _inflight = new ConcurrentDictionary<string, Lazy<Task<IMediaInfo>>>();

        private sealed class CacheEntry
        {
            public IMediaInfo Value { get; set; }
            public DateTimeOffset ExpiresAtUtc { get; set; }
        }

        private MediaInfo(string path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public IEnumerable<IStream> Streams => VideoStreams.Concat<IStream>(AudioStreams)
                                                           .Concat(SubtitleStreams);

        /// <inheritdoc />
        public TimeSpan Duration { get; internal set; }

        /// <inheritdoc />
        public long Size { get; internal set; }

        /// <inheritdoc />
        public string FormatName { get; internal set; }

        /// <inheritdoc />
        public long Bitrate { get; internal set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Metadata { get; internal set; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public DateTime? CreationTime { get; internal set; }

        /// <inheritdoc />
        public IEnumerable<IVideoStream> VideoStreams { get; internal set; }

        /// <inheritdoc />
        public IEnumerable<IAudioStream> AudioStreams { get; internal set; }

        /// <inheritdoc />
        public IEnumerable<ISubtitleStream> SubtitleStreams { get; internal set; }

        /// <inheritdoc />
        public string Path { get; }

        /// <summary>
        ///     Get MediaInfo from file
        /// </summary>
        /// <param name="filePath">FullPath to file</param>
        internal static async Task<IMediaInfo> Get(string filePath)
        {
            using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                var cancellationToken = source.Token;
                return await Get(filePath, cancellationToken);
            }
        }

        /// <summary>
        ///     Get MediaInfo from file
        /// </summary>
        /// <param name="filePath">FullPath to file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        internal static async Task<IMediaInfo> Get(string filePath, CancellationToken cancellationToken)
        {
            var cacheKey = BuildCacheKey(filePath);
            var cacheEnabled = MediaOrchestrator.MediaInfoCacheEnabled;
            var cacheLifetime = MediaOrchestrator.MediaInfoCacheLifetime;
            IMediaInfo cached;
            if (cacheEnabled && TryGetFromCache(cacheKey, out cached))
            {
                return Clone(cached);
            }

            if (!cacheEnabled)
            {
                return await LoadMediaInfoAsync(filePath, cancellationToken).ConfigureAwait(false);
            }

            var loader = _inflight.GetOrAdd(
                cacheKey,
                _ => new Lazy<Task<IMediaInfo>>(
                    () => LoadMediaInfoAsync(filePath, CancellationToken.None),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            try
            {
                var mediaInfo = await loader.Value.ConfigureAwait(false);
                _cache[cacheKey] = new CacheEntry
                {
                    Value = Clone(mediaInfo),
                    ExpiresAtUtc = DateTimeOffset.UtcNow.Add(cacheLifetime)
                };

                cancellationToken.ThrowIfCancellationRequested();
                return Clone(mediaInfo);
            }
            finally
            {
                _inflight.TryRemove(cacheKey, out _);
            }
        }

        /// <summary>
        ///     Get MediaInfo from file
        /// </summary>
        /// <param name="fileInfo">FileInfo</param>
        internal static async Task<IMediaInfo> Get(FileInfo fileInfo)
        {
            if (!File.Exists(fileInfo.FullName))
            {
                throw new InvalidInputException(string.Format(ErrorMessages.InputFileDoesNotExist, fileInfo.FullName));
            }

            return await Get(fileInfo.FullName);
        }

        internal static void ClearCache()
        {
            _cache.Clear();
            _inflight.Clear();
        }

        private static async Task<IMediaInfo> LoadMediaInfoAsync(string filePath, CancellationToken cancellationToken)
        {
            await MediaFileSignatureValidator.ValidateOrThrowAsync(filePath, cancellationToken).ConfigureAwait(false);
            var mediaInfo = new MediaInfo(filePath);
            var wrapper = new MediaProbeRunner();
            return await wrapper.SetProperties(mediaInfo, cancellationToken).ConfigureAwait(false);
        }

        private static bool TryGetFromCache(string key, out IMediaInfo mediaInfo)
        {
            mediaInfo = null;
            if (!_cache.TryGetValue(key, out var entry))
            {
                return false;
            }

            if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _cache.TryRemove(key, out _);
                return false;
            }

            mediaInfo = entry.Value;
            return mediaInfo != null;
        }

        private static string BuildCacheKey(string filePath)
        {
            if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return $"uri::{filePath}";
            }

            if (!System.IO.Path.IsPathRooted(filePath))
            {
                return $"path::{filePath}";
            }

            var fullPath = System.IO.Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                return $"path::{fullPath}";
            }

            try
            {
                if (!FileHelper.TryGetFileInfo(fullPath, out var fileInfo))
                {
                    return $"path::{fullPath}";
                }

                return $"file::{fullPath}::{fileInfo.Length}::{fileInfo.LastWriteTimeUtc.Ticks}";
            }
            catch (IOException)
            {
                return $"path::{fullPath}";
            }
            catch (UnauthorizedAccessException)
            {
                return $"path::{fullPath}";
            }
        }

        private static IMediaInfo Clone(IMediaInfo source)
        {
            var clone = new MediaInfo(source.Path)
            {
                Duration = source.Duration,
                Size = source.Size,
                FormatName = source.FormatName,
                Bitrate = source.Bitrate,
                Metadata = source.Metadata?.ToDictionary(pair => pair.Key, pair => pair.Value)
                           ?? new Dictionary<string, string>(),
                CreationTime = source.CreationTime,
                VideoStreams = source.VideoStreams.Select(CloneVideo).ToList(),
                AudioStreams = source.AudioStreams.Select(CloneAudio).ToList(),
                SubtitleStreams = source.SubtitleStreams.Select(CloneSubtitle).ToList()
            };

            return clone;
        }

        private static IVideoStream CloneVideo(IVideoStream source)
        {
            return new VideoStream
            {
                Index = source.Index,
                Path = source.Path,
                Codec = source.Codec,
                Duration = source.Duration,
                Width = source.Width,
                Height = source.Height,
                Framerate = source.Framerate,
                Ratio = source.Ratio,
                Bitrate = source.Bitrate,
                Default = source.Default,
                Title = source.Title,
                Forced = source.Forced,
                PixelFormat = source.PixelFormat,
                Rotation = source.Rotation
            };
        }

        private static IAudioStream CloneAudio(IAudioStream source)
        {
            return new AudioStream
            {
                Index = source.Index,
                Path = source.Path,
                Codec = source.Codec,
                Duration = source.Duration,
                Bitrate = source.Bitrate,
                Channels = source.Channels,
                SampleRate = source.SampleRate,
                Language = source.Language,
                Title = source.Title,
                Default = source.Default,
                Forced = source.Forced
            };
        }

        private static ISubtitleStream CloneSubtitle(ISubtitleStream source)
        {
            return new SubtitleStream
            {
                Index = source.Index,
                Path = source.Path,
                Codec = source.Codec,
                Language = source.Language,
                Default = source.Default,
                Forced = source.Forced,
                Title = source.Title
            };
        }
    }
}
