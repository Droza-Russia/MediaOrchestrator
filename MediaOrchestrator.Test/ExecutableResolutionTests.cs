using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using MediaOrchestrator.Exceptions;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class ExecutableResolutionTests : IDisposable
    {
        private readonly string _originalFfmpegExecutable = Environment.GetEnvironmentVariable("FFMPEG_EXECUTABLE");
        private readonly string _originalFfprobeExecutable = Environment.GetEnvironmentVariable("FFPROBE_EXECUTABLE");

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("FFMPEG_EXECUTABLE", _originalFfmpegExecutable);
            Environment.SetEnvironmentVariable("FFPROBE_EXECUTABLE", _originalFfprobeExecutable);
            MediaOrchestrator.HardwareAccelerationProfileDetector = GetDefaultHardwareAccelerationProfileDetector();
            MediaOrchestrator.ApplyAutoHardwareAccelerationToConversions = true;
            MediaOrchestrator.SetExecutablesPath(null);
        }

        [Fact]
        public void Initialization_WithConfiguredExecutablesDirectory_ResolvesPaths()
        {
            var tempDir = CreateTempDirectory();
            var ffmpegPath = CreateFakeExecutable(tempDir, "ffmpeg");
            var ffprobePath = CreateFakeExecutable(tempDir, "ffprobe");

            MediaOrchestrator.SetExecutablesPath(tempDir);

            var accessor = new TestFFmpegAccessor();

            Assert.Equal(ffmpegPath, accessor.CurrentFFmpegPath);
            Assert.Equal(ffprobePath, accessor.CurrentFFprobePath);
        }

        [Fact]
        public void Initialization_WithEnvironmentExecutableVariables_ResolvesPaths()
        {
            var tempDir = CreateTempDirectory();
            var ffmpegPath = CreateFakeExecutable(tempDir, "ffmpeg");
            var ffprobePath = CreateFakeExecutable(tempDir, "ffprobe");

            MediaOrchestrator.SetExecutablesPath(null);
            Environment.SetEnvironmentVariable("FFMPEG_EXECUTABLE", ffmpegPath);
            Environment.SetEnvironmentVariable("FFPROBE_EXECUTABLE", ffprobePath);

            var accessor = new TestFFmpegAccessor();

            Assert.Equal(ffmpegPath, accessor.CurrentFFmpegPath);
            Assert.Equal(ffprobePath, accessor.CurrentFFprobePath);
        }

        [Fact]
        public void Initialization_FindsExecutablesInStartupFfmpegBinariesDirectory()
        {
            var startupDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            Assert.False(string.IsNullOrWhiteSpace(startupDirectory));

            var binariesDirectory = Path.Combine(startupDirectory, "ffmpeg-binaries", GetCurrentOsFolderName());
            Directory.CreateDirectory(binariesDirectory);

            var ffmpegPath = CreateFakeExecutable(binariesDirectory, "ffmpeg");
            var ffprobePath = CreateFakeExecutable(binariesDirectory, "ffprobe");

            try
            {
                MediaOrchestrator.SetExecutablesPath(null);

                var accessor = new TestFFmpegAccessor();

                Assert.Equal(ffmpegPath, accessor.CurrentFFmpegPath);
                Assert.Equal(ffprobePath, accessor.CurrentFFprobePath);
            }
            finally
            {
                TryDeleteDirectory(Path.Combine(startupDirectory, "ffmpeg-binaries"));
            }
        }

        [Fact]
        public void Initialization_FindsExecutablesInStartupLegacyFfmpegPackDirectory()
        {
            var startupDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            Assert.False(string.IsNullOrWhiteSpace(startupDirectory));

            var binariesDirectory = Path.Combine(startupDirectory, "ffmpegpack", GetCurrentLegacyPackRid());
            Directory.CreateDirectory(binariesDirectory);

            var ffmpegPath = CreateFakeExecutable(binariesDirectory, "ffmpeg");
            var ffprobePath = CreateFakeExecutable(binariesDirectory, "ffprobe");

            try
            {
                MediaOrchestrator.SetExecutablesPath(null);

                var accessor = new TestFFmpegAccessor();

                Assert.Equal(ffmpegPath, accessor.CurrentFFmpegPath);
                Assert.Equal(ffprobePath, accessor.CurrentFFprobePath);
            }
            finally
            {
                TryDeleteDirectory(Path.Combine(startupDirectory, "ffmpegpack"));
            }
        }

        [Fact]
        public void Initialization_WithMissingConfiguredDirectory_ThrowsFfmpegNotFoundException()
        {
            var missingDir = Path.Combine(Path.GetTempPath(), "media-orchestrator-missing-" + Guid.NewGuid().ToString("N"));
            MediaOrchestrator.SetExecutablesPath(missingDir);

            var exception = Assert.Throws<ToolchainNotFoundException>(() => new TestFFmpegAccessor());

            Assert.Contains("Не удалось найти MediaOrchestrator", exception.Message);
        }

        [Fact]
        public void Initialization_WithInvalidExecutableSignature_ThrowsExecutableSignatureMismatchException()
        {
            var tempDir = CreateTempDirectory();
            File.WriteAllText(Path.Combine(tempDir, "ffmpeg"), "not-an-executable");
            CreateFakeExecutable(tempDir, "ffprobe");

            MediaOrchestrator.SetExecutablesPath(tempDir);

            var exception = Assert.Throws<ExecutableSignatureMismatchException>(() => new TestFFmpegAccessor());

            Assert.Contains(Path.Combine(tempDir, "ffmpeg"), exception.Message);
        }

        [Fact]
        public void Initialization_WithInaccessibleExecutablesDirectory_ThrowsExecutablesPathAccessDeniedException()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var tempDir = CreateTempDirectory();
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");

            try
            {
                File.SetUnixFileMode(tempDir, UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                MediaOrchestrator.SetExecutablesPath(tempDir);

                var exception = Assert.Throws<ExecutablesPathAccessDeniedException>(() => new TestFFmpegAccessor());

                Assert.Contains(tempDir, exception.Message);
            }
            finally
            {
                File.SetUnixFileMode(
                    tempDir,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute);
            }
        }

        [Fact]
        public void EnsureExecutablesLocated_DetectsHardwareAccelerationOnce_AndConversionUsesIt()
        {
            var tempDir = CreateTempDirectory();
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");

            var detectionCalls = 0;
            MediaOrchestrator.HardwareAccelerationProfileDetector = (_, _) =>
            {
                Interlocked.Increment(ref detectionCalls);
                return new HardwareAccelerationProfile("cuda", "h264_cuvid", "h264_nvenc", "hevc_nvenc");
            };

            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaOrchestrator.EnsureExecutablesLocated();
            MediaOrchestrator.EnsureExecutablesLocated();

            Assert.Equal(1, detectionCalls);
            Assert.True(MediaOrchestrator.IsHardwareAccelerationProfileDetected);
            Assert.Equal("cuda", MediaOrchestrator.DetectedHardwareAcceleratorName);
            Assert.Equal("h264_nvenc", MediaOrchestrator.ResolveTranscodeVideoCodecToString(VideoCodec.h264));
            Assert.Equal("hevc_nvenc", MediaOrchestrator.ResolveTranscodeVideoCodecToString(VideoCodec.hevc));

            var video = new VideoStream
            {
                Path = "input.mp4",
                Index = 0
            };
            video.SetCodec(VideoCodec.h264);

            var conversion = new Conversion()
                .AddStream(video)
                .SetOutput("output.mp4");

            var args = conversion.Build();

            Assert.Contains(FFmpegHardwareAccelerationArguments.SetHardwareAcceleration("cuda"), args, StringComparison.Ordinal);
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "media-orchestrator-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static string GetCurrentOsFolderName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macos";
            }

            return "linux";
        }

        private static string GetCurrentLegacyPackRid()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win-x64";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx-x64";
            }

            return "linux-x64";
        }

        private static string CreateFakeExecutable(string directory, string baseName)
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? baseName + ".exe" : baseName;
            var path = Path.Combine(directory, fileName);

            File.WriteAllBytes(path, CreateExecutableBytes());
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

        private static byte[] CreateExecutableBytes()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new byte[] { 0x4D, 0x5A, 0x00, 0x00 };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new byte[] { 0xCF, 0xFA, 0xED, 0xFE };
            }

            return new byte[] { 0x7F, 0x45, 0x4C, 0x46 };
        }

        private static void TryDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Не маскируем результат теста ошибками cleanup.
            }
        }

        private sealed class TestFFmpegAccessor : MediaOrchestrator
        {
            public string CurrentFFmpegPath => FFmpegPath;

            public string CurrentFFprobePath => FFprobePath;
        }

        private static Func<string, CancellationToken, HardwareAccelerationProfile> GetDefaultHardwareAccelerationProfileDetector()
        {
            return (ffmpegPath, cancellationToken) =>
            {
                return HardwareAccelerationAutoDetector.TryDetect(
                    ffmpegPath,
                    new OperatingSystemProvider().GetOperatingSystem(),
                    cancellationToken,
                    out var profile)
                    ? profile
                    : null;
            };
        }
    }
}
