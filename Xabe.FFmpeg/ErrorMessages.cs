namespace Xabe.FFmpeg
{
    internal static class ErrorMessages
    {
        internal const string OperatingSystemAndArchitectureMissing = "Отсутствует тип системы и архитектура";
        internal const string ConversionAlreadyStarted = "Конвертация уже была запущена.";
        internal const string FailedToStopProcess = "Не удалось остановить процесс. Процесс был принудительно завершен.";
        internal const string UnknownVideoSize = "Неизвестный размер видео.";
        internal const string StreamMustBeReadable = "Поток должен быть доступен для чтения";
        internal const string StreamMustBeWritable = "Поток должен быть доступен для записи";
        internal const string SpeedOutOfRange = "Значение должно быть больше 0.5 и меньше 2.0.";
        internal const string ConcatAtLeastTwoFiles = "Для объединения необходимо указать как минимум 2 файла";
        internal const string BitrateMustBeGreaterThanZero = "Битрейт должен быть больше 0.";
        internal const string SampleRateMustBeGreaterThanZero = "Частота дискретизации должна быть больше 0.";
        internal const string AtLeastTwoTimeBoundaries = "Необходимо указать как минимум две временные границы.";
        internal const string TimecodesMustDefineIncreasingRanges = "Таймкоды должны задавать возрастающие диапазоны.";
        internal const string InvalidFileUnableToLoad = "Неверный файл. Не удалось загрузить файл {0}";
        internal const string InputFileDoesNotExist = "Входной файл {0} не существует.";
        internal const string InputFileDoesNotContainAudioStream = "Входной файл не содержит аудиодорожку.";
        internal const string TimecodeOutOfRange = "Таймкод {0} выходит за пределы длительности медиафайла.";
        internal const string SeekCannotExceedDuration = "Позиция поиска не может быть больше длительности видео. Позиция: {0} Длительность: {1}";
        internal const string FrequencyMustBeGreaterThanZero = "Частота должна быть больше 0.";
        internal const string SampleRateMustBeGreaterThanZeroForSettings = "Частота дискретизации должна быть больше 0.";
        internal const string ChannelsMustBeGreaterThanZero = "Количество каналов должно быть больше 0.";
    }

    // Технические паттерны stderr FFmpeg для распознавания ошибок.
    // Не переводятся, потому что зависят от текста вывода FFmpeg.
    internal static class FFmpegLogPatterns
    {
        internal const string OutputFileIsEmpty = "Output file is empty";
        internal const string InvalidNalUnitSize = "Invalid NAL unit size";
        internal const string PacketMismatch = "Packet mismatch";
        internal const string AsfReadPtsFailed = "asf_read_pts failed";
        internal const string MissingKeyFrameWhileSearchingTimestamp = "Missing key frame while searching for timestamp";
        internal const string OldInterlacedModeNotSupported = "Old interlaced mode is not supported";
        internal const string Mpeg1Video = "mpeg1video";
        internal const string FrameRateVeryHighForMuxer = "Frame rate very high for a muxer not efficiently supporting it";
        internal const string MultipleFourccNotSupported = "multiple fourcc not supported";
        internal const string UnknownDecoder = "Unknown decoder";
        internal const string FailedToOpenCodecInStreamInfo = "Failed to open codec in avformat_find_stream_info";
        internal const string UnrecognizedHwAccel = "Unrecognized hwaccel: ";
        internal const string UnableToFindSuitableOutputFormat = "Unable to find a suitable output format";
        internal const string NotSupportedByBitstreamFilter = "is not supported by the bitstream filter";
    }
}
