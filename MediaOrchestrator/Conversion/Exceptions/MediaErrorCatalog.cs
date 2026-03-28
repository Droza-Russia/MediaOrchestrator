using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Публичный справочник кодов ошибок библиотеки MediaOrchestrator.
    /// </summary>
    public static class MediaErrorCatalog
    {
        private static readonly IReadOnlyDictionary<MediaErrorCode, MediaErrorDescriptor> _descriptors =
            new Dictionary<MediaErrorCode, MediaErrorDescriptor>
            {
                [MediaErrorCode.Unknown] = Create(MediaErrorCode.Unknown, "MOR-GN-0000", "Unknown error", "Unclassified library error."),
                [MediaErrorCode.ConversionFailed] = Create(MediaErrorCode.ConversionFailed, "MOR-CV-1000", "Conversion failed", "Generic MediaOrchestrator conversion failure."),
                [MediaErrorCode.UnknownDecoder] = Create(MediaErrorCode.UnknownDecoder, "MOR-CV-1001", "Unknown decoder", "MediaOrchestrator could not resolve a decoder for the input."),
                [MediaErrorCode.NoSuitableOutputFormat] = Create(MediaErrorCode.NoSuitableOutputFormat, "MOR-CV-1003", "No suitable output format", "MediaOrchestrator could not determine a suitable output container format."),
                [MediaErrorCode.InvalidBitstreamFilter] = Create(MediaErrorCode.InvalidBitstreamFilter, "MOR-CV-1004", "Invalid bitstream filter", "The configured bitstream filter is not supported."),
                [MediaErrorCode.StreamMappingFailed] = Create(MediaErrorCode.StreamMappingFailed, "MOR-CV-1005", "Stream mapping failed", "MediaOrchestrator could not map one or more requested streams."),
                [MediaErrorCode.StreamCodecNotSupported] = Create(MediaErrorCode.StreamCodecNotSupported, "MOR-CV-1006", "Stream codec not supported", "The stream codec is not supported by the selected operation or container."),
                [MediaErrorCode.InsufficientDiskSpace] = Create(MediaErrorCode.InsufficientDiskSpace, "MOR-IO-4002", "Insufficient disk space", "There is not enough disk space to write the output."),
                [MediaErrorCode.ExecutableNotFound] = Create(MediaErrorCode.ExecutableNotFound, "MOR-TL-2000", "Executable not found", "MediaOrchestrator or FFprobe executable could not be found."),
                [MediaErrorCode.ExecutablesPathAccessDenied] = Create(MediaErrorCode.ExecutablesPathAccessDenied, "MOR-TL-2001", "Executables path access denied", "The configured MediaOrchestrator executables path is not accessible."),
                [MediaErrorCode.ExecutableSignatureMismatch] = Create(MediaErrorCode.ExecutableSignatureMismatch, "MOR-TL-2002", "Executable signature mismatch", "The located executable does not match the expected platform signature."),
                [MediaErrorCode.InvalidInput] = Create(MediaErrorCode.InvalidInput, "MOR-IN-3000", "Invalid input", "The input media or source is invalid."),
                [MediaErrorCode.InputFileUnreadable] = Create(MediaErrorCode.InputFileUnreadable, "MOR-IN-3001", "Input file unreadable", "The input file cannot be read."),
                [MediaErrorCode.InputPathAccessDenied] = Create(MediaErrorCode.InputPathAccessDenied, "MOR-IN-3002", "Input path access denied", "The input path is not accessible due to permissions."),
                [MediaErrorCode.InputFileEmpty] = Create(MediaErrorCode.InputFileEmpty, "MOR-IN-3003", "Input file empty", "The input file is empty."),
                [MediaErrorCode.InputFileLocked] = Create(MediaErrorCode.InputFileLocked, "MOR-IN-3004", "Input file locked", "The input file is locked by another process."),
                [MediaErrorCode.InputFileStillBeingWritten] = Create(MediaErrorCode.InputFileStillBeingWritten, "MOR-IN-3005", "Input file still being written", "The input file is still changing and has not stabilized."),
                [MediaErrorCode.InputFileSignatureMismatch] = Create(MediaErrorCode.InputFileSignatureMismatch, "MOR-IN-3006", "Input signature mismatch", "The input file signature does not match the expected media type."),
                [MediaErrorCode.AudioStreamNotFound] = Create(MediaErrorCode.AudioStreamNotFound, "MOR-IN-3007", "Audio stream not found", "The input does not contain an audio stream."),
                [MediaErrorCode.VideoStreamNotFound] = Create(MediaErrorCode.VideoStreamNotFound, "MOR-IN-3008", "Video stream not found", "The input does not contain a video stream."),
                [MediaErrorCode.SubtitleStreamNotFound] = Create(MediaErrorCode.SubtitleStreamNotFound, "MOR-IN-3009", "Subtitle stream not found", "The input does not contain a subtitle stream."),
                [MediaErrorCode.StreamIndexOutOfRange] = Create(MediaErrorCode.StreamIndexOutOfRange, "MOR-IN-3010", "Stream index out of range", "The requested stream index is outside the available range."),
                [MediaErrorCode.OutputPathAccessDenied] = Create(MediaErrorCode.OutputPathAccessDenied, "MOR-IO-4000", "Output path access denied", "The output path is not accessible."),
                [MediaErrorCode.OutputDirectoryNotWritable] = Create(MediaErrorCode.OutputDirectoryNotWritable, "MOR-IO-4001", "Output directory not writable", "The output directory cannot be written to."),
                [MediaErrorCode.HostedVideoDownloadFailed] = Create(MediaErrorCode.HostedVideoDownloadFailed, "MOR-HD-5000", "Hosted video download failed", "The external hosted video download step failed."),
                [MediaErrorCode.HardwareAcceleratorNotFound] = Create(MediaErrorCode.HardwareAcceleratorNotFound, "MOR-HW-6000", "Hardware accelerator not found", "Requested hardware acceleration is not available."),
                [MediaErrorCode.OperationCanceled] = Create(MediaErrorCode.OperationCanceled, "MOR-OP-9000", "Operation canceled", "The operation was canceled.")
            };

        /// <summary>
        ///     Возвращает все известные коды ошибок с описаниями.
        /// </summary>
        public static IReadOnlyCollection<MediaErrorDescriptor> GetAll()
        {
            return _descriptors.Values.OrderBy(item => item.ErrorCode).ToArray();
        }

        /// <summary>
        ///     Возвращает описание кода ошибки.
        /// </summary>
        public static MediaErrorDescriptor Get(MediaErrorCode errorCode)
        {
            return _descriptors.TryGetValue(errorCode, out var descriptor)
                ? descriptor
                : _descriptors[MediaErrorCode.Unknown];
        }

        internal static MediaErrorCode Resolve(Type exceptionType)
        {
            if (exceptionType == null)
            {
                return MediaErrorCode.Unknown;
            }

            if (exceptionType == typeof(OperationCanceledException) || exceptionType.IsSubclassOf(typeof(OperationCanceledException)))
            {
                return MediaErrorCode.OperationCanceled;
            }

            if (exceptionType == typeof(UnknownDecoderException))
            {
                return MediaErrorCode.UnknownDecoder;
            }

            if (exceptionType == typeof(HardwareAcceleratorNotFoundException))
            {
                return MediaErrorCode.HardwareAcceleratorNotFound;
            }

            if (exceptionType == typeof(NoSuitableOutputFormatException))
            {
                return MediaErrorCode.NoSuitableOutputFormat;
            }

            if (exceptionType == typeof(InvalidBitstreamFilterException))
            {
                return MediaErrorCode.InvalidBitstreamFilter;
            }

            if (exceptionType == typeof(StreamMappingException))
            {
                return MediaErrorCode.StreamMappingFailed;
            }

            if (exceptionType == typeof(StreamCodecNotSupportedException))
            {
                return MediaErrorCode.StreamCodecNotSupported;
            }

            if (exceptionType == typeof(InsufficientDiskSpaceException))
            {
                return MediaErrorCode.InsufficientDiskSpace;
            }

            if (exceptionType == typeof(ExecutableSignatureMismatchException))
            {
                return MediaErrorCode.ExecutableSignatureMismatch;
            }

            if (exceptionType == typeof(ExecutablesPathAccessDeniedException))
            {
                return MediaErrorCode.ExecutablesPathAccessDenied;
            }

            if (exceptionType == typeof(ToolchainNotFoundException))
            {
                return MediaErrorCode.ExecutableNotFound;
            }

            if (exceptionType == typeof(InputPathAccessDeniedException))
            {
                return MediaErrorCode.InputPathAccessDenied;
            }

            if (exceptionType == typeof(InputFileUnreadableException))
            {
                return MediaErrorCode.InputFileUnreadable;
            }

            if (exceptionType == typeof(InputFileEmptyException))
            {
                return MediaErrorCode.InputFileEmpty;
            }

            if (exceptionType == typeof(InputFileLockedException))
            {
                return MediaErrorCode.InputFileLocked;
            }

            if (exceptionType == typeof(InputFileStillBeingWrittenException))
            {
                return MediaErrorCode.InputFileStillBeingWritten;
            }

            if (exceptionType == typeof(InputFileSignatureMismatchException))
            {
                return MediaErrorCode.InputFileSignatureMismatch;
            }

            if (exceptionType == typeof(AudioStreamNotFoundException))
            {
                return MediaErrorCode.AudioStreamNotFound;
            }

            if (exceptionType == typeof(VideoStreamNotFoundException))
            {
                return MediaErrorCode.VideoStreamNotFound;
            }

            if (exceptionType == typeof(SubtitleStreamNotFoundException))
            {
                return MediaErrorCode.SubtitleStreamNotFound;
            }

            if (exceptionType == typeof(StreamIndexOutOfRangeException))
            {
                return MediaErrorCode.StreamIndexOutOfRange;
            }

            if (exceptionType == typeof(OutputPathAccessDeniedException))
            {
                return MediaErrorCode.OutputPathAccessDenied;
            }

            if (exceptionType == typeof(OutputDirectoryNotWritableException))
            {
                return MediaErrorCode.OutputDirectoryNotWritable;
            }

            if (exceptionType == typeof(HostedVideoDownloadException))
            {
                return MediaErrorCode.HostedVideoDownloadFailed;
            }

            if (exceptionType == typeof(InvalidInputException) || exceptionType.IsSubclassOf(typeof(InvalidInputException)))
            {
                return MediaErrorCode.InvalidInput;
            }

            if (exceptionType == typeof(ConversionException) || exceptionType.IsSubclassOf(typeof(ConversionException)))
            {
                return MediaErrorCode.ConversionFailed;
            }

            return MediaErrorCode.Unknown;
        }

        internal static MediaErrorCode Resolve(string failureType)
        {
            if (string.IsNullOrWhiteSpace(failureType))
            {
                return MediaErrorCode.Unknown;
            }

            var normalized = failureType.Trim();
            foreach (var descriptor in _descriptors.Values)
            {
                if (string.Equals(descriptor.Code, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(descriptor.ErrorCode.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return descriptor.ErrorCode;
                }
            }

            var name = normalized;
            int separatorIndex = normalized.LastIndexOf('.');
            if (separatorIndex >= 0 && separatorIndex < normalized.Length - 1)
            {
                name = normalized.Substring(separatorIndex + 1);
            }

            switch (name)
            {
                case nameof(UnknownDecoderException):
                    return MediaErrorCode.UnknownDecoder;
                case nameof(HardwareAcceleratorNotFoundException):
                    return MediaErrorCode.HardwareAcceleratorNotFound;
                case nameof(NoSuitableOutputFormatException):
                    return MediaErrorCode.NoSuitableOutputFormat;
                case nameof(InvalidBitstreamFilterException):
                    return MediaErrorCode.InvalidBitstreamFilter;
                case nameof(StreamMappingException):
                    return MediaErrorCode.StreamMappingFailed;
                case nameof(StreamCodecNotSupportedException):
                    return MediaErrorCode.StreamCodecNotSupported;
                case nameof(InsufficientDiskSpaceException):
                    return MediaErrorCode.InsufficientDiskSpace;
                case nameof(ExecutableSignatureMismatchException):
                    return MediaErrorCode.ExecutableSignatureMismatch;
                case nameof(ExecutablesPathAccessDeniedException):
                    return MediaErrorCode.ExecutablesPathAccessDenied;
                case nameof(ToolchainNotFoundException):
                    return MediaErrorCode.ExecutableNotFound;
                case nameof(InputPathAccessDeniedException):
                    return MediaErrorCode.InputPathAccessDenied;
                case nameof(InputFileUnreadableException):
                    return MediaErrorCode.InputFileUnreadable;
                case nameof(InputFileEmptyException):
                    return MediaErrorCode.InputFileEmpty;
                case nameof(InputFileLockedException):
                    return MediaErrorCode.InputFileLocked;
                case nameof(InputFileStillBeingWrittenException):
                    return MediaErrorCode.InputFileStillBeingWritten;
                case nameof(InputFileSignatureMismatchException):
                    return MediaErrorCode.InputFileSignatureMismatch;
                case nameof(AudioStreamNotFoundException):
                    return MediaErrorCode.AudioStreamNotFound;
                case nameof(VideoStreamNotFoundException):
                    return MediaErrorCode.VideoStreamNotFound;
                case nameof(SubtitleStreamNotFoundException):
                    return MediaErrorCode.SubtitleStreamNotFound;
                case nameof(StreamIndexOutOfRangeException):
                    return MediaErrorCode.StreamIndexOutOfRange;
                case nameof(OutputPathAccessDeniedException):
                    return MediaErrorCode.OutputPathAccessDenied;
                case nameof(OutputDirectoryNotWritableException):
                    return MediaErrorCode.OutputDirectoryNotWritable;
                case nameof(HostedVideoDownloadException):
                    return MediaErrorCode.HostedVideoDownloadFailed;
                case "OperationCanceledException":
                case "TaskCanceledException":
                    return MediaErrorCode.OperationCanceled;
                case nameof(ConversionException):
                    return MediaErrorCode.ConversionFailed;
                default:
                    return MediaErrorCode.Unknown;
            }
        }

        private static MediaErrorDescriptor Create(MediaErrorCode errorCode, string code, string title, string description)
        {
            return new MediaErrorDescriptor
            {
                ErrorCode = errorCode,
                Code = code,
                Title = title,
                Description = description
            };
        }
    }
}
