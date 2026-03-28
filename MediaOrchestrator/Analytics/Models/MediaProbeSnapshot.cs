using System;
using System.Linq;

namespace MediaOrchestrator.Analytics.Models
{
    internal sealed class MediaProbeSnapshot
    {
        public string ContainerHint { get; set; }

        public string PrimaryVideoCodec { get; set; }

        public string PrimaryAudioCodec { get; set; }

        public bool HasVideo { get; set; }

        public bool HasAudio { get; set; }

        public bool HasSubtitles { get; set; }

        public int VideoWidth { get; set; }

        public int VideoHeight { get; set; }

        public int AudioChannels { get; set; }

        public int AudioSampleRate { get; set; }

        public double DurationSeconds { get; set; }

        public long InputSizeBytes { get; set; }

        public static MediaProbeSnapshot Create(IMediaInfo mediaInfo)
        {
            if (mediaInfo == null)
            {
                throw new ArgumentNullException(nameof(mediaInfo));
            }

            var video = mediaInfo.VideoStreams.FirstOrDefault();
            var audio = mediaInfo.AudioStreams.FirstOrDefault();

            return new MediaProbeSnapshot
            {
                ContainerHint = GetContainerHint(mediaInfo.Path),
                PrimaryVideoCodec = video?.Codec ?? string.Empty,
                PrimaryAudioCodec = audio?.Codec ?? string.Empty,
                HasVideo = video != null,
                HasAudio = audio != null,
                HasSubtitles = mediaInfo.SubtitleStreams.Any(),
                VideoWidth = video?.Width ?? 0,
                VideoHeight = video?.Height ?? 0,
                AudioChannels = audio?.Channels ?? 0,
                AudioSampleRate = audio?.SampleRate ?? 0,
                DurationSeconds = mediaInfo.Duration.TotalSeconds,
                InputSizeBytes = mediaInfo.Size
            };
        }

        public string ToSignature()
        {
            return string.Join("|", new[]
            {
                ContainerHint ?? string.Empty,
                PrimaryVideoCodec ?? string.Empty,
                PrimaryAudioCodec ?? string.Empty,
                HasVideo ? "v1" : "v0",
                HasAudio ? "a1" : "a0",
                HasSubtitles ? "s1" : "s0",
                Bucket(VideoWidth),
                Bucket(VideoHeight),
                Bucket(AudioChannels),
                Bucket(AudioSampleRate),
                BucketDuration(DurationSeconds)
            });
        }

        private static string GetContainerHint(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                var extension = System.IO.Path.GetExtension(path);
                return string.IsNullOrWhiteSpace(extension)
                    ? string.Empty
                    : extension.TrimStart('.').ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string Bucket(int value)
        {
            if (value <= 0)
            {
                return "0";
            }

            if (value <= 1)
            {
                return "1";
            }

            if (value <= 2)
            {
                return "2";
            }

            if (value <= 720)
            {
                return "720";
            }

            if (value <= 1080)
            {
                return "1080";
            }

            if (value <= 2160)
            {
                return "2160";
            }

            return "2160+";
        }

        private static string BucketDuration(double durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                return "0";
            }

            if (durationSeconds < 30)
            {
                return "lt30";
            }

            if (durationSeconds < 300)
            {
                return "lt300";
            }

            if (durationSeconds < 1800)
            {
                return "lt1800";
            }

            return "ge1800";
        }
    }
}
