using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    internal static class MediaFileSignatureValidator
    {
        private const int HeaderReadLength = 4096;

        /// <summary>
        ///     Максимальное время чтения заголовка при отсутствии внешней отмены (защита от FIFO и зависаний).
        /// </summary>
        private static readonly TimeSpan DefaultHeaderReadTimeout = TimeSpan.FromSeconds(30);

        private static readonly Dictionary<string, MediaSignatureKind[]> ExtensionMap =
            new Dictionary<string, MediaSignatureKind[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".mp3"] = new[] { MediaSignatureKind.Mp3 },
                [".aac"] = new[] { MediaSignatureKind.AacAdts },
                [".wav"] = new[] { MediaSignatureKind.Wav },
                [".flac"] = new[] { MediaSignatureKind.Flac },
                [".ogg"] = new[] { MediaSignatureKind.Ogg },
                [".oga"] = new[] { MediaSignatureKind.Ogg },
                [".ogv"] = new[] { MediaSignatureKind.Ogg },
                [".opus"] = new[] { MediaSignatureKind.Ogg },
                [".webm"] = new[] { MediaSignatureKind.Ebml },
                [".mkv"] = new[] { MediaSignatureKind.Ebml },
                [".mp4"] = new[] { MediaSignatureKind.IsoBmff },
                [".m4a"] = new[] { MediaSignatureKind.IsoBmff },
                [".m4v"] = new[] { MediaSignatureKind.IsoBmff },
                [".mov"] = new[] { MediaSignatureKind.IsoBmff },
                [".3gp"] = new[] { MediaSignatureKind.IsoBmff },
                [".avi"] = new[] { MediaSignatureKind.Avi },
                [".flv"] = new[] { MediaSignatureKind.Flv },
                [".asf"] = new[] { MediaSignatureKind.Asf },
                [".wmv"] = new[] { MediaSignatureKind.Asf },
                [".mpeg"] = new[] { MediaSignatureKind.MpegPs },
                [".mpg"] = new[] { MediaSignatureKind.MpegPs },
                [".ts"] = new[] { MediaSignatureKind.MpegTs },
                [".m2ts"] = new[] { MediaSignatureKind.MpegTs }
            };

        private static readonly Dictionary<string, string[]> ExtensionToFfprobeAliases =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".mkv"] = new[] { "matroska", "webm" },
                [".webm"] = new[] { "webm", "matroska" },
                [".ts"] = new[] { "mpegts" },
                [".m2ts"] = new[] { "mpegts" },
                [".mpg"] = new[] { "mpeg" },
                [".mpeg"] = new[] { "mpeg" },
                [".wmv"] = new[] { "asf" },
                [".oga"] = new[] { "ogg" },
                [".ogv"] = new[] { "ogg" },
                [".opus"] = new[] { "ogg" },
                [".aac"] = new[] { "aac", "adts" }
            };

        internal static void ValidateOrThrow(string filePath)
        {
            try
            {
                ValidateOrThrowAsync(filePath, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                var fullPath = TryGetResolvedLocalPath(filePath);
                throw new IOException(
                    string.Format(ErrorMessages.MediaFileHeaderReadTimeout, fullPath ?? filePath ?? string.Empty));
            }
        }

        internal static async Task ValidateOrThrowAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return;
            }

            var fullPath = Path.GetFullPath(filePath);
            EnsureLocalPathIsSafeReadableFile(fullPath);

            var extension = Path.GetExtension(fullPath);
            if (string.IsNullOrWhiteSpace(extension) || !ExtensionMap.TryGetValue(extension, out var expectedKinds))
            {
                return;
            }

            using (var timeoutCts = new CancellationTokenSource(DefaultHeaderReadTimeout))
            {
                if (!cancellationToken.CanBeCanceled)
                {
                    await ValidateHeaderMatchesExtensionAsync(fullPath, extension, expectedKinds, timeoutCts.Token, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    {
                        await ValidateHeaderMatchesExtensionAsync(fullPath, extension, expectedKinds, linked.Token, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private static async Task ValidateHeaderMatchesExtensionAsync(
            string fullPath,
            string extension,
            MediaSignatureKind[] expectedKinds,
            CancellationToken readToken,
            CancellationToken userToken)
        {
            MediaSignatureKind detectedKind;
            try
            {
                detectedKind = await DetectSignatureKindAsync(fullPath, readToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (userToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new IOException(string.Format(ErrorMessages.MediaFileHeaderReadTimeout, fullPath));
            }
            catch (UnauthorizedAccessException)
            {
                throw new InputPathAccessDeniedException(string.Format(ErrorMessages.InputPathAccessDenied, fullPath));
            }
            catch (IOException ex)
            {
                throw CreateInputReadException(fullPath, ex);
            }

            if (!Contains(expectedKinds, detectedKind))
            {
                throw new InputFileSignatureMismatchException(
                    string.Format(ErrorMessages.InputFileSignatureMismatch, fullPath, extension));
            }
        }

        private static string TryGetResolvedLocalPath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return null;
                }

                if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && !uri.IsFile)
                {
                    return null;
                }

                return Path.GetFullPath(filePath);
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureLocalPathIsSafeReadableFile(string fullPath)
        {
            if (Directory.Exists(fullPath))
            {
                throw new InvalidInputException(string.Format(ErrorMessages.InputPathIsNotAFile, fullPath));
            }

            if (!File.Exists(fullPath))
            {
                throw new InvalidInputException(string.Format(ErrorMessages.InputFileDoesNotExist, fullPath));
            }

            FileAttributes attributes;
            try
            {
                attributes = File.GetAttributes(fullPath);
            }
            catch (IOException ex)
            {
                throw CreateInputReadException(fullPath, ex);
            }
            catch (UnauthorizedAccessException)
            {
                throw new InputPathAccessDeniedException(string.Format(ErrorMessages.InputPathAccessDenied, fullPath));
            }

            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                throw new InvalidInputException(string.Format(ErrorMessages.InputPathIsNotAFile, fullPath));
            }

            var info = new FileInfo(fullPath);
            if (ExtensionMap.ContainsKey(Path.GetExtension(fullPath)) && info.Exists && info.Length == 0)
            {
                throw new InputFileEmptyException(string.Format(ErrorMessages.InputFileIsEmpty, fullPath));
            }
        }

        internal static void ValidateDeclaredFormatOrThrow(string filePath, string detectedFormatName)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(detectedFormatName))
            {
                return;
            }

            if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return;
            }

            var extension = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return;
            }

            var ext = extension.TrimStart('.').ToLowerInvariant();
            var formats = detectedFormatName.Split(',');
            for (var i = 0; i < formats.Length; i++)
            {
                var token = formats[i].Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (token == ext || token.Contains(ext))
                {
                    return;
                }
            }

            if (ExtensionToFfprobeAliases.TryGetValue(extension, out var aliases))
            {
                for (var i = 0; i < formats.Length; i++)
                {
                    var token = formats[i].Trim().ToLowerInvariant();
                    for (var j = 0; j < aliases.Length; j++)
                    {
                        if (token == aliases[j] || token.Contains(aliases[j]))
                        {
                            return;
                        }
                    }
                }

                throw new InputFileSignatureMismatchException(
                    string.Format(ErrorMessages.InputFileSignatureMismatch, filePath, extension));
            }
        }

        private static bool Contains(MediaSignatureKind[] items, MediaSignatureKind value)
        {
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<MediaSignatureKind> DetectSignatureKindAsync(string filePath, CancellationToken cancellationToken)
        {
            var buffer = new byte[HeaderReadLength];
            int read;
            using (var stream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       buffer.Length,
                       FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            }

            return ClassifySignature(buffer, read);
        }

        private static InvalidInputException CreateInputReadException(string fullPath, IOException ex)
        {
            if (IsLikelyAccessDenied(ex))
            {
                return new InputPathAccessDeniedException(string.Format(ErrorMessages.InputPathAccessDenied, fullPath));
            }

            if (IsLikelyFileLock(ex))
            {
                return new InputFileLockedException(string.Format(ErrorMessages.InputFileIsLocked, fullPath));
            }

            return new InputFileUnreadableException(string.Format(ErrorMessages.InputFileIsUnreadable, fullPath));
        }

        private static bool IsLikelyAccessDenied(IOException ex)
        {
            const int accessDenied = 5;
            var lowWord = ex.HResult & 0xFFFF;
            if (lowWord == accessDenied)
            {
                return true;
            }

            var message = ex.Message ?? string.Empty;
            return message.IndexOf("access to the path", StringComparison.OrdinalIgnoreCase) >= 0
                   || message.IndexOf("access is denied", StringComparison.OrdinalIgnoreCase) >= 0
                   || message.IndexOf("permission denied", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLikelyFileLock(IOException ex)
        {
            const int sharingViolation = 32;
            const int lockViolation = 33;
            var lowWord = ex.HResult & 0xFFFF;
            if (lowWord == sharingViolation || lowWord == lockViolation)
            {
                return true;
            }

            var message = ex.Message ?? string.Empty;
            return message.IndexOf("used by another process", StringComparison.OrdinalIgnoreCase) >= 0
                   || message.IndexOf("sharing violation", StringComparison.OrdinalIgnoreCase) >= 0
                   || message.IndexOf("resource busy", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static MediaSignatureKind ClassifySignature(byte[] buffer, int n)
        {
            if (IsIsoBmff(buffer, n)) return MediaSignatureKind.IsoBmff;
            if (IsEbml(buffer, n)) return MediaSignatureKind.Ebml;
            if (IsOgg(buffer, n)) return MediaSignatureKind.Ogg;
            if (IsFlac(buffer, n)) return MediaSignatureKind.Flac;
            if (IsWav(buffer, n)) return MediaSignatureKind.Wav;
            if (IsAvi(buffer, n)) return MediaSignatureKind.Avi;
            if (IsFlv(buffer, n)) return MediaSignatureKind.Flv;
            if (IsAsf(buffer, n)) return MediaSignatureKind.Asf;
            if (IsMpegPs(buffer, n)) return MediaSignatureKind.MpegPs;
            if (IsMpegTs(buffer, n)) return MediaSignatureKind.MpegTs;
            if (IsMp3(buffer, n)) return MediaSignatureKind.Mp3;
            if (IsAacAdts(buffer, n)) return MediaSignatureKind.AacAdts;

            return MediaSignatureKind.Unknown;
        }

        private static bool IsIsoBmff(byte[] b, int n) =>
            n > 11 && b[4] == 0x66 && b[5] == 0x74 && b[6] == 0x79 && b[7] == 0x70;

        private static bool IsEbml(byte[] b, int n) =>
            n > 3 && b[0] == 0x1A && b[1] == 0x45 && b[2] == 0xDF && b[3] == 0xA3;

        private static bool IsOgg(byte[] b, int n) =>
            n > 3 && b[0] == 0x4F && b[1] == 0x67 && b[2] == 0x67 && b[3] == 0x53;

        private static bool IsFlac(byte[] b, int n) =>
            n > 3 && b[0] == 0x66 && b[1] == 0x4C && b[2] == 0x61 && b[3] == 0x43;

        private static bool IsWav(byte[] b, int n) =>
            n > 11 &&
            b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
            b[8] == 0x57 && b[9] == 0x41 && b[10] == 0x56 && b[11] == 0x45;

        private static bool IsAvi(byte[] b, int n) =>
            n > 11 &&
            b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
            b[8] == 0x41 && b[9] == 0x56 && b[10] == 0x49 && b[11] == 0x20;

        private static bool IsFlv(byte[] b, int n) =>
            n > 2 && b[0] == 0x46 && b[1] == 0x4C && b[2] == 0x56;

        private static bool IsAsf(byte[] b, int n) =>
            n > 15 &&
            b[0] == 0x30 && b[1] == 0x26 && b[2] == 0xB2 && b[3] == 0x75 &&
            b[4] == 0x8E && b[5] == 0x66 && b[6] == 0xCF && b[7] == 0x11 &&
            b[8] == 0xA6 && b[9] == 0xD9 && b[10] == 0x00 && b[11] == 0xAA &&
            b[12] == 0x00 && b[13] == 0x62 && b[14] == 0xCE && b[15] == 0x6C;

        private static bool IsMpegPs(byte[] b, int n) =>
            n > 3 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0x01 && b[3] == 0xBA;

        private static bool IsMpegTs(byte[] b, int n)
        {
            if (n < 189)
            {
                return false;
            }

            if (b[0] != 0x47)
            {
                return false;
            }

            return (n <= 376 || b[188] == 0x47) && (n <= 564 || b[376] == 0x47);
        }

        private static bool IsMp3(byte[] b, int n)
        {
            if (n > 2 && b[0] == 0x49 && b[1] == 0x44 && b[2] == 0x33)
            {
                return true;
            }

            return n > 1 && b[0] == 0xFF && (b[1] & 0xE0) == 0xE0;
        }

        private static bool IsAacAdts(byte[] b, int n) =>
            n > 1 && b[0] == 0xFF && (b[1] & 0xF0) == 0xF0;

        private enum MediaSignatureKind
        {
            Unknown = 0,
            IsoBmff,
            Ebml,
            Ogg,
            Flac,
            Wav,
            Avi,
            Flv,
            Asf,
            MpegPs,
            MpegTs,
            Mp3,
            AacAdts
        }
    }
}
