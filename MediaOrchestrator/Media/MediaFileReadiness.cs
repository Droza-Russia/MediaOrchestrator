using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Ожидание появления и стабилизации локального файла (например, пока сторонний сервис дописывает загрузку),
    ///     прежде чем вызывать ffprobe или открывать файл на чтение.
    /// </summary>
    public static class MediaFileReadiness
    {
        /// <summary>
        ///     Интервал «тишины»: два подряд снимка размера и времени изменения совпали с разницей не меньше этого значения.
        /// </summary>
        public static readonly TimeSpan DefaultStabilityQuietPeriod = TimeSpan.FromMilliseconds(400);

        /// <summary>
        ///     Опрос, пока файла ещё нет по пути.
        /// </summary>
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        ///     Максимальное время ожидания появления и стабилизации с момента вызова.
        /// </summary>
        public static readonly TimeSpan DefaultMaximumWait = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     Для не-файловых URI (http и т.д.) сразу завершается успешно. Для локального пути ждёт появления файла
        ///     и двух совпадающих снимков (размер + время записи) с интервалом <paramref name="stabilityQuietPeriod"/>.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <param name="stabilityQuietPeriod">Минимальный интервал, в течение которого размер и время изменения не менялись.</param>
        /// <param name="pollInterval">Интервал опроса, пока файл ещё не создан.</param>
        /// <param name="maximumWait">Общий предел ожидания.</param>
        /// <param name="cancellationToken">Отмена.</param>
        /// <exception cref="TimeoutException">Истёк <paramref name="maximumWait"/>.</exception>
        /// <exception cref="InvalidInputException">Путь указывает на каталог.</exception>
        public static async Task WaitUntilStableAsync(
            string filePath,
            TimeSpan? stabilityQuietPeriod = null,
            TimeSpan? pollInterval = null,
            TimeSpan? maximumWait = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(ErrorMessages.PathMustNotBeEmpty, nameof(filePath));
            }

            if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ErrorMessages.InvalidFilePath, nameof(filePath), ex);
            }

            var quiet = stabilityQuietPeriod ?? DefaultStabilityQuietPeriod;
            var poll = pollInterval ?? DefaultPollInterval;
            var maxWait = maximumWait ?? DefaultMaximumWait;
            if (quiet <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(stabilityQuietPeriod));
            }

            if (poll <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(pollInterval));
            }

            if (maxWait <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumWait));
            }

            var deadlineUtc = DateTime.UtcNow + maxWait;
            var fileSeen = false;
            var observedChanges = false;

            if (Directory.Exists(fullPath))
            {
                throw new InvalidInputException(string.Format(ErrorMessages.InputPathIsNotAFile, fullPath));
            }

            while (!File.Exists(fullPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadlineUtc)
                {
                    throw new TimeoutException(string.Format(ErrorMessages.MediaFileStableWaitTimeout, fullPath, maxWait));
                }

                await Task.Delay(poll, cancellationToken).ConfigureAwait(false);
            }

            fileSeen = true;
            if (Directory.Exists(fullPath))
            {
                throw new InvalidInputException(string.Format(ErrorMessages.InputPathIsNotAFile, fullPath));
            }

            while (DateTime.UtcNow < deadlineUtc)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryReadSnapshot(fullPath, out var before))
                {
                    await Task.Delay(poll, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await Task.Delay(quiet, cancellationToken).ConfigureAwait(false);

                if (!TryReadSnapshot(fullPath, out var after))
                {
                    continue;
                }

                if (before.Equals(after))
                {
                    return;
                }

                observedChanges = true;
            }

            if (fileSeen && observedChanges)
            {
                throw new InputFileStillBeingWrittenException(string.Format(ErrorMessages.InputFileIsStillBeingWritten, fullPath, maxWait));
            }

            throw new TimeoutException(string.Format(ErrorMessages.MediaFileStableWaitTimeout, fullPath, maxWait));
        }

        private readonly struct FileSnapshot : IEquatable<FileSnapshot>
        {
            internal FileSnapshot(long length, long lastWriteTimeUtcTicks)
            {
                Length = length;
                LastWriteTimeUtcTicks = lastWriteTimeUtcTicks;
            }

            internal long Length { get; }

            internal long LastWriteTimeUtcTicks { get; }

            public bool Equals(FileSnapshot other) =>
                Length == other.Length && LastWriteTimeUtcTicks == other.LastWriteTimeUtcTicks;
        }

        private static bool TryReadSnapshot(string fullPath, out FileSnapshot snapshot)
        {
            snapshot = default;
            try
            {
                if (!File.Exists(fullPath))
                {
                    return false;
                }

                var info = new FileInfo(fullPath);
                if (!info.Exists)
                {
                    return false;
                }

                if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    return false;
                }

                snapshot = new FileSnapshot(info.Length, info.LastWriteTimeUtc.Ticks);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                throw new InputPathAccessDeniedException(string.Format(ErrorMessages.InputPathAccessDenied, fullPath));
            }
        }
    }
}
