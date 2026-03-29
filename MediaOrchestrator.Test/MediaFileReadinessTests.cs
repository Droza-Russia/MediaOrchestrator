using System;
using System.IO;
using System.Threading;
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

            using var writerCancellation = new CancellationTokenSource();
            var firstGrowthObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var writer = Task.Run(async () =>
            {
                while (!writerCancellation.Token.IsCancellationRequested)
                {
                    using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        await stream.WriteAsync(new byte[] { 0x01 }, 0, 1, writerCancellation.Token).ConfigureAwait(false);
                        await stream.FlushAsync(writerCancellation.Token).ConfigureAwait(false);
                    }

                    if (new FileInfo(path).Length > 1)
                    {
                        firstGrowthObserved.TrySetResult(true);
                    }

                    await Task.Delay(1, writerCancellation.Token).ConfigureAwait(false);
                }
            }, writerCancellation.Token);

            var firstGrowthCompleted = await Task.WhenAny(
                firstGrowthObserved.Task,
                writer,
                Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);

            if (firstGrowthCompleted != firstGrowthObserved.Task)
            {
                writerCancellation.Cancel();

                if (firstGrowthCompleted == writer)
                {
                    await writer.ConfigureAwait(false);
                }

                throw new Xunit.Sdk.XunitException("The background writer did not grow the test file before the timeout elapsed.");
            }

            var exception = await Assert.ThrowsAsync<InputFileStillBeingWrittenException>(() =>
                MediaFileReadiness.WaitUntilStableAsync(
                    path,
                    stabilityQuietPeriod: TimeSpan.FromMilliseconds(25),
                    pollInterval: TimeSpan.FromMilliseconds(2),
                    maximumWait: TimeSpan.FromMilliseconds(200))).ConfigureAwait(false);

            writerCancellation.Cancel();
            try
            {
                await writer.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            Assert.Contains(path, exception.Message);
        }
    }
}
