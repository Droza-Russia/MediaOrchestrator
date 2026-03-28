using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaOrchestrator
{
    internal static class MediaIoBridge
    {
        internal static async Task<PreparedMediaInput> PrepareInputAsync(MediaSource source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            switch (source.Kind)
            {
                case MediaSource.SourceKind.File:
                    return new PreparedMediaInput(source.Path, null);

                case MediaSource.SourceKind.Stream:
                    var streamInputPath = CreateTempFilePath(source.FileExtension);
                    await CopyToFileAsync(source.Stream, streamInputPath, source.BufferSize, cancellationToken).ConfigureAwait(false);
                    return new PreparedMediaInput(streamInputPath, () => CleanupTempFile(streamInputPath, source.Stream, source.LeaveOpen));

                default:
                    var bytesInputPath = CreateTempFilePath(source.FileExtension);
                    await WriteBytesAsync(bytesInputPath, source.Data, cancellationToken).ConfigureAwait(false);
                    return new PreparedMediaInput(bytesInputPath, () => CleanupTempFile(bytesInputPath, null, true));
            }
        }

        internal static PreparedMediaOutput PrepareOutput(MediaDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Kind == MediaDestination.DestinationKind.File)
            {
                return new PreparedMediaOutput(destination.Path, null, null);
            }

            var tempOutputPath = CreateTempFilePath(destination.FileExtension);
            return new PreparedMediaOutput(
                tempOutputPath,
                cancellationToken => FinalizeOutputAsync(tempOutputPath, destination, cancellationToken),
                () => CleanupTempFile(tempOutputPath, destination.Stream, destination.LeaveOpen));
        }

        internal static PreparedMediaDirectory PrepareDirectoryOutput(MediaDirectoryDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Kind == MediaDirectoryDestination.DirectoryDestinationKind.FileSystem)
            {
                Directory.CreateDirectory(destination.DirectoryPath);
                return new PreparedMediaDirectory(destination.DirectoryPath, _ => Task.CompletedTask, () => Task.CompletedTask);
            }

            var tempDirectory = CreateTempWorkDirectoryPath();
            return new PreparedMediaDirectory(
                tempDirectory,
                cancellationToken => FinalizeDirectoryAsync(tempDirectory, destination, cancellationToken),
                () => CleanupTempDirectory(tempDirectory));
        }

        private static async Task FinalizeOutputAsync(string tempOutputPath, MediaDestination destination, CancellationToken cancellationToken)
        {
            switch (destination.Kind)
            {
                case MediaDestination.DestinationKind.Stream:
                    using (var input = new FileStream(tempOutputPath, FileMode.Open, FileAccess.Read, FileShare.Read, destination.BufferSize, useAsync: true))
                    {
                        await input.CopyToAsync(destination.Stream, destination.BufferSize, cancellationToken).ConfigureAwait(false);
                        await destination.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    break;

                case MediaDestination.DestinationKind.Bytes:
                    destination.SetBytes(await ReadFileBytesAsync(tempOutputPath, cancellationToken).ConfigureAwait(false));
                    break;
            }
        }

        private static async Task CopyToFileAsync(Stream input, string outputPath, int bufferSize, CancellationToken cancellationToken)
        {
            using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            {
                await input.CopyToAsync(output, bufferSize, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static string CreateTempFilePath(string extension)
        {
            var directory = CreateTempDirectoryPath();
            return Path.Combine(directory, Guid.NewGuid().ToString("N") + (string.IsNullOrWhiteSpace(extension) ? ".bin" : extension));
        }

        private static string CreateTempDirectoryPath()
        {
            var directory = Path.Combine(Path.GetTempPath(), "xabe-media-io");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string CreateTempWorkDirectoryPath()
        {
            var directory = Path.Combine(CreateTempDirectoryPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static async Task FinalizeDirectoryAsync(string tempDirectoryPath, MediaDirectoryDestination destination, CancellationToken cancellationToken)
        {
            foreach (var file in Directory.GetFiles(tempDirectoryPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(tempDirectoryPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                destination.SetFile(relativePath, await ReadFileBytesAsync(file, cancellationToken).ConfigureAwait(false));
            }
        }

        internal static async Task<byte[]> ReadFileBytesAsync(string path, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory, 81920, cancellationToken).ConfigureAwait(false);
                return memory.ToArray();
            }
        }

        private static async Task WriteBytesAsync(string path, byte[] data, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task CleanupTempFile(string path, Stream stream, bool leaveOpen)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }

            if (stream != null && !leaveOpen)
            {
                stream.Dispose();
            }

            return Task.CompletedTask;
        }

        private static Task CleanupTempDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
            }

            return Task.CompletedTask;
        }

        internal sealed class PreparedMediaInput
        {
            internal PreparedMediaInput(string path, Func<Task> cleanupAsync)
            {
                Path = path;
                CleanupAsync = cleanupAsync ?? (() => Task.CompletedTask);
            }

            internal string Path { get; }

            internal Func<Task> CleanupAsync { get; }
        }

        internal sealed class PreparedMediaOutput
        {
            internal PreparedMediaOutput(string path, Func<CancellationToken, Task> finalizeAsync, Func<Task> cleanupAsync)
            {
                Path = path;
                FinalizeAsync = finalizeAsync ?? (_ => Task.CompletedTask);
                CleanupAsync = cleanupAsync ?? (() => Task.CompletedTask);
            }

            internal string Path { get; }

            internal Func<CancellationToken, Task> FinalizeAsync { get; }

            internal Func<Task> CleanupAsync { get; }
        }

        internal sealed class PreparedMediaDirectory
        {
            internal PreparedMediaDirectory(string path, Func<CancellationToken, Task> finalizeAsync, Func<Task> cleanupAsync)
            {
                Path = path;
                FinalizeAsync = finalizeAsync ?? (_ => Task.CompletedTask);
                CleanupAsync = cleanupAsync ?? (() => Task.CompletedTask);
            }

            internal string Path { get; }

            internal Func<CancellationToken, Task> FinalizeAsync { get; }

            internal Func<Task> CleanupAsync { get; }
        }
    }
}
