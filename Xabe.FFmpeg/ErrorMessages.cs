namespace Xabe.FFmpeg
{
    internal static class ErrorMessages
    {
        internal static string OperatingSystemAndArchitectureMissing =>
            Get("Отсутствует тип системы и архитектура", "Operating system and architecture are missing.", "Betriebssystem und Architektur fehlen.");
        internal static string ConversionAlreadyStarted =>
            Get("Конвертация уже была запущена.", "Conversion has already been started.", "Die Konvertierung wurde bereits gestartet.");
        internal static string FailedToStopProcess =>
            Get("Не удалось остановить процесс. Процесс был принудительно завершен.", "Failed to stop process. The process was terminated forcibly.", "Der Prozess konnte nicht gestoppt werden. Der Prozess wurde zwangsweise beendet.");
        internal static string UnknownVideoSize =>
            Get("Неизвестный размер видео.", "Unknown video size.", "Unbekannte Videogröße.");
        internal static string StreamMustBeReadable =>
            Get("Поток должен быть доступен для чтения", "Stream must be readable.", "Der Stream muss lesbar sein.");
        internal static string StreamMustBeWritable =>
            Get("Поток должен быть доступен для записи", "Stream must be writable.", "Der Stream muss beschreibbar sein.");
        internal static string SpeedOutOfRange =>
            Get("Значение должно быть больше 0.5 и меньше 2.0.", "Value must be greater than 0.5 and less than 2.0.", "Der Wert muss größer als 0,5 und kleiner als 2,0 sein.");
        internal static string ConcatAtLeastTwoFiles =>
            Get("Для объединения необходимо указать как минимум 2 файла", "At least 2 files are required for concatenation.", "Für die Zusammenführung sind mindestens 2 Dateien erforderlich.");
        internal static string BitrateMustBeGreaterThanZero =>
            Get("Битрейт должен быть больше 0.", "Bitrate must be greater than 0.", "Die Bitrate muss größer als 0 sein.");
        internal static string SampleRateMustBeGreaterThanZero =>
            Get("Частота дискретизации должна быть больше 0.", "Sample rate must be greater than 0.", "Die Abtastrate muss größer als 0 sein.");
        internal static string AtLeastTwoTimeBoundaries =>
            Get("Необходимо указать как минимум две временные границы.", "At least two time boundaries are required.", "Es müssen mindestens zwei Zeitgrenzen angegeben werden.");
        internal static string TimecodesMustDefineIncreasingRanges =>
            Get("Таймкоды должны задавать возрастающие диапазоны.", "Timecodes must define increasing ranges.", "Die Timecodes müssen aufsteigende Bereiche definieren.");
        internal static string InvalidFileUnableToLoad =>
            Get("Неверный файл. Не удалось загрузить файл {0}", "Invalid file. Unable to load file {0}", "Ungültige Datei. Datei {0} konnte nicht geladen werden.");
        internal static string InputFileDoesNotExist =>
            Get("Входной файл {0} не существует.", "Input file {0} does not exist.", "Die Eingabedatei {0} existiert nicht.");
        internal static string InputFileDoesNotContainAudioStream =>
            Get("Входной файл не содержит аудиодорожку.", "Input file does not contain an audio stream.", "Die Eingabedatei enthält keinen Audiostream.");
        internal static string InputFileDoesNotContainVideoStream =>
            Get("Входной файл не содержит видеодорожку.", "Input file does not contain a video stream.", "Die Eingabedatei enthält keinen Videostream.");
        internal static string TimecodeOutOfRange =>
            Get("Таймкод {0} выходит за пределы длительности медиафайла.", "Timecode {0} is out of media duration range.", "Der Timecode {0} liegt außerhalb der Mediendauer.");
        internal static string SeekCannotExceedDuration =>
            Get("Позиция поиска не может быть больше длительности видео. Позиция: {0} Длительность: {1}", "Seek position cannot exceed video duration. Position: {0} Duration: {1}", "Die Suchposition darf die Videodauer nicht überschreiten. Position: {0} Dauer: {1}");
        internal static string FrequencyMustBeGreaterThanZero =>
            Get("Частота должна быть больше 0.", "Frequency must be greater than 0.", "Die Frequenz muss größer als 0 sein.");
        internal static string SampleRateMustBeGreaterThanZeroForSettings =>
            Get("Частота дискретизации должна быть больше 0.", "Sample rate must be greater than 0.", "Die Abtastrate muss größer als 0 sein.");
        internal static string ChannelsMustBeGreaterThanZero =>
            Get("Количество каналов должно быть больше 0.", "Channels count must be greater than 0.", "Die Anzahl der Kanäle muss größer als 0 sein.");
        internal static string ExecutableSignatureMismatch =>
            Get("Файл '{0}' не соответствует ожидаемой сигнатуре исполняемого файла для текущей ОС.", "File '{0}' does not match the expected executable signature for the current OS.", "Die Datei '{0}' entspricht nicht der erwarteten Signatur einer ausführbaren Datei für das aktuelle Betriebssystem.");
        internal static string InputFileSignatureMismatch =>
            Get("Сигнатура входного файла '{0}' не соответствует заявленному типу '{1}'.", "Input file signature mismatch for '{0}'. Declared type '{1}' is not valid for this content.", "Die Signatur der Eingabedatei '{0}' entspricht nicht dem angegebenen Typ '{1}'.");

        private static string Get(string russian, string english, string german)
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case LocalizationLanguage.English:
                    return english;
                case LocalizationLanguage.German:
                    return german;
                case LocalizationLanguage.Russian:
                default:
                    return russian;
            }
        }
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
