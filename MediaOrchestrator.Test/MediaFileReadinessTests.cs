using System;
using System.IO;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class MediaFileReadinessTests
    {
        [Fact]
        public async Task WaitUntilStableAsync_ReturnsImmediately_ForRemoteUri()
        {
            await MediaFileReadiness.WaitUntilStableAsync("https://example.com/video.m3u8").ConfigureAwait(false);
        }

        [Fact]
        public async Task WaitUntilStableAsync_Throws_ForDirectoryPath()
        {
            var directory = Path.Combine(Path.GetTempPath(), "media-orchestrator-readiness-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var exception = await Assert.ThrowsAsync<InvalidInputException>(() =>
                MediaFileReadiness.WaitUntilStableAsync(
                    directory,
                    stabilityQuietPeriod: TimeSpan.FromMilliseconds(10),
                    pollInterval: TimeSpan.FromMilliseconds(10),
                    maximumWait: TimeSpan.FromMilliseconds(50))).ConfigureAwait(false);

            Assert.Contains(directory, exception.Message);
        }

        [Fact]
        public async Task WaitUntilStableAsync_ThrowsInputFileStillBeingWritten_WhenFileKeepsChanging()
        {
            var directory = Path.Combine(Path.GetTempPath(), "media-orchestrator-readiness-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "growing.mp4");
            File.WriteAllBytes(path, new byte[] { 0x00 });

            var writer = Task.Run(async () =>
            {
                for (var i = 0; i < 8; i++)
                {
                    await File.AppendAllTextAsync(path, "x").ConfigureAwait(false);
                    await Task.Delay(20).ConfigureAwait(false);
                }
            });

            var exception = await Assert.ThrowsAsync<InputFileStillBeingWrittenException>(() =>
                MediaFileReadiness.WaitUntilStableAsync(
                    path,
                    stabilityQuietPeriod: TimeSpan.FromMilliseconds(50),
                    pollInterval: TimeSpan.FromMilliseconds(10),
                    maximumWait: TimeSpan.FromMilliseconds(120))).ConfigureAwait(false);

            await writer.ConfigureAwait(false);
            Assert.Contains(path, exception.Message);
        }
    }
}
