using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Events;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Позволяет подготовить и запустить процесс конвертации.
    /// </summary>
    public interface IConversion
    {
        /// <summary>
        ///     Путь к выходному файлу.
        /// </summary>
        string OutputFilePath { get; }

        /// <summary>
        ///     Дескриптор канала вывода.
        /// </summary>
        PipeDescriptor? OutputPipeDescriptor { get; }

        /// <summary>
        ///     Раздел аудио-настроек конвертации.
        /// </summary>
        IAudioConversionSettings Audio { get; }

        /// <summary>
        ///     Раздел видео-настроек конвертации.
        /// </summary>
        IVideoConversionSettings Video { get; }

        /// <summary>
        ///     Устанавливает приоритет процесса MediaOrchestrator.
        /// </summary>
        /// <param name="priority">Приоритет процесса MediaOrchestrator.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetPriority(ProcessPriorityClass? priority);

        /// <summary>
        ///     Извлекает каждый frameNo-й кадр и сохраняет его как изображение.
        /// </summary>
        /// <param name="frameNo">Интервал, через который будут выбираться кадры.</param>
        /// <param name="buildOutputFileName">Функция для генерации имени файла при выводе нескольких изображений.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion ExtractEveryNthFrame(int frameNo, Func<string, string> buildOutputFileName);

        /// <summary>
        ///     Извлекает конкретный кадр и сохраняет его как изображение.
        /// </summary>
        /// <param name="frameNo">Номер кадра, который необходимо извлечь.</param>
        /// <param name="buildOutputFileName">Функция для генерации имени файла при выводе изображения.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion ExtractNthFrame(int frameNo, Func<string, string> buildOutputFileName);

        /// <summary>
        ///     Собирает видео из последовательности изображений с заданного номера.
        /// </summary>
        /// <param name="startNumber">Номер первого изображения.</param>
        /// <param name="buildInputFileName">Функция для генерации имени входного файла.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion BuildVideoFromImages(int startNumber, Func<string, string> buildInputFileName);

        /// <summary>
        ///     Собирает видео из списка изображений.
        /// </summary>
        /// <param name="imageFiles">Список файлов-изображений.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion BuildVideoFromImages(IEnumerable<string> imageFiles);

        /// <summary>
        ///     Устанавливает частоту кадров выходного видео для опции -framerate и -r.
        /// </summary>
        /// <param name="frameRate">Желаемая частота кадров.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetFrameRate(double frameRate);

        /// <summary>
        ///     Устанавливает частоту кадров для входного потока MediaOrchestrator.
        /// </summary>
        /// <param name="frameRate">Желаемая входная частота кадров.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetInputFrameRate(double frameRate);

        /// <summary>
        ///     Перемещает позицию чтения в выходном файле (параметр -ss).
        /// </summary>
        /// <param name="seek">Позиция смещения.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetSeek(TimeSpan? seek);

        /// <summary>
        ///     Ограничивает продолжительность входного потока (-t до входа).
        /// </summary>
        /// <param name="seek">Ограничение длины входа.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetInputTime(TimeSpan? seek);

        /// <summary>
        ///     Ограничивает продолжительность выходного файла (-t после входа).
        /// </summary>
        /// <param name="seek">Ограничение длины выхода.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetOutputTime(TimeSpan? seek);

        /// <summary>
        ///     Устанавливает пресет сжатия (предустановку MediaOrchestrator).
        /// </summary>
        /// <param name="preset">Выбранный пресет.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetPreset(ConversionPreset preset);

        /// <summary>
        ///     Устанавливает профиль настройки кодировщика MediaOrchestrator (`-tune`).
        /// </summary>
        /// <param name="tune">Выбранный профиль.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetTune(ConversionTune tune);

        /// <summary>
        ///     Задает формат хеша для вывода.
        /// </summary>
        /// <param name="format">Желаемый формат хеширования.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetHashFormat(Hash format = Hash.SHA256);

        /// <summary>
        ///     Задает формат хеша для вывода.
        /// </summary>
        /// <param name="format">Строковое представление формата хеширования.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetHashFormat(string format);

        /// <summary>
        ///     Устанавливает битрейт для видеопотоков.
        /// </summary>
        /// <param name="bitrate">Битрейт в битах.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetVideoBitrate(long bitrate);

        /// <summary>
        ///     Устанавливает битрейт для аудиопотоков.
        /// </summary>
        /// <param name="bitrate">Битрейт в битах.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetAudioBitrate(long bitrate);

        /// <summary>
        ///     Задает фиксированное количество потоков MediaOrchestrator.
        /// </summary>
        /// <param name="threadCount">Число потоков.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion UseMultiThread(int threadCount);

        /// <summary>
        ///     Позволяет автоматически использовать все ядра процессора при необходимости (максимум 16).
        /// </summary>
        /// <param name="multiThread">Использовать ли многопоточность.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion UseMultiThread(bool multiThread);

        /// <summary>
        ///     Устанавливает путь к выходному файлу.
        /// </summary>
        /// <param name="outputPath">Путь к файлу.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetOutput(string outputPath);

        /// <summary>
        ///     Направляет вывод MediaOrchestrator в pipe.
        /// </summary>
        /// <param name="descriptor">Выбранный дескриптор pipe.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion PipeOutput(PipeDescriptor descriptor = PipeDescriptor.stdout);

        /// <summary>
        ///     Передаёт входной поток в MediaOrchestrator через pipe.
        /// </summary>
        /// <param name="inputStream">Поток, из которого читаются данные.</param>
        /// <param name="inputSpecifier">Спецификатор pipe (по умолчанию pipe:0).</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion PipeInput(Stream inputStream, string inputSpecifier = "pipe:0");

        /// <summary>
        ///     Определяет поведение перезаписи выходного файла.
        /// </summary>
        /// <param name="overwrite">Перезаписывать существующий файл.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetOverwriteOutput(bool overwrite);

        /// <summary>
        ///     Задает формат входного файла через параметр -f перед входом.
        /// </summary>
        /// <param name="inputFormat">Желаемый формат.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetInputFormat(string inputFormat);

        /// <summary>
        ///     Задает формат входного файла через параметр -f перед входом.
        /// </summary>
        /// <param name="inputFormat">Формат из перечисления Format.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetInputFormat(Format inputFormat);

        /// <summary>
        ///     Задает формат выходного файла через параметр -f после входов.
        /// </summary>
        /// <param name="outputFormat">Формат из перечисления Format.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetOutputFormat(Format outputFormat);

        /// <summary>
        ///     Задает формат выходного файла через параметр -f после входов.
        /// </summary>
        /// <param name="outputFormat">Строковое представление формата.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetOutputFormat(string outputFormat);

        /// <summary>
        ///     Устанавливает формат пикселей для выходного видео.
        /// </summary>
        /// <param name="pixelFormat">Строковое имя формата.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetPixelFormat(string pixelFormat);

        /// <summary>
        ///     Устанавливает формат пикселей для выходного видео.
        /// </summary>
        /// <param name="pixelFormat">Формат из перечисления PixelFormat.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetPixelFormat(PixelFormat pixelFormat);

        /// <summary>
        ///     Событие обновления прогресса MediaOrchestrator.
        /// </summary>
        event ConversionProgressEventHandler OnProgress;

        /// <summary>
        ///     Событие, возникающее при выводе текста MediaOrchestrator.
        /// </summary>
        event DataReceivedEventHandler OnDataReceived;

        /// <summary>
        ///     Событие, возникающее при получении видеоданных из pipe (требует PipeOutput()).
        /// </summary>
        event VideoDataEventHandler OnVideoDataReceived;

        /// <summary>
        ///     Задаёт получатель прогресса для передачи в вызывающий сервис (HTTP, gRPC и т.д.) через стандартный <see cref="IProgress{T}"/>.
        ///     Уведомления приходят с потока чтения stderr MediaOrchestrator; при необходимости маршалите в UI-синхронизационный контекст сами.
        /// </summary>
        /// <param name="progressReporter">Репортер или null, чтобы не вызывать.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetProgressReporter(IProgress<ConversionProgressEventArgs> progressReporter);

        /// <summary>
        ///     Завершает кодирование, когда заканчивается самый короткий входной поток (-shortest).
        /// </summary>
        /// <param name="useShortest">Признак выключения ускоренного завершения.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion UseShortest(bool useShortest);

        /// <summary>
        ///     Собирает аргументы MediaOrchestrator для конвертации.
        /// </summary>
        /// <returns>Строка аргументов.</returns>
        string Build();

        /// <summary>
        ///     Запускает конвертацию с текущими параметрами.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <param name="progress">Дополнительный репортер на время этого запуска; если null, используется <see cref="SetProgressReporter"/>.</param>
        /// <returns>Результат конвертации.</returns>
        /// <exception cref="ConversionException">Возникает, когда процесс MediaOrchestrator возвращает ошибку.</exception>
        /// <exception cref="ArgumentException">Возникает, когда исполняемые файлы MediaOrchestrator не найдены.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="TaskCanceledException">Возникает, когда задача была отменена.</exception>
        Task<IConversionResult> Start(CancellationToken cancellationToken = default, IProgress<ConversionProgressEventArgs> progress = null);

        /// <summary>
        ///     Запускает MediaOrchestrator с указанными параметрами и токеном отмены.
        /// </summary>
        /// <param name="parameters">Строка параметров MediaOrchestrator.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <param name="progress">Дополнительный репортер на время этого запуска; если null, используется <see cref="SetProgressReporter"/>.</param>
        /// <returns>Результат конвертации.</returns>
        /// <exception cref="ConversionException">Возникает, когда процесс MediaOrchestrator возвращает ошибку.</exception>
        /// <exception cref="ArgumentException">Возникает, когда исполняемые файлы MediaOrchestrator не найдены.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="TaskCanceledException">Возникает, когда задача была отменена.</exception>
        Task<IConversionResult> Start(string parameters, CancellationToken cancellationToken = default, IProgress<ConversionProgressEventArgs> progress = null);

        /// <summary>
        ///     Добавляет дополнительный параметр в аргументы MediaOrchestrator.
        /// </summary>
        /// <param name="parameter">Параметр в виде строки.</param>
        /// <param name="parameterPosition">Позиция параметра относительно входа.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddParameter(string parameter, ParameterPosition parameterPosition = ParameterPosition.PostInput);

        /// <summary>
        ///     Отключает видеовывод (-vn).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion DisableVideo();

        /// <summary>
        ///     Сопоставляет все входные потоки (`-map 0`).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion MapAllStreams();

        /// <summary>
        ///     Сопоставляет все аудиопотоки входа (`-map 0:a?`).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion MapAudioStreams();

        /// <summary>
        ///     Сопоставляет аудиопоток входа по индексам.
        /// </summary>
        /// <param name="inputIndex">Индекс входа.</param>
        /// <param name="audioStreamIndex">Индекс аудиопотока.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion MapAudioStream(int inputIndex = 0, int audioStreamIndex = 0);

        /// <summary>
        ///     Включает копирование всех кодеков (`-c copy`).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion CopyAllCodecs();

        /// <summary>
        ///     Включает копирование аудиокодека (`-c:a copy`).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion CopyAudioCodec();

        /// <summary>
        ///     Устанавливает аудиокодек выхода.
        /// </summary>
        /// <param name="codec">Кодек аудио.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetAudioCodec(AudioCodec codec);

        /// <summary>
        ///     Устанавливает частоту дискретизации аудио.
        /// </summary>
        /// <param name="sampleRate">Частота дискретизации в Гц.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetAudioSampleRate(int sampleRate);

        /// <summary>
        ///     Устанавливает число аудиоканалов.
        /// </summary>
        /// <param name="channels">Количество каналов.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetAudioChannels(int channels);

        /// <summary>
        ///     Отключает субтитры в выходном файле (-sn).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion DisableSubtitles();

        /// <summary>
        ///     Указывает входной файл напрямую как параметр `-i`.
        /// </summary>
        /// <param name="inputPath">Путь или URI ко входу.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddInput(string inputPath);

        /// <summary>
        ///     Добавляет типизированный источник входа.
        /// </summary>
        /// <param name="inputSource">Описание входного источника.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddInput(InputSource inputSource);

        /// <summary>
        ///     Включает перечисление доступных устройств захвата (`-list_devices true`).
        /// </summary>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion EnableDeviceListing();

        /// <summary>
        ///     Добавляет входной источник lavfi.
        /// </summary>
        /// <param name="filterGraph">Граф фильтра для lavfi.</param>
        /// <param name="inputDuration">Необязательная длительность входа.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddLavfiInput(string filterGraph, TimeSpan? inputDuration = null);

        /// <summary>
        ///     Добавляет `-filter_complex` с указанным графом.
        /// </summary>
        /// <param name="filterGraph">Тело графа фильтров.</param>
        /// <returns>Текущий объект IConversion.</returns>
        [Obsolete("Use UseFilterGraph(FilterGraph) to avoid raw string filter graphs.", false)]
        IConversion SetFilterComplex(string filterGraph);

        /// <summary>
        ///     Добавляет `-filter_complex` из типизированного графа.
        /// </summary>
        /// <param name="filterGraph">Граф фильтров.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion UseFilterGraph(FilterGraph filterGraph);

        /// <summary>
        ///     Сопоставляет именованный выход фильтра.
        /// </summary>
        /// <param name="label">Метка выхода, например `v`.</param>
        /// <returns>Текущий объект IConversion.</returns>
        [Obsolete("Use MapFilterOutput(FilterLabel) to avoid raw string filter labels.", false)]
        IConversion MapFilterOutput(string label);

        /// <summary>
        ///     Сопоставляет выход типизированного фильтра.
        /// </summary>
        /// <param name="label">Метка выхода фильтра.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion MapFilterOutput(FilterLabel label);

        /// <summary>
        ///     Сопоставляет несколько типизированных выходов фильтра.
        /// </summary>
        /// <param name="labels">Метки выходов фильтра.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion MapFilterOutputs(params FilterLabel[] labels);

        /// <summary>
        ///     Устанавливает aspect ratio выхода.
        /// </summary>
        /// <param name="ratio">Строковое значение aspect ratio.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetAspectRatio(string ratio);

        /// <summary>
        ///     Добавляет один или несколько потоков в выходной файл.
        /// </summary>
        /// <param name="streams">Потоки для добавления.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddStream<T>(params T[] streams) where T : IStream;

        /// <summary>
        ///     Добавляет набор потоков в выходной файл.
        /// </summary>
        /// <param name="streams">Коллекция потоков.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddStream(IEnumerable<IStream> streams);

        /// <summary>
        ///     Включает аппаратное ускорение для кодирования и декодирования.
        /// </summary>
        /// <param name="hardwareAccelerator">Название ускорителя.</param>
        /// <param name="decoder">Кодек декодирования.</param>
        /// <param name="encoder">Кодек кодирования.</param>
        /// <param name="device">Номер устройства (0 по умолчанию).</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion UseHardwareAcceleration(HardwareAccelerator hardwareAccelerator, VideoCodec decoder, VideoCodec encoder, int device = 0);

        /// <summary>
        ///     Включает аппаратное ускорение при помощи строковых параметров.
        /// </summary>
        /// <param name="hardwareAccelerator">Название ускорителя.</param>
        /// <param name="decoder">Кодек декодирования.</param>
        /// <param name="encoder">Кодек кодирования.</param>
        /// <param name="device">Номер устройства, если их несколько.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion UseHardwareAcceleration(string hardwareAccelerator, string decoder, string encoder, int device = 0);

        /// <summary>
        ///     Задает метод синхронизации видео для MediaOrchestrator.
        /// </summary>
        /// <param name="method">Метод синхронизации.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion SetVideoSyncMethod(VideoSyncMethod method);

        /// <summary>
        ///     Перечисление всех потоков, добавленных в конвертацию.
        /// </summary>
        IEnumerable<IStream> Streams { get; }

        /// <summary>
        ///     Добавляет поток захвата рабочего стола по параметрам.
        /// </summary>
        /// <param name="videoSize">Размер видео.</param>
        /// <param name="framerate">Частота кадров.</param>
        /// <param name="xOffset">Смещение по X.</param>
        /// <param name="yOffset">Смещение по Y.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddDesktopStream(string videoSize = null, double framerate = 30, int xOffset = 0, int yOffset = 0);

        /// <summary>
        ///     Добавляет поток захвата рабочего стола с помощью типизированного размера.
        /// </summary>
        /// <param name="videoSize">Размер видео из перечисления VideoSize.</param>
        /// <param name="framerate">Частота кадров.</param>
        /// <param name="xOffset">Смещение по X.</param>
        /// <param name="yOffset">Смещение по Y.</param>
        /// <returns>Текущий объект IConversion.</returns>
        IConversion AddDesktopStream(VideoSize videoSize, double framerate = 30, int xOffset = 0, int yOffset = 0);
    }
}
