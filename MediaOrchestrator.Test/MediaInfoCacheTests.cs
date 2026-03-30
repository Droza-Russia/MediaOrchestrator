using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class MediaInfoCacheTests : IDisposable
    {
        private readonly Func<MediaProbeRunner, string, CancellationToken, Task<string>> _originalProbeExecutor = MediaProbeRunner.ProbeCommandExecutor;

        public void Dispose()
        {
            MediaProbeRunner.ProbeCommandExecutor = _originalProbeExecutor;
            MediaOrchestrator.ClearMediaInfoCache();
            MediaOrchestrator.ClearMediaAnalysisStore();
            MediaOrchestrator.MediaInfoCacheEnabled = true;
            MediaOrchestrator.SetExecutablesPath(null);
            MediaOrchestrator.MediaAnalysisLearningEnabled = true;
        }

        [Fact]
        public async Task GetMediaInfo_OnCacheMiss_UsesSingleFfprobeInvocation()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);

            var invocationCount = 0;
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) =>
            {
                Interlocked.Increment(ref invocationCount);
                return Task.FromResult(
                    "{\n" +
                    "  \"streams\": [\n" +
                    "    {\n" +
                    "      \"codec_name\": \"h264\",\n" +
                    "      \"width\": 1920,\n" +
                    "      \"height\": 1080,\n" +
                    "      \"codec_type\": \"video\",\n" +
                    "      \"r_frame_rate\": \"25/1\",\n" +
                    "      \"duration\": 2.0,\n" +
                    "      \"bit_rate\": 1000000,\n" +
                    "      \"index\": 0,\n" +
                    "      \"pix_fmt\": \"yuv420p\",\n" +
                    "      \"disposition\": { \"default\": 1, \"forced\": 0 },\n" +
                    "      \"tags\": { \"rotate\": 0 }\n" +
                    "    },\n" +
                    "    {\n" +
                    "      \"codec_name\": \"aac\",\n" +
                    "      \"codec_type\": \"audio\",\n" +
                    "      \"duration\": 2.0,\n" +
                    "      \"bit_rate\": 128000,\n" +
                    "      \"index\": 1,\n" +
                    "      \"channels\": 2,\n" +
                    "      \"sample_rate\": 48000,\n" +
                    "      \"disposition\": { \"default\": 1, \"forced\": 0 }\n" +
                    "    }\n" +
                    "  ],\n" +
                    "  \"format\": {\n" +
                    "    \"format_name\": \"mov,mp4,m4a,3gp,3g2,mj2\",\n" +
                    "    \"size\": \"4096\",\n" +
                    "    \"duration\": 2.0,\n" +
                    "    \"bit_rate\": 1128000,\n" +
                    "    \"tags\": {\n" +
                    "      \"creation_time\": \"2024-01-01T12:00:00Z\"\n" +
                    "    }\n" +
                    "  }\n" +
                    "}");
            };

            var mediaInfo = await MediaOrchestrator.GetMediaInfo(inputPath).ConfigureAwait(false);

            Assert.Equal(1, invocationCount);
            Assert.Single(mediaInfo.VideoStreams);
            Assert.Single(mediaInfo.AudioStreams);
            Assert.Equal(4096, mediaInfo.Size);
        }

        [Fact]
        public async Task GetMediaInfo_OnSecondCall_UsesCacheInsteadOfSecondFfprobeInvocation()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);

            var invocationCount = 0;
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) =>
            {
                Interlocked.Increment(ref invocationCount);
                return Task.FromResult(
                    "{\n" +
                    "  \"streams\": [\n" +
                    "    {\n" +
                    "      \"codec_name\": \"h264\",\n" +
                    "      \"width\": 1280,\n" +
                    "      \"height\": 720,\n" +
                    "      \"codec_type\": \"video\",\n" +
                    "      \"r_frame_rate\": \"30/1\",\n" +
                    "      \"duration\": 1.0,\n" +
                    "      \"bit_rate\": 800000,\n" +
                    "      \"index\": 0,\n" +
                    "      \"pix_fmt\": \"yuv420p\",\n" +
                    "      \"disposition\": { \"default\": 1, \"forced\": 0 }\n" +
                    "    }\n" +
                    "  ],\n" +
                    "  \"format\": {\n" +
                    "    \"format_name\": \"mov,mp4,m4a,3gp,3g2,mj2\",\n" +
                    "    \"size\": \"2048\",\n" +
                    "    \"duration\": 1.0,\n" +
                    "    \"bit_rate\": 800000\n" +
                    "  }\n" +
                    "}");
            };

            var first = await MediaOrchestrator.GetMediaInfo(inputPath).ConfigureAwait(false);
            var second = await MediaOrchestrator.GetMediaInfo(inputPath).ConfigureAwait(false);

            Assert.Equal(1, invocationCount);
            Assert.NotSame(first, second);
            Assert.Equal(first.Duration, second.Duration);
        }

        [Fact]
        public async Task GetMediaInfo_OnConcurrentCalls_UsesSingleFfprobeInvocation()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);

            var invocationCount = 0;
            MediaProbeRunner.ProbeCommandExecutor = async (_, _, _) =>
            {
                Interlocked.Increment(ref invocationCount);
                await Task.Delay(100).ConfigureAwait(false);
                return "{\n" +
                       "  \"streams\": [\n" +
                       "    {\n" +
                       "      \"codec_name\": \"h264\",\n" +
                       "      \"width\": 1280,\n" +
                       "      \"height\": 720,\n" +
                       "      \"codec_type\": \"video\",\n" +
                       "      \"r_frame_rate\": \"30/1\",\n" +
                       "      \"duration\": 1.0,\n" +
                       "      \"bit_rate\": 800000,\n" +
                       "      \"index\": 0,\n" +
                       "      \"pix_fmt\": \"yuv420p\",\n" +
                       "      \"disposition\": { \"default\": 1, \"forced\": 0 }\n" +
                       "    }\n" +
                       "  ],\n" +
                       "  \"format\": {\n" +
                       "    \"format_name\": \"mov,mp4,m4a,3gp,3g2,mj2\",\n" +
                       "    \"size\": \"2048\",\n" +
                       "    \"duration\": 1.0,\n" +
                       "    \"bit_rate\": 800000\n" +
                       "  }\n" +
                       "}";
            };

            var results = await Task.WhenAll(
                MediaOrchestrator.GetMediaInfo(inputPath),
                MediaOrchestrator.GetMediaInfo(inputPath),
                MediaOrchestrator.GetMediaInfo(inputPath)).ConfigureAwait(false);

            Assert.Equal(1, invocationCount);
            Assert.Equal(3, results.Length);
            Assert.All(results, result => Assert.Single(result.VideoStreams));
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "media-orchestrator-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static byte[] CreateIsoBmffHeader()
        {
            return new byte[]
            {
                0x00, 0x00, 0x00, 0x18,
                0x66, 0x74, 0x79, 0x70,
                0x69, 0x73, 0x6F, 0x6D,
                0x00, 0x00, 0x02, 0x00
            };
        }

        private static string CreateFakeExecutable(string directory, string baseName)
        {
            var fileName = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? baseName + ".exe"
                : baseName;
            var path = Path.Combine(directory, fileName);

            File.WriteAllBytes(path, CreateExecutableBytes());
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute);
            }

            return path;
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
