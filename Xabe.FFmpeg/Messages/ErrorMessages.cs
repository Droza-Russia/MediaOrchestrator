namespace MediaOrchestrator
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
        internal static string InputPathMustBeProvided =>
            Get("Необходимо указать входной путь.", "Input path must be provided.", "Ein Eingabepfad muss angegeben werden.");
        internal static string OutputPathMustBeProvided =>
            Get("Необходимо указать выходной путь.", "Output path must be provided.", "Ein Ausgabepfad muss angegeben werden.");
        internal static string SourceUrlMustBeProvided =>
            Get("Необходимо указать URL источника.", "Source URL must be provided.", "Eine Quell-URL muss angegeben werden.");
        internal static string InputSpecifierMustBeProvided =>
            Get("Необходимо указать спецификатор входа.", "Input specifier must be provided.", "Ein Eingabespezifizierer muss angegeben werden.");
        internal static string InputPipeAlreadyConfigured =>
            Get("Входной pipe уже настроен.", "An input pipe has already been configured.", "Eine Eingabe-Pipe wurde bereits konfiguriert.");
        internal static string AspectRatioMustBeProvided =>
            Get("Необходимо указать aspect ratio.", "Aspect ratio must be provided.", "Ein Seitenverhältnis muss angegeben werden.");
        internal static string InputSourceValueMustBeProvided =>
            Get("Необходимо указать значение источника входа.", "Input source value must be provided.", "Ein Wert für die Eingabequelle muss angegeben werden.");
        internal static string FilterGraphExpressionMustBeProvided =>
            Get("Необходимо указать выражение графа фильтров.", "Filter graph expression must be provided.", "Ein Filtergraph-Ausdruck muss angegeben werden.");
        internal static string FilterLabelNameMustBeProvided =>
            Get("Необходимо указать имя метки фильтра.", "Filter label name must be provided.", "Ein Name für das Filter-Label muss angegeben werden.");
        internal static string FilterNameMustBeProvided =>
            Get("Необходимо указать имя фильтра.", "Filter name must be provided.", "Ein Filtername muss angegeben werden.");
        internal static string AtLeastOneFilterMustBeProvided =>
            Get("Необходимо указать хотя бы один фильтр.", "At least one filter must be provided.", "Mindestens ein Filter muss angegeben werden.");
        internal static string PathMustNotBeEmpty =>
            Get("Путь не должен быть пустым.", "Path must not be empty.", "Der Pfad darf nicht leer sein.");
        internal static string InvalidFilePath =>
            Get("Некорректный путь к файлу.", "Invalid file path.", "Ungültiger Dateipfad.");
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
        internal static string InputPathIsNotAFile =>
            Get("Путь {0} указывает на каталог или не является обычным файлом.", "Path {0} refers to a directory or is not a regular file.", "Pfad {0} verweist auf ein Verzeichnis oder ist keine reguläre Datei.");
        internal static string MediaFileHeaderReadFailed =>
            Get("Не удалось прочитать начало медиафайла {0}.", "Failed to read the beginning of media file {0}.", "Der Anfang der Mediendatei {0} konnte nicht gelesen werden.");
        internal static string MediaFileHeaderReadTimeout =>
            Get("Превышено время ожидания при чтении начала медиафайла {0} (возможен именованный канал или недоступный ресурс).", "Timeout while reading the beginning of media file {0} (possible named pipe or unavailable resource).", "Zeitüberschreitung beim Lesen des Anfangs der Mediendatei {0} (möglicherweise benannte Pipe oder nicht verfügbare Ressource).");
        internal static string InputFileIsLocked =>
            Get("Файл {0} сейчас занят другим процессом или временно недоступен для чтения.", "File {0} is locked by another process or temporarily unavailable for reading.", "Die Datei {0} wird von einem anderen Prozess verwendet oder ist vorübergehend nicht lesbar.");
        internal static string InputFileIsUnreadable =>
            Get("Файл {0} не удалось прочитать. Проверьте права доступа и состояние файла.", "File {0} could not be read. Check permissions and file state.", "Die Datei {0} konnte nicht gelesen werden. Überprüfen Sie die Berechtigungen und den Zustand der Datei.");
        internal static string InputPathAccessDenied =>
            Get("Нет доступа к входному пути {0}. Проверьте права доступа и доступность локальной или сетевой папки.", "Access denied to input path {0}. Check permissions and the availability of the local or network folder.", "Zugriff auf den Eingabepfad {0} verweigert. Prüfen Sie Berechtigungen und die Verfügbarkeit des lokalen oder Netzwerkordners.");
        internal static string InputFileIsEmpty =>
            Get("Файл {0} пуст и не может быть использован как медиаисточник.", "File {0} is empty and cannot be used as a media source.", "Die Datei {0} ist leer und kann nicht als Medienquelle verwendet werden.");
        internal static string MediaFileStableWaitTimeout =>
            Get(
                "Файл {0} не стабилизировался за {1}: размер или время изменения продолжали меняться, либо файл не появился по пути.",
                "File {0} did not stabilize within {1}: size or last write time kept changing, or the file did not appear.",
                "Datei {0} wurde innerhalb von {1} nicht stabil: Größe oder letzte Änderung änderten sich weiter, oder die Datei ist nicht aufgetaucht.");
        internal static string InputFileIsStillBeingWritten =>
            Get("Файл {0} всё ещё дописывается и не стабилизировался за {1}.", "File {0} is still being written and did not stabilize within {1}.", "Die Datei {0} wird noch geschrieben und wurde innerhalb von {1} nicht stabil.");
        internal static string InputFileDoesNotContainAudioStream =>
            Get("Входной файл не содержит аудиодорожку.", "Input file does not contain an audio stream.", "Die Eingabedatei enthält keinen Audiostream.");
        internal static string InputFileDoesNotContainVideoStream =>
            Get("Входной файл не содержит видеодорожку.", "Input file does not contain a video stream.", "Die Eingabedatei enthält keinen Videostream.");
        internal static string InputFileDoesNotContainSubtitleStream =>
            Get("Входной файл не содержит дорожку субтитров.", "Input file does not contain a subtitle stream.", "Die Eingabedatei enthält keinen Untertitel-Stream.");
        internal static string StreamIndexOutOfRange =>
            Get("Индекс потока выходит за допустимые границы.", "Stream index is out of range.", "Der Stream-Index liegt außerhalb des gültigen Bereichs.");
        internal static string StreamCodecNotSupported =>
            Get("Выбранный кодек потока не поддерживается для данной операции или контейнера.", "Selected stream codec is not supported for this operation or container.", "Der ausgewählte Stream-Codec wird für diesen Vorgang oder Container nicht unterstützt.");
        internal static string StreamMappingFailed =>
            Get("Не удалось сопоставить указанные потоки MediaOrchestrator.", "Failed to map the requested MediaOrchestrator streams.", "Die angeforderten MediaOrchestrator-Streams konnten nicht zugeordnet werden.");
        internal static string OutputPathAccessDenied =>
            Get("Не удалось получить доступ к выходному пути {0}. Проверьте права доступа и доступность локальной или сетевой папки.", "Failed to access output path {0}. Check permissions and the availability of the local or network folder.", "Auf den Ausgabepfad {0} konnte nicht zugegriffen werden. Prüfen Sie Berechtigungen und die Verfügbarkeit des lokalen oder Netzwerkordners.");
        internal static string OutputDirectoryIsNotWritable =>
            Get("Выходная директория {0} недоступна для записи. Проверьте права доступа и доступность локальной или сетевой папки.", "Output directory {0} is not writable. Check permissions and the availability of the local or network folder.", "Das Ausgabeverzeichnis {0} ist nicht beschreibbar. Prüfen Sie Berechtigungen und die Verfügbarkeit des lokalen oder Netzwerkordners.");
        internal static string ExecutablesPathAccessDenied =>
            Get("Нет доступа к каталогу бинарных файлов MediaOrchestrator {0}. Проверьте права доступа и доступность локальной или сетевой папки.", "Access denied to MediaOrchestrator binaries directory {0}. Check permissions and the availability of the local or network folder.", "Zugriff auf das Verzeichnis der MediaOrchestrator-Binärdateien {0} verweigert. Prüfen Sie Berechtigungen und die Verfügbarkeit des lokalen oder Netzwerkordners.");
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
        internal static string InsufficientDiskSpace =>
            Get(
                "Недостаточно места на диске для завершения операции. Освободите место или выберите другой выходной путь.",
                "Insufficient disk space to complete the operation. Free some space or choose a different output path.",
                "Nicht genug Speicherplatz auf dem Datenträger, um den Vorgang abzuschließen. Geben Sie Speicher frei oder wählen Sie einen anderen Ausgabepfad.");

        internal static string HostedVideoDownloaderNotFound(string downloader)
        {
            return Get(
                $"Загрузчик '{downloader}' не найден или не может быть запущен.",
                $"Downloader '{downloader}' was not found or could not be started.",
                $"Downloader '{downloader}' wurde nicht gefunden oder konnte nicht gestartet werden.");
        }

        internal static string HostedVideoDownloadFailed(string downloader, int exitCode)
        {
            return Get(
                $"Загрузчик '{downloader}' завершился с кодом {exitCode}.",
                $"Downloader '{downloader}' exited with code {exitCode}.",
                $"Downloader '{downloader}' wurde mit Code {exitCode} beendet.");
        }

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

    // Технические паттерны stderr MediaOrchestrator для распознавания ошибок.
    // Не переводятся, потому что зависят от текста вывода MediaOrchestrator.
    internal static class MediaToolLogPatterns
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
        internal const string StreamMatchesNoStreams = "matches no streams";
        internal const string StreamMap = "Stream map";
        internal const string CodecNotCurrentlySupportedInContainer = "codec not currently supported in container";
        internal const string CouldNotFindTagForCodec = "Could not find tag for codec";
        internal const string UnsupportedCodec = "unsupported codec";
    }
}
