using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Test.TestSupport;
using MediaOrchestrator.Exceptions;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class HostedVideoDownloadTests : IDisposable
    {
        public void Dispose()
        {
            MediaOrchestrator.SetExecutablesPath(null);
        }

        [Fact]
        public async Task DownloadHostedVideoAsync_UsesDefaultFormatAndConfiguredFfmpegLocation()
        {
            var tempDir = CreateTempDirectory();
            var argsFile = Path.Combine(tempDir, "args.txt");
            var downloaderPath = CreateDownloaderScript(
                tempDir,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? $"@echo off{Environment.NewLine}> \"{argsFile}\" ({Environment.NewLine}for %%i in (%*) do @echo %%~i{Environment.NewLine}){Environment.NewLine}exit /b 0{Environment.NewLine}"
                    : $"#!/bin/sh\nprintf '%s\\n' \"$@\" > \"{argsFile}\"\nexit 0\n");

            MediaOrchestrator.SetExecutablesPath(tempDir);

            await MediaOrchestrator.DownloadHostedVideoAsync(
                    "https://example.com/video",
                    Path.Combine(tempDir, "output.mp4"),
                    new HostedVideoDownloadSettings
                    {
                        DownloaderPath = downloaderPath,
                        Format = " ",
                        AdditionalArguments = new[] { "--no-mtime" }
                    })
                .ConfigureAwait(false);

            var args = await File.ReadAllLinesAsync(argsFile).ConfigureAwait(false);
            args.Should()
                .ShouldUseDefaultDownloadFormat()
                .ShouldIncludeFfmpegLocation(tempDir)
                .ShouldIncludeArgument("--no-mtime")
                .ShouldIncludeArgument("https://example.com/video");
        }

        [Fact]
        public async Task DownloadHostedVideoAsync_WhenCancelled_ThrowsOperationCanceledException()
        {
            var tempDir = CreateTempDirectory();
            var downloaderPath = CreateDownloaderScript(
                tempDir,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "@echo off\r\nping 127.0.0.1 -n 20 > nul\r\nexit /b 0\r\n"
                    : "#!/bin/sh\nsleep 10\nexit 0\n");

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                MediaOrchestrator.DownloadHostedVideoAsync(
                    "https://example.com/video",
                    Path.Combine(tempDir, "output.mp4"),
                    new HostedVideoDownloadSettings
                    {
                        DownloaderPath = downloaderPath
                    },
                    cts.Token)).ConfigureAwait(false);
        }

        private static string CreateDownloaderScript(string tempDir, string content)
        {
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            var path = Path.Combine(tempDir, "fake-downloader" + extension);
            File.WriteAllText(path, content);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute);
            }

            return path;
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "xabe-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
