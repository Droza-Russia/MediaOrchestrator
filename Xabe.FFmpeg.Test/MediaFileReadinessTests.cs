using System;
using System.IO;
using System.Threading.Tasks;
using Xabe.FFmpeg.Exceptions;
using Xunit;

namespace Xabe.FFmpeg.Test
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
            var directory = Path.Combine(Path.GetTempPath(), "xabe-readiness-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var exception = await Assert.ThrowsAsync<InvalidInputException>(() =>
                MediaFileReadiness.WaitUntilStableAsync(
                    directory,
                    stabilityQuietPeriod: TimeSpan.FromMilliseconds(10),
                    pollInterval: TimeSpan.FromMilliseconds(10),
                    maximumWait: TimeSpan.FromMilliseconds(50))).ConfigureAwait(false);

            Assert.Contains(directory, exception.Message);
        }
    }
}
