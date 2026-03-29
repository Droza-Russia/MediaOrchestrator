using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    internal static class MediaIoBridge
    {
        private const int MinimumBufferSize = 81920;
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 100;
        private const int MaxCleanupErrors = 1000;

        private static readonly ConcurrentDictionary<string, string> _cleanupErrors = new ConcurrentDictionary<string, string>();
        private static readonly object _cleanupErrorsGate = new object();

        private static string GetIsolatedTempDirectory()
        {
            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var guid = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Path.GetTempPath(), "media-orchestrator-io", "pid" + processId + "_" + guid);
            Directory.CreateDirectory(directory);
            return directory;
        }

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
                    var streamInputPath = await CreateTempFileFromStreamAsync(source, cancellationToken).ConfigureAwait(false);
                    return new PreparedMediaInput(streamInputPath, () => CleanupTempFileAsync(streamInputPath, source.Stream, source.LeaveOpen));

                default:
                    var bytesInputPath = await CreateTempFileFromBytesAsync(source, cancellationToken).ConfigureAwait(false);
                    return new PreparedMediaInput(bytesInputPath, () => CleanupTempFileAsync(bytesInputPath, null, true));
            }
        }

        private static async Task<string> CreateTempFileFromStreamAsync(MediaSource source, CancellationToken cancellationToken)
        {
            var directory = GetIsolatedTempDirectory();
            var extension = source.FileExtension;
            var tempPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + (string.IsNullOrWhiteSpace(extension) ? ".bin" : extension));
            var finalPath = tempPath;

            var bufferSize = Math.Max(MinimumBufferSize, source.BufferSize);
            Exception lastException = null;

            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    string tempWithExt = tempPath + ".tmp";
                    using (var output = new FileStream(tempWithExt, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
                    {
                        await source.Stream.CopyToAsync(output, bufferSize, cancellationToken).ConfigureAwait(false);
                        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (File.Exists(finalPath))
                    {
                        try { File.Delete(finalPath); } catch { }
                    }

                    File.Move(tempWithExt, finalPath);
                    ValidateWrittenFile(finalPath);
                    return finalPath;
                }
                catch (IOException ex)
                {
                    if (attempt < MaxRetryAttempts - 1)
                    {
                        lastException = ex;
                        await Task.Delay(RetryDelayMs, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        LogCleanupError(finalPath, ex);
                        CleanupTempFileOnError(finalPath);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogCleanupError(finalPath, ex);
                    CleanupTempFileOnError(finalPath);
                    throw;
                }
            }

            LogCleanupError(finalPath, lastException);
            throw new IOException("Failed to write stream to temp file after " + MaxRetryAttempts + " attempts", lastException);
        }

        private static async Task<string> CreateTempFileFromBytesAsync(MediaSource source, CancellationToken cancellationToken)
        {
            var directory = GetIsolatedTempDirectory();
            var extension = source.FileExtension;
            var tempPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + (string.IsNullOrWhiteSpace(extension) ? ".bin" : extension));
            var finalPath = tempPath;

            if (source.Data == null || source.Data.Length == 0)
            {
                throw new ArgumentException("Source data cannot be null or empty", nameof(source));
            }

            Exception lastException = null;

            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    string tempWithExt = tempPath + ".tmp";
                    using (var stream = new FileStream(tempWithExt, FileMode.Create, FileAccess.Write, FileShare.None, MinimumBufferSize, useAsync: true))
                    {
                        await stream.WriteAsync(source.Data, 0, source.Data.Length, cancellationToken).ConfigureAwait(false);
                        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (File.Exists(finalPath))
                    {
                        try { File.Delete(finalPath); } catch { }
                    }

                    File.Move(tempWithExt, finalPath);
                    ValidateWrittenFile(finalPath);
                    return finalPath;
                }
                catch (IOException ex)
                {
                    if (attempt < MaxRetryAttempts - 1)
                    {
                        lastException = ex;
                        await Task.Delay(RetryDelayMs, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        LogCleanupError(finalPath, ex);
                        CleanupTempFileOnError(finalPath);
                        throw new IOException("Failed to write bytes to temp file after " + MaxRetryAttempts + " attempts", ex);
                    }
                }
                catch (Exception ex)
                {
                    LogCleanupError(finalPath, ex);
                    CleanupTempFileOnError(finalPath);
                    throw new IOException("Failed to write bytes to temp file after " + MaxRetryAttempts + " attempts", ex);
                }
            }

            LogCleanupError(finalPath, lastException);
            throw new IOException("Failed to write bytes to temp file after " + MaxRetryAttempts + " attempts", lastException);
        }

        private static void ValidateWrittenFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    throw new IOException("Temp file was not created: " + path);
                }

                var info = new FileInfo(path);
                if (info.Length == 0)
                {
                    throw new IOException("Temp file is empty after write: " + path);
                }

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false))
                {
                    var headerBuffer = new byte[4];
                    var bytesRead = stream.Read(headerBuffer, 0, 4);
                    if (bytesRead > 0)
                    {
                        return;
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException("Written file validation failed: " + path, ex);
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
                () => CleanupTempFileAsync(tempOutputPath, destination.Stream, destination.LeaveOpen));
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

            var tempDirectory = GetIsolatedTempDirectory();
            return new PreparedMediaDirectory(
                tempDirectory,
                cancellationToken => FinalizeDirectoryAsync(tempDirectory, destination, cancellationToken),
                () => CleanupTempDirectoryAsync(tempDirectory));
        }

        private static string CreateTempFilePath(string extension = null)
        {
            var directory = GetIsolatedTempDirectory();
            var ext = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
            return Path.Combine(directory, Guid.NewGuid().ToString("N") + ext);
        }

        private static async Task FinalizeOutputAsync(string tempOutputPath, MediaDestination destination, CancellationToken cancellationToken)
        {
            switch (destination.Kind)
            {
                case MediaDestination.DestinationKind.Stream:
                    var bufferSize = Math.Max(MinimumBufferSize, destination.BufferSize);
                    try
                    {
                        using (var input = new FileStream(tempOutputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
                        {
                            await input.CopyToAsync(destination.Stream, bufferSize, cancellationToken).ConfigureAwait(false);
                            await destination.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await CleanupTempFileAsync(tempOutputPath, null, true).ConfigureAwait(false);
                    }
                    break;

                case MediaDestination.DestinationKind.Bytes:
                    try
                    {
                        destination.SetBytes(await ReadFileBytesAsync(tempOutputPath, cancellationToken).ConfigureAwait(false));
                    }
                    finally
                    {
                        await CleanupTempFileAsync(tempOutputPath, null, true).ConfigureAwait(false);
                    }
                    break;
            }
        }

        private static async Task FinalizeDirectoryAsync(string tempDirectoryPath, MediaDirectoryDestination destination, CancellationToken cancellationToken)
        {
            try
            {
                var files = Directory.GetFiles(tempDirectoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var relativePath = file.Substring(tempDirectoryPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    destination.SetFile(relativePath, await ReadFileBytesAsync(file, cancellationToken).ConfigureAwait(false));
                }
            }
            finally
            {
                await CleanupTempDirectoryAsync(tempDirectoryPath).ConfigureAwait(false);
            }
        }

        internal static async Task<byte[]> ReadFileBytesAsync(string path, CancellationToken cancellationToken)
        {
            byte[] buffer = BufferPool.RentDefault();
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, MinimumBufferSize, useAsync: true))
                using (var memory = new MemoryStream())
                {
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await memory.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                    }
                    return memory.ToArray();
                }
            }
            finally
            {
                BufferPool.ReturnDefault(buffer);
            }
        }

        private static Task CleanupTempFileAsync(string path, Stream stream, bool leaveOpen)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        var dirInfo = new DirectoryInfo(directory);
                        var hasFiles = dirInfo.EnumerateFiles().Any();
                        var hasDirs = dirInfo.EnumerateDirectories().Any();
                        if (!hasFiles && !hasDirs)
                        {
                            try
                            {
                                Directory.Delete(directory, recursive: true);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogCleanupError(path, ex);
                }

                if (stream != null && !leaveOpen)
                {
                    try
                    {
                        stream.Dispose();
                    }
                    catch
                    {
                    }
                }
            });
        }

        private static Task CleanupTempDirectoryAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    LogCleanupError(path, ex);
                }
            });
        }

        private static void CleanupTempFileOnError(string path)
        {
            FileHelper.SafeDeleteTempFiles(path);
        }

        private static void LogCleanupError(string path, Exception ex)
        {
            var key = path + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var errorMessage = ex?.GetType().Name + ": " + (ex?.Message ?? "Unknown error");
            if (!_cleanupErrors.ContainsKey(key))
            {
                _cleanupErrors.TryAdd(key, errorMessage);
            }

            if (_cleanupErrors.Count > MaxCleanupErrors)
            {
                lock (_cleanupErrorsGate)
                {
                    if (_cleanupErrors.Count > MaxCleanupErrors)
                    {
                        var keysToRemove = _cleanupErrors.Keys.Take(_cleanupErrors.Count / 2).ToList();
                        foreach (var k in keysToRemove)
                        {
                            _cleanupErrors.TryRemove(k, out _);
                        }
                    }
                }
            }
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