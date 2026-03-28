using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg.Analytics;
using Xabe.FFmpeg.Analytics.Models;
using Xabe.FFmpeg.Streams.SubtitleStream;
using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class MediaProcessingAnalyticsTests : IDisposable
    {
        private readonly Func<FFprobeWrapper, string, System.Threading.CancellationToken, Task<string>> _originalProbeExecutor = FFprobeWrapper.ProbeCommandExecutor;

        public MediaProcessingAnalyticsTests()
        {
            FFmpeg.ClearMediaInfoCache();
            FFmpeg.MediaInfoCacheEnabled = true;
        }

        public void Dispose()
        {
            FFprobeWrapper.ProbeCommandExecutor = _originalProbeExecutor;
            FFmpeg.ClearMediaInfoCache();
            FFmpeg.SetExecutablesPath(null);
        }

        [Fact]
        public void DecideProcessingPlan_AiTranscription_ReturnsNormalizeAudioPlan()
        {
            var analytics = new MediaProcessingAnalytics();
            var mediaInfo = new FakeMediaInfo(
                path: "sample.mp4",
                audioCodecs: new[] { "aac" },
                videoCodecs: new[] { "h264" });

            var plan = analytics.DecideProcessingPlan(
                mediaInfo,
                ProcessingScenario.AiTranscription,
                ProcessingConstraints.Default,
                new EnvironmentCapabilities());

            Assert.Equal(MediaProcessingStrategy.NormalizeAudio, plan.Strategy);
            Assert.Equal(Format.wav, plan.TargetContainer);
            Assert.Contains(ProcessingDecisionReason.ScenarioAiTranscriptionContract, plan.Reasons);
        }

        [Fact]
        public void DecideProcessingPlan_BrowserPlayback_UsesRemuxForCompatibleMp4()
        {
            var analytics = new MediaProcessingAnalytics();
            var mediaInfo = new FakeMediaInfo(
                path: "sample.mp4",
                audioCodecs: new[] { "aac" },
                videoCodecs: new[] { "h264" });

            var plan = analytics.DecideProcessingPlan(
                mediaInfo,
                ProcessingScenario.BrowserPlayback,
                new ProcessingConstraints { AllowRemux = true, AllowTranscode = true, PreferredContainer = Format.mp4 },
                new EnvironmentCapabilities());

            Assert.Equal(MediaProcessingStrategy.Remux, plan.Strategy);
            Assert.Contains(ProcessingDecisionReason.BrowserCodecsCompatible, plan.Reasons);
            Assert.Contains(ProcessingDecisionReason.RemuxAllowed, plan.Reasons);
        }

        [Fact]
        public void DecideProcessingPlan_BrowserPlayback_UsesFullTranscodeForIncompatibleCodecs()
        {
            var analytics = new MediaProcessingAnalytics();
            var mediaInfo = new FakeMediaInfo(
                path: "sample.mkv",
                audioCodecs: new[] { "flac" },
                videoCodecs: new[] { "hevc" });

            var plan = analytics.DecideProcessingPlan(
                mediaInfo,
                ProcessingScenario.BrowserPlayback,
                new ProcessingConstraints { AllowRemux = true, AllowTranscode = true, PreferredContainer = Format.mp4 },
                new EnvironmentCapabilities { IsHardwareAccelerationDetected = true });

            Assert.Equal(MediaProcessingStrategy.FullTranscode, plan.Strategy);
            Assert.True(plan.UseHardwareAcceleration);
            Assert.Contains(ProcessingDecisionReason.BrowserCodecsIncompatible, plan.Reasons);
            Assert.Contains(ProcessingDecisionReason.TranscodeAllowed, plan.Reasons);
        }

        [Fact]
        public async Task BuildConversionAsync_AiTranscription_UsesNormalizeSnippet()
        {
            var tempDir = TestFileFactory.CreateTempDirectory();
            var inputPath = TestFileFactory.CreateFakeMediaFile(tempDir, "sample.mp4");
            TestFileFactory.CreateFakeExecutable(tempDir, "ffmpeg");
            TestFileFactory.CreateFakeExecutable(tempDir, "ffprobe");
            FFmpeg.SetExecutablesPath(tempDir);
            FFprobeWrapper.ProbeCommandExecutor = (_, _, _) => Task.FromResult(TestFileFactory.CreateAudioProbeJson(1));

            var analytics = new MediaProcessingAnalytics();
            var conversion = await analytics.BuildConversionAsync(
                inputPath,
                System.IO.Path.Combine(tempDir, "out.wav"),
                ProcessingScenario.AiTranscription);

            var command = conversion.Should();
            command.Video.ShouldDisableOutput();
            command.Audio.ShouldUseCodec(AudioCodec.pcm_s16le)
                .ShouldSetSampleRate(16000)
                .ShouldSetChannels(1);
        }

        private sealed class FakeMediaInfo : IMediaInfo
        {
            public FakeMediaInfo(string path, IEnumerable<string> audioCodecs, IEnumerable<string> videoCodecs)
            {
                Path = path;
                AudioStreams = (audioCodecs ?? Enumerable.Empty<string>())
                    .Select((codec, i) => new AudioStream { Codec = codec, Index = i, SampleRate = 48000, Channels = 2 })
                    .Cast<IAudioStream>()
                    .ToArray();
                VideoStreams = (videoCodecs ?? Enumerable.Empty<string>())
                    .Select((codec, i) => new VideoStream { Codec = codec, Index = i, Width = 1920, Height = 1080, Framerate = 30 })
                    .Cast<IVideoStream>()
                    .ToArray();
                SubtitleStreams = Array.Empty<ISubtitleStream>();
            }

            public IEnumerable<IStream> Streams => VideoStreams.Cast<IStream>().Concat(AudioStreams).Concat(SubtitleStreams);

            public string Path { get; }

            public TimeSpan Duration => TimeSpan.FromMinutes(1);

            public DateTime? CreationTime => null;

            public long Size => 1024;

            public IEnumerable<IVideoStream> VideoStreams { get; }

            public IEnumerable<IAudioStream> AudioStreams { get; }

            public IEnumerable<ISubtitleStream> SubtitleStreams { get; }
        }
    }

    internal static class TestFileFactory
    {
        internal static string CreateTempDirectory()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "xabe-tests-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(path);
            return path;
        }

        internal static string CreateFakeMediaFile(string directory, string name)
        {
            var inputPath = System.IO.Path.Combine(directory, name);
            System.IO.File.WriteAllBytes(inputPath, new byte[]
            {
                0x00, 0x00, 0x00, 0x18,
                0x66, 0x74, 0x79, 0x70,
                0x69, 0x73, 0x6F, 0x6D,
                0x00, 0x00, 0x02, 0x00
            });
            return inputPath;
        }

        internal static string CreateFakeExecutable(string directory, string baseName)
        {
            var fileName = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? baseName + ".exe"
                : baseName;
            var path = System.IO.Path.Combine(directory, fileName);

            System.IO.File.WriteAllBytes(path, CreateExecutableBytes());
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                System.IO.File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute);
            }

            return path;
        }

        internal static string CreateAudioProbeJson(int audioStreamCount)
        {
            var audioStreams = string.Empty;
            for (var i = 0; i < audioStreamCount; i++)
            {
                if (i > 0)
                {
                    audioStreams += ",\n";
                }

                audioStreams +=
                    "    {\n" +
                    "      \"codec_name\": \"aac\",\n" +
                    "      \"codec_type\": \"audio\",\n" +
                    "      \"duration\": 2.0,\n" +
                    "      \"bit_rate\": 128000,\n" +
                    "      \"index\": " + (i + 1) + ",\n" +
                    "      \"channels\": 2,\n" +
                    "      \"sample_rate\": 48000,\n" +
                    "      \"disposition\": { \"default\": " + (i == 0 ? 1 : 0) + ", \"forced\": 0 }\n" +
                    "    }";
            }

            return "{\n" +
                   "  \"streams\": [\n" +
                   "    {\n" +
                   "      \"codec_name\": \"h264\",\n" +
                   "      \"codec_type\": \"video\",\n" +
                   "      \"width\": 1280,\n" +
                   "      \"height\": 720,\n" +
                   "      \"r_frame_rate\": \"30/1\",\n" +
                   "      \"duration\": 2.0,\n" +
                   "      \"index\": 0,\n" +
                   "      \"pix_fmt\": \"yuv420p\",\n" +
                   "      \"disposition\": { \"default\": 1, \"forced\": 0 }\n" +
                   "    },\n" +
                   audioStreams + "\n" +
                   "  ],\n" +
                   "  \"format\": {\n" +
                   "    \"format_name\": \"mov,mp4,m4a,3gp,3g2,mj2\",\n" +
                   "    \"size\": \"4096\",\n" +
                   "    \"duration\": 2.0,\n" +
                   "    \"bit_rate\": 928000\n" +
                   "  }\n" +
                   "}";
        }

        private static byte[] CreateExecutableBytes()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return new byte[] { 0x4D, 0x5A, 0x00, 0x00 };
            }

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                return new byte[] { 0xCF, 0xFA, 0xED, 0xFE };
            }

            return new byte[] { 0x7F, 0x45, 0x4C, 0x46 };
        }
    }
}
