using System;
using System.Collections.Generic;
using Xabe.FFmpeg;

namespace Xabe.FFmpeg.Exceptions
{
    internal class ExceptionCheck
    {
        private readonly string _searchPhrase;
        private readonly bool _containsFileIsEmptyMessage;
        public ExceptionCheck(string searchPhrase, bool containsFileIsEmptyMessage = false)
        {
            _searchPhrase = searchPhrase;
            _containsFileIsEmptyMessage = containsFileIsEmptyMessage;
        }

        /// <summary>
        /// Проверяет журнал вывода и выбрасывает исключение.
        /// Некоторые ошибки считаются критическими только если в журнале найдено сообщение об пустом выходном файле.
        /// </summary>
        /// <param name="log">Журнал вывода FFmpeg.</param>
        internal bool CheckLog(string log)
        {
            return log.Contains(_searchPhrase) && (!_containsFileIsEmptyMessage || log.Contains(FFmpegLogPatterns.OutputFileIsEmpty));
        }
    }

    internal class FFmpegExceptionCatcher
    {
        private static readonly Dictionary<ExceptionCheck, Action<string, string>> _checks = new Dictionary<ExceptionCheck, Action<string, string>>();

        static FFmpegExceptionCatcher()
        {
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.InvalidNalUnitSize), (output, args) => throw new ConversionException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.PacketMismatch, true), (output, args) => throw new ConversionException(output, args));

            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.AsfReadPtsFailed, true), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.MissingKeyFrameWhileSearchingTimestamp, true), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.OldInterlacedModeNotSupported, true), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.Mpeg1Video, true), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.FrameRateVeryHighForMuxer, true), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.MultipleFourccNotSupported), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.UnknownDecoder), (output, args) => throw new UnknownDecoderException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.FailedToOpenCodecInStreamInfo), (output, args) => throw new UnknownDecoderException(output, args));

            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.UnrecognizedHwAccel), (output, args) => throw new HardwareAcceleratorNotFoundException(output, args));

            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.UnableToFindSuitableOutputFormat), (output, args) => throw new FFmpegNoSuitableOutputFormatFoundException(output, args));

            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.NotSupportedByBitstreamFilter), (output, args) => throw new InvalidBitstreamFilterException(output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.StreamMatchesNoStreams), (output, args) => throw new StreamMappingException(ErrorMessages.StreamMappingFailed, output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.CodecNotCurrentlySupportedInContainer), (output, args) => throw new StreamCodecNotSupportedException(ErrorMessages.StreamCodecNotSupported, output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.CouldNotFindTagForCodec), (output, args) => throw new StreamCodecNotSupportedException(ErrorMessages.StreamCodecNotSupported, output, args));
            _checks.Add(new ExceptionCheck(FFmpegLogPatterns.UnsupportedCodec), (output, args) => throw new StreamCodecNotSupportedException(ErrorMessages.StreamCodecNotSupported, output, args));
        }

        internal void CatchFFmpegErrors(string output, string args)
        {
            if (LogIndicatesInsufficientDiskSpace(output))
            {
                throw new InsufficientDiskSpaceException(ErrorMessages.InsufficientDiskSpace, output, args);
            }

            foreach (var check in _checks)
            {
                try
                {
                    if (check.Key.CheckLog(output))
                    {
                        check.Value(output, args);
                    }
                }
                catch (ConversionException e)
                {
                    throw new ConversionException(e.Message, e, e.InputParameters);
                }
            }
        }

        /// <summary>
        ///     Проверка журнала FFmpeg на признаки нехватки места на диске (для повторного использования после других правил).
        /// </summary>
        internal static bool OutputIndicatesInsufficientDiskSpace(string log) => LogIndicatesInsufficientDiskSpace(log);

        /// <summary>
        ///     Типичные фрагменты stderr ОС/FFmpeg при ENOSPC и аналогах Windows.
        /// </summary>
        private static bool LogIndicatesInsufficientDiskSpace(string log)
        {
            if (string.IsNullOrEmpty(log))
            {
                return false;
            }

            var u = log.ToUpperInvariant();
            return u.Contains("NO SPACE LEFT ON DEVICE")
                   || u.Contains("NOT ENOUGH SPACE ON THE DISK")
                   || u.Contains("NOT ENOUGH STORAGE IS AVAILABLE")
                   || u.Contains("ENOSPC")
                   || u.Contains("ERRNO=28")
                   || u.Contains("ERROR NUMBER -28")
                   || u.Contains("НЕДОСТАТОЧНО МЕСТА НА ДИСКЕ")
                   || u.Contains("НЕ ХВАТАЕТ МЕСТА НА ДИСКЕ")
                   || u.Contains("NICHT GENÜGEND SPEICHERPLATZ")
                   || u.Contains("NICHT GENUG SPEICHERPLATZ");
        }
    }
}
