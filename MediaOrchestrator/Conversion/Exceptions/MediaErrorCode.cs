namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Стабильный код ошибки библиотеки MediaOrchestrator для документации, мониторинга и аналитики.
    /// </summary>
    public enum MediaErrorCode
    {
        /// <summary>
        ///     Неизвестная или неклассифицированная ошибка.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Общая ошибка конвертации MediaOrchestrator.
        /// </summary>
        ConversionFailed = 1000,

        /// <summary>
        ///     Не удалось подобрать декодер для входных данных.
        /// </summary>
        UnknownDecoder = 1001,

        /// <summary>
        ///     Указанный аппаратный ускоритель недоступен.
        /// </summary>
        HardwareAcceleratorNotFound = 1002,

        /// <summary>
        ///     MediaOrchestrator не смог определить подходящий выходной формат.
        /// </summary>
        NoSuitableOutputFormat = 1003,

        /// <summary>
        ///     Указанный bitstream filter не поддерживается.
        /// </summary>
        InvalidBitstreamFilter = 1004,

        /// <summary>
        ///     MediaOrchestrator не смог сопоставить запрошенные потоки.
        /// </summary>
        StreamMappingFailed = 1005,

        /// <summary>
        ///     Кодек потока не поддерживается контейнером или операцией.
        /// </summary>
        StreamCodecNotSupported = 1006,

        /// <summary>
        ///     Недостаточно места на диске для записи результата.
        /// </summary>
        InsufficientDiskSpace = 1007,

        /// <summary>
        ///     Исполняемые файлы MediaOrchestrator или FFprobe не найдены.
        /// </summary>
        ExecutableNotFound = 2000,

        /// <summary>
        ///     Каталог исполняемых файлов MediaOrchestrator недоступен.
        /// </summary>
        ExecutablesPathAccessDenied = 2001,

        /// <summary>
        ///     Найденный исполняемый файл не соответствует ожидаемой сигнатуре платформы.
        /// </summary>
        ExecutableSignatureMismatch = 2002,

        /// <summary>
        ///     Ошибка входных данных.
        /// </summary>
        InvalidInput = 3000,

        /// <summary>
        ///     Входной файл не может быть прочитан.
        /// </summary>
        InputFileUnreadable = 3001,

        /// <summary>
        ///     Входной путь недоступен по правам доступа.
        /// </summary>
        InputPathAccessDenied = 3002,

        /// <summary>
        ///     Входной файл пуст.
        /// </summary>
        InputFileEmpty = 3003,

        /// <summary>
        ///     Входной файл заблокирован другим процессом.
        /// </summary>
        InputFileLocked = 3004,

        /// <summary>
        ///     Входной файл всё ещё записывается и не стабилизирован.
        /// </summary>
        InputFileStillBeingWritten = 3005,

        /// <summary>
        ///     Сигнатура входного файла не соответствует ожидаемому медиаформату.
        /// </summary>
        InputFileSignatureMismatch = 3006,

        /// <summary>
        ///     Во входном файле отсутствует аудиопоток.
        /// </summary>
        AudioStreamNotFound = 3007,

        /// <summary>
        ///     Во входном файле отсутствует видеопоток.
        /// </summary>
        VideoStreamNotFound = 3008,

        /// <summary>
        ///     Во входном файле отсутствует поток субтитров.
        /// </summary>
        SubtitleStreamNotFound = 3009,

        /// <summary>
        ///     Запрошен индекс потока вне допустимого диапазона.
        /// </summary>
        StreamIndexOutOfRange = 3010,

        /// <summary>
        ///     Выходной путь недоступен.
        /// </summary>
        OutputPathAccessDenied = 4000,

        /// <summary>
        ///     Выходная директория недоступна для записи.
        /// </summary>
        OutputDirectoryNotWritable = 4001,

        /// <summary>
        ///     Ошибка скачивания видео с видеохостинга.
        /// </summary>
        HostedVideoDownloadFailed = 5000,

        /// <summary>
        ///     Операция была отменена.
        /// </summary>
        OperationCanceled = 9000
    }
}
