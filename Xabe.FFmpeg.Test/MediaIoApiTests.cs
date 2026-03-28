using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Test.TestSupport;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class MediaIoApiTests : IDisposable
    {
        private readonly Func<MediaProbeRunner, string, System.Threading.CancellationToken, Task<string>> _originalProbeExecutor = MediaProbeRunner.ProbeCommandExecutor;

        public void Dispose()
        {
            MediaProbeRunner.ProbeCommandExecutor = _originalProbeExecutor;
            MediaOrchestrator.ClearMediaInfoCache();
            MediaOrchestrator.SetExecutablesPath(null);
        }

        [Fact]
        public async Task GetMediaInfo_FromBytes_Works()
        {
            var tempDir = TestFileFactory.CreateTempDirectory();
            TestFileFactory.CreateFakeExecutable(tempDir, "ffmpeg");
            TestFileFactory.CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(TestFileFactory.CreateAudioProbeJson(1));

            var mediaInfo = await MediaOrchestrator.GetMediaInfo(
                MediaSource.FromBytes(CreateMp4Bytes(), ".mp4")).ConfigureAwait(false);

            Assert.Single(mediaInfo.VideoStreams);
            Assert.Single(mediaInfo.AudioStreams);
        }

        [Fact]
        public async Task ToMp4_FromBytes_ToBytes_BuildsConversion()
        {
            var tempDir = TestFileFactory.CreateTempDirectory();
            TestFileFactory.CreateFakeExecutable(tempDir, "ffmpeg");
            TestFileFactory.CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(TestFileFactory.CreateAudioProbeJson(1));

            var conversion = await MediaOrchestrator.Conversions.FromSnippet.ToMp4(
                MediaSource.FromBytes(CreateMp4Bytes(), ".mp4"),
                MediaDestination.ToBytes(".mp4")).ConfigureAwait(false);

            var command = conversion.Should();
            command.Audio.ShouldUseCodec(AudioCodec.aac);
            Assert.EndsWith(".mp4", conversion.OutputFilePath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StreamFromStdin_ToBytesDestination_BuildsConversion()
        {
            var output = MediaDestination.ToBytes(".mkv");
            using (var input = new MemoryStream(CreateMp4Bytes()))
            {
                var conversion = MediaOrchestrator.Conversions.FromSnippet.StreamFromStdin(input, output, Format.matroska);
                var command = conversion.Should();
                command.Container.ShouldUseOutputFormat(Format.matroska);
                Assert.NotNull(conversion.OutputFilePath);
                Assert.EndsWith(".mkv", conversion.OutputFilePath, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task SplitAudioByTimecodes_FromBytes_ToMemoryDirectory_BuildsConversions()
        {
            var tempDir = TestFileFactory.CreateTempDirectory();
            TestFileFactory.CreateFakeExecutable(tempDir, "ffmpeg");
            TestFileFactory.CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(TestFileFactory.CreateAudioProbeJson(6));

            var outputDirectory = MediaDirectoryDestination.ToMemory();
            var conversions = await MediaOrchestrator.Conversions.FromSnippet.SplitAudioByTimecodes(
                MediaSource.FromBytes(CreateMp4Bytes(), ".mp4"),
                outputDirectory,
                new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1) },
                cancellationToken: CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(2, conversions.Count);
            Assert.All(conversions, conversion => Assert.EndsWith(".mp3", conversion.OutputFilePath, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SaveM3U8Stream_ToMemoryDirectory_BuildsConversion()
        {
            var tempDir = TestFileFactory.CreateTempDirectory();
            TestFileFactory.CreateFakeExecutable(tempDir, "ffmpeg");
            TestFileFactory.CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(TestFileFactory.CreateAudioProbeJson(6));

            var conversion = await MediaOrchestrator.Conversions.FromSnippet.SaveM3U8Stream(
                new Uri("https://example.com/live.m3u8"),
                MediaDirectoryDestination.ToMemory(),
                "playlist.m3u8",
                TimeSpan.FromSeconds(5),
                CancellationToken.None).ConfigureAwait(false);

            Assert.EndsWith("playlist.m3u8", conversion.OutputFilePath, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] CreateMp4Bytes()
        {
            return new byte[]
            {
                0x00, 0x00, 0x00, 0x18,
                0x66, 0x74, 0x79, 0x70,
                0x69, 0x73, 0x6F, 0x6D,
                0x00, 0x00, 0x02, 0x00
            };
        }
    }
}
