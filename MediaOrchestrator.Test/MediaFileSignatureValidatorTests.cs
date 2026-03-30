using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class MediaFileSignatureValidatorTests
    {
        [Fact]
        public async Task ValidateOrThrowAsync_ThrowsInvalidInput_ForDirectoryPath()
        {
            var directory = CreateTempDirectory();

            var exception = await Assert.ThrowsAsync<InvalidInputException>(() =>
                MediaFileSignatureValidator.ValidateOrThrowAsync(directory)).ConfigureAwait(false);

            Assert.Contains(directory, exception.Message);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_ThrowsInvalidInput_ForMissingFile()
        {
            var missingPath = Path.Combine(Path.GetTempPath(), "media-orchestrator-missing-" + Guid.NewGuid().ToString("N") + ".mp4");

            var exception = await Assert.ThrowsAsync<InvalidInputException>(() =>
                MediaFileSignatureValidator.ValidateOrThrowAsync(missingPath)).ConfigureAwait(false);

            Assert.Contains(missingPath, exception.Message);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_ThrowsSignatureMismatch_ForWrongHeader()
        {
            var tempDir = CreateTempDirectory();
            var path = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllText(path, "this is not an mp4 header");

            var exception = await Assert.ThrowsAsync<InputFileSignatureMismatchException>(() =>
                MediaFileSignatureValidator.ValidateOrThrowAsync(path)).ConfigureAwait(false);

            Assert.Contains(path, exception.Message);
            Assert.Contains(".mp4", exception.Message);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_ThrowsInputFileEmpty_ForEmptyKnownMediaFile()
        {
            var tempDir = CreateTempDirectory();
            var path = Path.Combine(tempDir, "empty.mp4");
            File.WriteAllBytes(path, Array.Empty<byte>());

            var exception = await Assert.ThrowsAsync<InputFileEmptyException>(() =>
                MediaFileSignatureValidator.ValidateOrThrowAsync(path)).ConfigureAwait(false);

            Assert.Contains(path, exception.Message);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_ThrowsInputFileLocked_WhenFileIsLocked()
        {
            var tempDir = CreateTempDirectory();
            var path = Path.Combine(tempDir, "locked.mp4");
            File.WriteAllBytes(path, CreateIsoBmffHeader());

            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var exception = await Assert.ThrowsAsync<InputFileLockedException>(() =>
                    MediaFileSignatureValidator.ValidateOrThrowAsync(path)).ConfigureAwait(false);

                Assert.Contains(path, exception.Message);
            }
        }

        [Fact]
        public async Task ValidateOrThrowAsync_ThrowsInputPathAccessDenied_WhenFilePermissionsDenyRead()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var tempDir = CreateTempDirectory();
            var path = Path.Combine(tempDir, "restricted.mp4");
            File.WriteAllBytes(path, CreateIsoBmffHeader());

            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserWrite);

                var exception = await Assert.ThrowsAsync<InputPathAccessDeniedException>(() =>
                    MediaFileSignatureValidator.ValidateOrThrowAsync(path)).ConfigureAwait(false);

                Assert.Contains(path, exception.Message);
            }
            finally
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite);
            }
        }

        [Fact]
        public async Task ValidateOrThrowAsync_AllowsRemoteUri()
        {
            await MediaFileSignatureValidator.ValidateOrThrowAsync("https://example.com/video.mp4").ConfigureAwait(false);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_AllowsRecognizedHeader()
        {
            var tempDir = CreateTempDirectory();
            var path = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(path, CreateIsoBmffHeader());

            await MediaFileSignatureValidator.ValidateOrThrowAsync(path).ConfigureAwait(false);
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
    }
}
