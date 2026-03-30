using System;
using System.Collections.Generic;
using MediaOrchestrator;

namespace MediaOrchestrator.Exceptions
{
    internal sealed class ExceptionCheck
    {
        private readonly string _searchPhrase;
        private readonly bool _requiresOutputFileIsEmptyMessage;

        public ExceptionCheck(string searchPhrase, bool requiresOutputFileIsEmptyMessage = false)
        {
            _searchPhrase = searchPhrase;
            _requiresOutputFileIsEmptyMessage = requiresOutputFileIsEmptyMessage;
        }

        internal bool CheckLog(string log, bool outputFileIsEmpty)
        {
            return log.IndexOf(_searchPhrase, StringComparison.Ordinal) >= 0
                   && (!_requiresOutputFileIsEmptyMessage || outputFileIsEmpty);
        }
    }

    internal sealed class MediaToolExceptionCatcher
    {
        private sealed class ExceptionRule
        {
            internal ExceptionRule(ExceptionCheck check, Action<string, string> throwException)
            {
                Check = check;
                ThrowException = throwException;
            }

            internal ExceptionCheck Check { get; }
            internal Action<string, string> ThrowException { get; }
        }

        private static readonly IReadOnlyList<ExceptionRule> _checks =
            new[]
            {
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.InvalidNalUnitSize), (output, args) => throw new ConversionException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.PacketMismatch, true), (output, args) => throw new ConversionException(output, args)),

                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.AsfReadPtsFailed, true), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.MissingKeyFrameWhileSearchingTimestamp, true), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.OldInterlacedModeNotSupported, true), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.Mpeg1Video, true), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.FrameRateVeryHighForMuxer, true), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.MultipleFourccNotSupported), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.UnknownDecoder), (output, args) => throw new UnknownDecoderException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.FailedToOpenCodecInStreamInfo), (output, args) => throw new UnknownDecoderException(output, args)),

                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.UnrecognizedHwAccel), (output, args) => throw new HardwareAcceleratorNotFoundException(output, args)),

                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.UnableToFindSuitableOutputFormat), (output, args) => throw new NoSuitableOutputFormatException(output, args)),

                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.NotSupportedByBitstreamFilter), (output, args) => throw new InvalidBitstreamFilterException(output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.StreamMatchesNoStreams), (output, args) => throw new StreamMappingException(ErrorMessages.StreamMappingFailed, output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.CodecNotCurrentlySupportedInContainer), (output, args) => throw new StreamCodecNotSupportedException(ErrorMessages.StreamCodecNotSupported, output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.CouldNotFindTagForCodec), (output, args) => throw new StreamCodecNotSupportedException(ErrorMessages.StreamCodecNotSupported, output, args)),
                new ExceptionRule(new ExceptionCheck(MediaToolLogPatterns.UnsupportedCodec), (output, args) => throw new StreamCodecNotSupportedException(ErrorMessages.StreamCodecNotSupported, output, args))
            };

        internal void CatchFFmpegErrors(string output, string args)
        {
            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            if (LogIndicatesInsufficientDiskSpace(output))
            {
                throw new InsufficientDiskSpaceException(ErrorMessages.InsufficientDiskSpace, output, args);
            }

            var outputFileIsEmpty = output.IndexOf(MediaToolLogPatterns.OutputFileIsEmpty, StringComparison.Ordinal) >= 0;
            foreach (var rule in _checks)
            {
                if (!rule.Check.CheckLog(output, outputFileIsEmpty))
                {
                    continue;
                }

                try
                {
                    rule.ThrowException(output, args);
                }
                catch (ConversionException e)
                {
                    throw new ConversionException(e.Message, e, e.InputParameters);
                }
            }
        }

        internal static bool OutputIndicatesInsufficientDiskSpace(string log) => LogIndicatesInsufficientDiskSpace(log);

        private static bool LogIndicatesInsufficientDiskSpace(string log)
        {
            if (string.IsNullOrEmpty(log))
            {
                return false;
            }

            return ContainsIgnoreCase(log, "NO SPACE LEFT ON DEVICE")
                   || ContainsIgnoreCase(log, "NOT ENOUGH SPACE ON THE DISK")
                   || ContainsIgnoreCase(log, "NOT ENOUGH STORAGE IS AVAILABLE")
                   || ContainsIgnoreCase(log, "ENOSPC")
                   || ContainsIgnoreCase(log, "ERRNO=28")
                   || ContainsIgnoreCase(log, "ERROR NUMBER -28")
                   || ContainsIgnoreCase(log, "НЕДОСТАТОЧНО МЕСТА НА ДИСКЕ")
                   || ContainsIgnoreCase(log, "НЕ ХВАТАЕТ МЕСТА НА ДИСКЕ")
                   || ContainsIgnoreCase(log, "NICHT GENÜGEND SPEICHERPLATZ")
                   || ContainsIgnoreCase(log, "NICHT GENUG SPEICHERPLATZ");
        }

        private static bool ContainsIgnoreCase(string source, string phrase)
        {
            return source.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
