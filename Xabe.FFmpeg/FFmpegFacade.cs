using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg.Analytics;
using Xabe.FFmpeg.Streams.SubtitleStream;

namespace Xabe.FFmpeg
{
    /// <summary> 
    ///     Обертка для FFmpeg
    /// </summary>
    public abstract partial class FFmpeg
    {
        /// <summary>
        ///     Директория, содержащая FFmpeg и FFprobe
        /// </summary>
        public static string ExecutablesPath
        {
            get
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    return _executablesPath;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Метод фильтрации для поиска файлов FFmpeg и FFprobe
        /// </summary>
        public static FileNameFilterMethod FilterMethod
        {
            get
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    return _filterMethod;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Выбирает, должен ли метод фильтрации учитывать регистр
        ///     Это будет использоваться для сравнения имен файлов
        /// </summary>
        public static IFormatProvider FormatProvider
        {
            get
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    return _formatProvider;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Включает или отключает кэширование MediaInfo.
        /// </summary>
        public static bool MediaInfoCacheEnabled { get; set; } = true;

        /// <summary>
        ///     Время жизни записи в кэше MediaInfo.
        /// </summary>
        public static TimeSpan MediaInfoCacheLifetime { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        ///     Получает новый экземпляр Conversion.
        /// </summary>
        /// <returns>Объект IConversion.</returns>
        public static Conversions Conversions = new Conversions();

        /// <summary>
        ///     Слой аналитики и выбора сценариев обработки.
        /// </summary>
        public static MediaProcessingAnalytics Analytics { get; } = new MediaProcessingAnalytics();

        /// <summary>
        ///     Получает MediaInfo из файла.
        /// </summary>
        /// <param name="fileName">Полный путь к файлу</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <param name="waitUntilFileStable">Если true — перед ffprobe дождаться появления и стабилизации локального файла (см. <see cref="MediaFileReadiness"/>).</param>
        /// <param name="stabilityQuietPeriod">Интервал «тишины» при стабилизации; по умолчанию <see cref="MediaFileReadiness.DefaultStabilityQuietPeriod"/>.</param>
        /// <param name="maximumWaitForStable">Максимальное ожидание появления/стабилизации; по умолчанию <see cref="MediaFileReadiness.DefaultMaximumWait"/>.</param>
        /// <exception cref="ArgumentException">Файл не существует</exception>
        /// <exception cref="TaskCanceledException">Операция отменена или занимает слишком много времени</exception>
        /// <exception cref="TimeoutException">Истекло ожидание стабилизации при <paramref name="waitUntilFileStable"/>.</exception>
        public static async Task<IMediaInfo> GetMediaInfo(
            string fileName,
            CancellationToken cancellationToken = default,
            bool waitUntilFileStable = false,
            TimeSpan? stabilityQuietPeriod = null,
            TimeSpan? maximumWaitForStable = null)
        {
            if (waitUntilFileStable)
            {
                await MediaFileReadiness.WaitUntilStableAsync(
                        fileName,
                        stabilityQuietPeriod,
                        null,
                        maximumWaitForStable,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            EnsureExecutablesLocated(cancellationToken);
            return await MediaInfo.Get(fileName, cancellationToken);
        }

        /// <summary>
        ///     Устанавливает путь к директории, содержащей FFmpeg и FFprobe
        /// </summary>
        /// <param name="directoryWithFFmpegAndFFprobe"></param>
        /// <param name="ffmpegExeutableName">Имя исполняемого файла FFmpeg</param>
        /// <param name="ffprobeExecutableName">Имя исполняемого файла FFprobe</param>
        /// <param name="filteringMethod">Выбирает метод сравнения имен файлов</param>
        /// <param name="formatprovider">Провайдер формата для сравнения строк</param>
        /// <param name="language">Язык локализации сообщений исключений</param>
        /// <param name="maxOutputVideoFrameRate">Необязательный лимит частоты кадров выходного видео (максимум).</param>
        /// <param name="maxOutputAudioSampleRate">Необязательный лимит частоты дискретизации выходного аудио в Гц (максимум).</param>
        /// <param name="maxOutputAudioChannels">Необязательный лимит числа каналов выходного аудио (максимум).</param>
        /// <param name="tryDetectHardwareAcceleration">Если true — выполняется <c>ffmpeg -hwaccels</c> и выбирается ускоритель с учётом ОС (NVIDIA, Intel QSV, AMD AMF через D3D11, VAAPI, Video Toolbox).</param>
        /// <param name="cancellationToken">Отмена во время автоопределения HW (процесс ffmpeg -hwaccels).</param>
        public static void SetExecutablesPath(
            string directoryWithFFmpegAndFFprobe,
            string ffmpegExeutableName = "ffmpeg",
            string ffprobeExecutableName = "ffprobe",
            FileNameFilterMethod filteringMethod = FileNameFilterMethod.Contains,
            IFormatProvider formatprovider = null,
            LocalizationLanguage language = LocalizationLanguage.English,
            double? maxOutputVideoFrameRate = null,
            int? maxOutputAudioSampleRate = null,
            int? maxOutputAudioChannels = null,
            bool tryDetectHardwareAcceleration = false,
            CancellationToken cancellationToken = default)
        {
            _executableConfigurationLock.EnterWriteLock();
            try
            {
                _executablesPath = directoryWithFFmpegAndFFprobe == null ? null : new DirectoryInfo(directoryWithFFmpegAndFFprobe).FullName;
                _filterMethod = filteringMethod;
                _formatProvider = formatprovider ?? CultureInfo.CurrentCulture;
                _ffmpegExecutableName = ffmpegExeutableName;
                _ffprobeExecutableName = ffprobeExecutableName;
                _lastExecutablePathMarker = null;
                _lastHardwareAccelerationDetectionMarker = null;
                _ffmpegPath = null;
                _ffprobePath = null;
                _autoDetectedHardwareAccelerationProfile = null;
            }
            finally
            {
                _executableConfigurationLock.ExitWriteLock();
            }

            LocalizationManager.Initialize(language);
            if (maxOutputVideoFrameRate != null || maxOutputAudioSampleRate != null || maxOutputAudioChannels != null)
            {
                SetGlobalOutputLimits(maxOutputVideoFrameRate, maxOutputAudioSampleRate, maxOutputAudioChannels);
            }

            if (!tryDetectHardwareAcceleration)
            {
                return;
            }
            
            EnsureExecutablesLocated(cancellationToken);
        }

        /// <summary>
        ///     Задаёт глобальные лимиты параметров выхода (без смены пути к FFmpeg). Null сбрасывает соответствующий лимит.
        /// </summary>
        /// <param name="maxOutputVideoFrameRate">Максимальная частота кадров видео.</param>
        /// <param name="maxOutputAudioSampleRate">Максимальная частота дискретизации аудио (Гц).</param>
        /// <param name="maxOutputAudioChannels">Максимальное число каналов аудио.</param>
        public static void SetGlobalOutputLimits(double? maxOutputVideoFrameRate = null, int? maxOutputAudioSampleRate = null, int? maxOutputAudioChannels = null)
        {
            if (maxOutputVideoFrameRate.HasValue && maxOutputVideoFrameRate.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxOutputVideoFrameRate));
            }

            if (maxOutputAudioSampleRate.HasValue && maxOutputAudioSampleRate.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxOutputAudioSampleRate));
            }

            if (maxOutputAudioChannels.HasValue && maxOutputAudioChannels.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxOutputAudioChannels));
            }

            MaxOutputVideoFrameRate = maxOutputVideoFrameRate;
            MaxOutputAudioSampleRate = maxOutputAudioSampleRate;
            MaxOutputAudioChannels = maxOutputAudioChannels;
        }

        /// <summary>
        ///     Устанавливает язык локализации сообщений исключений.
        /// </summary>
        /// <param name="language">Язык локализации.</param>
        public static void SetLocalizationLanguage(LocalizationLanguage language = LocalizationLanguage.English)
        {
            LocalizationManager.Initialize(language);
        }

        /// <summary>
        ///     Получает доступные аудио и видео устройства (например, камеры или микрофоны)
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Список доступных устройств</returns>
        public static async Task<Device[]> GetAvailableDevices(CancellationToken cancellationToken = default)
        {
            return await Conversion.GetAvailableDevices(cancellationToken);
        }

        /// <summary>
        ///     Очищает кэш MediaInfo.
        /// </summary>
        public static void ClearMediaInfoCache()
        {
            MediaInfo.ClearCache();
        }

        /// <summary>
        ///     Скачивает видео с видеохостинга (YouTube, RuTube и т.п.) через yt-dlp/youtube-dl.
        /// </summary>
        public static Task DownloadHostedVideoAsync(string url, string outputPath, HostedVideoDownloadSettings settings = null, CancellationToken cancellationToken = default)
        {
            return Conversion.DownloadHostedVideoAsync(url, outputPath, settings, cancellationToken);
        }

        /// <summary>
        ///     Скачивает видео с видеохостинга по URI.
        /// </summary>
        public static Task DownloadHostedVideoAsync(Uri uri, string outputPath, HostedVideoDownloadSettings settings = null, CancellationToken cancellationToken = default)
        {
            return DownloadHostedVideoAsync(uri?.OriginalString ?? throw new ArgumentNullException(nameof(uri)), outputPath, settings, cancellationToken);
        }
    }

    public sealed class Conversions
    {
        /// <summary>
        ///     Получает новый экземпляр Conversion.
        /// </summary>
        /// <returns>Объект IConversion.</returns>
        public IConversion New()
        {
            return Conversion.New();
        }

        /// <summary>
        ///     Доступ к готовым сценариям конвертации.
        /// </summary>
        /// <returns>Объект Snippets.</returns>
        public readonly Snippets FromSnippet = new Snippets();

        internal Conversions()
        {

        }
    }

    public sealed class Snippets
    {
        internal Snippets()
        {

        }

        /// <summary>
        ///     Извлекает аудио из файла
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной видеопоток</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ExtractAudio(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.ExtractAudio(inputPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Извлекает аудио из файла с обязательной проверкой наличия аудиодорожки.
        ///     Входной файл может не содержать видеопоток.
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь аудио</param>
        /// <param name="audioCodec">Кодек выхода (по умолчанию mp3)</param>
        /// <param name="bitrate">Опциональный битрейт в битах</param>
        /// <param name="sampleRate">Опциональная частота дискретизации в Гц</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ExtractAudio(
            string inputPath,
            string outputPath,
            AudioCodec audioCodec = AudioCodec.mp3,
            long? bitrate = null,
            int? sampleRate = null,
            CancellationToken cancellationToken = default)
        {
            return await Conversion.ExtractAudio(inputPath, outputPath, audioCodec, bitrate, sampleRate, cancellationToken);
        }

        /// <summary>
        ///     Быстро сохраняет первую аудиодорожку входа в WAV (PCM s16le). Глобальные лимиты выхода не применяются.
        /// </summary>
        /// <param name="inputPath">Аудио- или видеофайл (берётся первая аудиодорожка).</param>
        /// <param name="outputPath">Путь к .wav.</param>
        /// <param name="sampleRate">Частота дискретизации (по умолчанию 16000 Гц).</param>
        /// <param name="channels">Число каналов (по умолчанию 1 — моно).</param>
        /// <returns>Объект конвертации.</returns>
        public Task<IConversion> ConvertToWav(string inputPath, string outputPath, int sampleRate = 16000, int channels = 1, CancellationToken cancellationToken = default)
        {
            return Conversion.ConvertToWavFastAsync(inputPath, outputPath, sampleRate, channels, cancellationToken);
        }

        /// <summary>
        ///     Normalizes audio for speech-to-text transcription.
        ///     By default, exports the first audio stream as mono WAV PCM s16le at 16 kHz.
        /// </summary>
        /// <param name="inputPath">Path to the input media file.</param>
        /// <param name="outputPath">Path to the normalized output audio file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Configured conversion.</returns>
        public Task<IConversion> NormalizeAudioForTranscription(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return Conversion.NormalizeAudioForTranscription(inputPath, outputPath, null, cancellationToken);
        }

        /// <summary>
        ///     Normalizes audio for speech-to-text transcription using custom settings.
        /// </summary>
        /// <param name="inputPath">Path to the input media file.</param>
        /// <param name="outputPath">Path to the normalized output audio file.</param>
        /// <param name="settings">Normalization settings for transcription audio.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Configured conversion.</returns>
        public Task<IConversion> NormalizeAudioForTranscription(
            string inputPath,
            string outputPath,
            TranscriptionAudioSettings settings,
            CancellationToken cancellationToken = default)
        {
            return Conversion.NormalizeAudioForTranscription(inputPath, outputPath, settings, cancellationToken);
        }

        /// <summary>
        ///     Добавляет аудиопоток к видеофайлу
        /// </summary>
        /// <param name="videoPath">Видео</param>
        /// <param name="audioPath">Аудио</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> AddAudio(string videoPath, string audioPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.AddAudio(videoPath, audioPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Разделяет медиафайл на части по таймкодам и конвертирует аудио в выбранный формат.
        ///     Работает с файлами как с видеорядом, так и без него, при наличии аудиодорожки.
        /// </summary>
        /// <param name="inputPath">Путь к входному файлу.</param>
        /// <param name="outputDirectory">Директория для выходных частей.</param>
        /// <param name="timecodes">Таймкоды разделения.</param>
        /// <param name="audioCodec">Аудиокодек выхода (по умолчанию mp3).</param>
        /// <param name="bitrate">Битрейт аудио в битах.</param>
        /// <param name="sampleRate">Частота дискретизации в Гц.</param>
        /// <returns>Список конвертаций, по одной на каждую часть.</returns>
        public async Task<IReadOnlyList<IConversion>> SplitAudioByTimecodes(
            string inputPath,
            string outputDirectory,
            IEnumerable<TimeSpan> timecodes,
            AudioCodec audioCodec = AudioCodec.mp3,
            long bitrate = 192000,
            int sampleRate = 44100,
            CancellationToken cancellationToken = default)
        {
            return await Conversion.SplitAudioByTimecodesAsync(inputPath, outputDirectory, timecodes, audioCodec, bitrate, sampleRate, cancellationToken);
        }

        /// <summary>
        ///     Конвертирует файл в MP4
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToMp4(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.ToMp4(inputPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Конвертирует файл в TS
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToTs(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.ToTs(inputPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Конвертирует файл в OGV
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToOgv(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.ToOgv(inputPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Конвертирует файл в WebM
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToWebM(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.ToWebM(inputPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Выполняет remux в WebM без перекодирования потоков.
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="keepSubtitles">Сохранять ли потоки субтитров</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> RemuxToWebM(string inputPath, string outputPath, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return await Conversion.RemuxToWebM(inputPath, outputPath, keepSubtitles, cancellationToken);
        }

        /// <summary>
        ///     Конвертирует видеопоток изображений в gif
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="loop">Количество повторов</param>
        /// <param name="delay">Задержка между повторами (в секундах)</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToGif(string inputPath, string outputPath, int loop, int delay = 0, CancellationToken cancellationToken = default)
        {
            return await Conversion.ToGif(inputPath, outputPath, loop, delay, cancellationToken);
        }

        /// <summary>
        ///     Конвертирует один файл в другой с целевым форматом, используя аппаратное ускорение (если возможно). Использует cuvid. Работает только на Windows/Linux с видеокартой NVidia.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="outputFilePath">Путь к файлу</param>
        /// <param name="hardwareAccelerator">Аппаратный ускоритель. Список всех доступных ускорителей для вашей системы - "ffmpeg -hwaccels"</param>
        /// <param name="decoder">Кодек, используемый для декодирования входного видео (например, h264_cuvid)</param>
        /// <param name="encoder">Кодек, используемый для кодирования выходного видео (например, h264_nvenc)</param>
        /// <param name="device">Номер устройства (0 = видеокарта по умолчанию), если видеокарт больше одной.</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> ConvertWithHardware(string inputFilePath, string outputFilePath, HardwareAccelerator hardwareAccelerator, VideoCodec decoder, VideoCodec encoder, int device = 0, CancellationToken cancellationToken = default)
        {
            return await Conversion.ConvertWithHardwareAsync(inputFilePath, outputFilePath, hardwareAccelerator, decoder, encoder, device, cancellationToken);
        }

        /// <summary>
        ///     Добавляет субтитры к видеопотоку
        /// </summary>
        /// <param name="inputPath">Видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="subtitlesPath">Субтитры</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> BurnSubtitle(string inputPath, string outputPath, string subtitlesPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.AddSubtitlesAsync(inputPath, outputPath, subtitlesPath, cancellationToken);
        }

        /// <summary>
        ///     Добавляет субтитры к файлу. Они будут добавлены как новый поток, поэтому если вы хотите встроить субтитры в видео, используйте
        ///     метод BurnSubtitle.
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="subtitlePath">Путь к файлу субтитров в формате .srt</param>
        /// <param name="language">Код языка в ISO 639. Пример: "eng", "pol", "pl", "de", "ger"</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> AddSubtitle(string inputPath, string outputPath, string subtitlePath, string language = null, CancellationToken cancellationToken = default)
        {
            return await Conversion.AddSubtitleAsync(inputPath, outputPath, subtitlePath, language, cancellationToken);
        }

        /// <summary>
        ///     Добавляет субтитры в файл как новый поток.
        ///     Если нужно встроить субтитры в видео, используйте метод BurnSubtitle.
        /// </summary>
        /// <param name="inputPath">Входной путь.</param>
        /// <param name="outputPath">Выходной путь.</param>
        /// <param name="subtitlePath">Путь к файлу субтитров в формате .srt.</param>
        /// <param name="subtitleCodec">Кодек субтитров для кодирования субтитров</param>
        /// <param name="language">Код языка в ISO 639. Пример: "eng", "pol", "pl", "de", "ger".</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> AddSubtitle(string inputPath, string outputPath, string subtitlePath, SubtitleCodec subtitleCodec, string language = null, CancellationToken cancellationToken = default)
        {
            return await Conversion.AddSubtitleAsync(inputPath, outputPath, subtitlePath, subtitleCodec, language, cancellationToken);
        }

        /// <summary>
        ///     Встраивает водяной знак в видео
        /// </summary>
        /// <param name="inputPath">Входной путь к видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="inputImage">Водяной знак</param>
        /// <param name="position">Позиция водяного знака</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> SetWatermark(string inputPath, string outputPath, string inputImage, Position position, CancellationToken cancellationToken = default)
        {
            return await Conversion.SetWatermarkAsync(inputPath, outputPath, inputImage, position, cancellationToken);
        }

        /// <summary>
        ///     Вшивает в видео текстовую подпись у правого края кадра (drawtext).
        /// </summary>
        /// <param name="inputPath">Входной путь к видео.</param>
        /// <param name="outputPath">Выходной файл.</param>
        /// <param name="text">Текст подписи.</param>
        /// <param name="fontColor">Цвет шрифта.</param>
        /// <param name="fontSize">Размер шрифта.</param>
        /// <param name="marginRight">Отступ справа в пикселях.</param>
        /// <param name="marginY">Отступ сверху или снизу (см. <see cref="DrawTextVerticalAlign"/>).</param>
        /// <param name="verticalAlign">Вертикальное положение у правого края.</param>
        /// <param name="fontFilePath">Необязательный путь к шрифту (TTF/OTF).</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> BurnRightSideTextLabel(
            string inputPath,
            string outputPath,
            string text,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null,
            CancellationToken cancellationToken = default)
        {
            return await Conversion.BurnRightSideTextLabelAsync(inputPath, outputPath, text, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath, cancellationToken);
        }

        /// <summary>
        ///     Вшивает у правого края динамическое время: по умолчанию по PTS (ЧЧ:ММ:СС), опционально локальное время системы.
        ///     Для таймкода с полем «кадр» и заданным fps используйте <see cref="BurnRightSideSmpteTimecode"/>.
        /// </summary>
        /// <param name="inputPath">Входной путь к видео.</param>
        /// <param name="outputPath">Выходной файл.</param>
        /// <param name="prefix">Текст перед временем.</param>
        /// <param name="suffix">Текст после времени.</param>
        /// <param name="useLocalWallClock">True — %{localtime}, false — %{pts:hms}.</param>
        /// <param name="fontColor">Цвет шрифта.</param>
        /// <param name="fontSize">Размер шрифта.</param>
        /// <param name="marginRight">Отступ справа.</param>
        /// <param name="marginY">Отступ сверху/снизу.</param>
        /// <param name="verticalAlign">Вертикальное выравнивание.</param>
        /// <param name="fontFilePath">Необязательный путь к шрифту.</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> BurnRightSidePtsTimeLabel(
            string inputPath,
            string outputPath,
            string prefix = null,
            string suffix = null,
            bool useLocalWallClock = false,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null,
            CancellationToken cancellationToken = default)
        {
            return await Conversion.BurnRightSidePtsTimeLabelAsync(inputPath, outputPath, prefix, suffix, useLocalWallClock, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath, cancellationToken);
        }

        /// <summary>
        ///     Вшивает у правого края таймкод в стиле SMPTE (drawtext timecode/rate): поля ЧЧ:ММ:СС:кадр с заданным fps.
        /// </summary>
        /// <param name="inputPath">Входной путь к видео.</param>
        /// <param name="outputPath">Выходной файл.</param>
        /// <param name="startTimecode">Начальное значение (например 00:00:00:00).</param>
        /// <param name="frameRate">Частота кадров (25, 29.97 и т.д.).</param>
        /// <param name="fontColor">Цвет шрифта.</param>
        /// <param name="fontSize">Размер шрифта.</param>
        /// <param name="marginRight">Отступ справа.</param>
        /// <param name="marginY">Отступ сверху/снизу.</param>
        /// <param name="verticalAlign">Вертикальное выравнивание.</param>
        /// <param name="fontFilePath">Необязательный путь к шрифту.</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> BurnRightSideSmpteTimecode(
            string inputPath,
            string outputPath,
            string startTimecode = "00:00:00:00",
            double frameRate = 25,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null,
            CancellationToken cancellationToken = default)
        {
            return await Conversion.BurnRightSideSmpteTimecodeAsync(inputPath, outputPath, startTimecode, frameRate, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath, cancellationToken);
        }

        /// <summary>
        ///     Извлекает видео из файла
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной аудиопоток</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ExtractVideo(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return await Conversion.ExtractVideoAsync(inputPath, outputPath, cancellationToken);
        }

        /// <summary>
        ///     Сохраняет снимок видео
        /// </summary>
        /// <param name="inputPath">Видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="captureTime">Временной интервал снимка</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> Snapshot(string inputPath, string outputPath, TimeSpan captureTime, CancellationToken cancellationToken = default)
        {
            return await Conversion.SnapshotAsync(inputPath, outputPath, captureTime, cancellationToken);
        }

        /// <summary>
        ///     Изменяет размер видео
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="width">Ожидаемая ширина</param>
        /// <param name="height">Ожидаемая высота</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ChangeSize(string inputPath, string outputPath, int width, int height, CancellationToken cancellationToken = default)
        {
            return await Conversion.ChangeSizeAsync(inputPath, outputPath, width, height, cancellationToken);
        }

        /// <summary>
        ///     Изменяет размер видео.
        /// </summary>
        /// <param name="inputPath">Входной путь.</param>
        /// <param name="outputPath">Выходной путь.</param>
        /// <param name="size">Ожидаемый размер</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> ChangeSize(string inputPath, string outputPath, VideoSize size, CancellationToken cancellationToken = default)
        {
            return await Conversion.ChangeSizeAsync(inputPath, outputPath, size, cancellationToken);
        }

        /// <summary>
        ///     Получает часть видео
        /// </summary>
        /// <param name="inputPath">Видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="startTime">Начальная точка</param>
        /// <param name="duration">Длительность нового видео</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> Split(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return await Conversion.SplitAsync(inputPath, outputPath, startTime, duration, cancellationToken);
        }

        /// <summary>
        /// Сохраняет поток M3U8
        /// </summary>
        /// <param name="uri">Uri потока</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="duration">Длительность потока</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> SaveM3U8Stream(Uri uri, string outputPath, TimeSpan? duration = null, CancellationToken cancellationToken = default)
        {
            return await Conversion.SaveM3U8StreamAsync(uri, outputPath, duration, cancellationToken);
        }

        /// <summary>
        ///     Сохраняет только аудиодорожку потока в файл (поддерживается HLS/RTSP/HTTP).
        /// </summary>
        /// <param name="inputPath">URI или путь к источнику.</param>
        /// <param name="outputPath">Путь к выходному файлу.</param>
        /// <param name="outputFormat">Опциональный формат выходного файла.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        public async Task<IConversion> SaveAudioStream(string inputPath, string outputPath, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            return await Conversion.SaveAudioStreamAsync(inputPath, outputPath, outputFormat, cancellationToken);
        }

        /// <summary>
        ///     Сохраняет аудиопоток по URI в файл.
        /// </summary>
        public Task<IConversion> SaveAudioStream(Uri inputUri, string outputPath, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            return SaveAudioStream(inputUri?.OriginalString ?? throw new ArgumentNullException(nameof(inputUri)), outputPath, outputFormat, cancellationToken);
        }

        /// <summary>
        ///     Ремуксит поток с заданного URI без перекодирования.
        /// </summary>
        /// <param name="inputPath">URI или путь к источнику.</param>
        /// <param name="outputPath">Путь к выходному файлу.</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры.</param>
        /// <param name="outputFormat">Желаемый формат вывода.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> RemuxStream(string inputPath, string outputPath, bool keepSubtitles = false, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            return await Conversion.RemuxStreamAsync(inputPath, outputPath, outputFormat, keepSubtitles, cancellationToken);
        }

        /// <summary>
        ///     Ремуксит поток по Uri без перекодирования.
        /// </summary>
        /// <param name="inputUri">Источник потока.</param>
        /// <param name="outputPath">Путь к выходному файлу.</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры.</param>
        /// <param name="outputFormat">Желаемый формат вывода.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат конвертации.</returns>
        public Task<IConversion> RemuxStream(Uri inputUri, string outputPath, bool keepSubtitles = false, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            return RemuxStream(inputUri?.OriginalString ?? throw new ArgumentNullException(nameof(inputUri)), outputPath, keepSubtitles, outputFormat, cancellationToken);
        }

        /// <summary>
        ///     Подносит данные через stdin и ремуксит их на выход.
        /// </summary>
        /// <param name="inputStream">Поток stdin.</param>
        /// <param name="outputPath">Путь к выходному файлу.</param>
        /// <param name="outputFormat">Желаемый формат вывода.</param>
        /// <returns>Результат конвертации.</returns>
        public IConversion StreamFromStdin(Stream inputStream, string outputPath, Format? outputFormat = null)
        {
            return Conversion.StreamFromStdin(inputStream, outputPath, outputFormat);
        }

        /// <summary>
        ///     Сохраняет только аудиодорожку из stdin за счёт подстановки map 0:a.
        /// </summary>
        public IConversion StreamAudioFromStdin(Stream inputStream, string outputPath, Format? outputFormat = null)
        {
            return Conversion.StreamAudioFromStdin(inputStream, outputPath, outputFormat);
        }

        /// <summary>
        ///     Объединяет несколько входных видео.
        /// </summary>
        /// <param name="output">Объединенные входные видео</param>
        /// <param name="inputVideos">Видео для добавления</param>
        /// <returns>Результат конвертации</returns>
        public Task<IConversion> Concatenate(string output, params string[] inputVideos)
        {
            return Concatenate(output, default, inputVideos);
        }

        public async Task<IConversion> Concatenate(string output, CancellationToken cancellationToken, params string[] inputVideos)
        {
            return await Conversion.Concatenate(output, cancellationToken, inputVideos);
        }

        /// <summary>
        ///     Конвертирует один файл в другой с целевым форматом.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="outputFilePath">Путь к файлу</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры в выходном видео</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> Convert(string inputFilePath, string outputFilePath, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return await Conversion.ConvertAsync(inputFilePath, outputFilePath, keepSubtitles, cancellationToken);
        }

        /// <summary>
        ///     Транскодирует один файл в другой с целевым форматом и кодеками.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="outputFilePath">Путь к файлу</param>
        /// <param name="audioCodec">Аудиокодек для транскодирования входа</param>
        /// <param name="videoCodec">Видеокодек для транскодирования входа</param>
        /// <param name="videoCodec">Кодек субтитров для транскодирования входа</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры в выходном видео</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> Transcode(string inputFilePath, string outputFilePath, VideoCodec videoCodec, AudioCodec audioCodec, SubtitleCodec subtitleCodec, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return await Conversion.TranscodeAsync(inputFilePath, outputFilePath, videoCodec, audioCodec, subtitleCodec, keepSubtitles, cancellationToken);
        }

        /// <summary>
        ///     Транскодирует файл с кодеками по умолчанию (<see cref="FFmpeg.DefaultTranscodeVideoCodec"/>, <see cref="FFmpeg.DefaultTranscodeAudioCodec"/>, mov_text для субтитров).
        /// </summary>
        /// <param name="inputFilePath">Путь к входному файлу.</param>
        /// <param name="outputFilePath">Путь к выходному файлу.</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры.</param>
        /// <returns>Объект IConversion.</returns>
        public async Task<IConversion> Transcode(string inputFilePath, string outputFilePath, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return await Conversion.TranscodeAsync(inputFilePath, outputFilePath, FFmpeg.DefaultTranscodeVideoCodec, FFmpeg.DefaultTranscodeAudioCodec, SubtitleCodec.mov_text, keepSubtitles, cancellationToken);
        }

        /// <summary>
        /// Генерирует визуализацию аудиопотока с использованием фильтра 'showfreqs'
        /// </summary>
        /// <param name="inputPath">Путь к входному файлу, содержащему аудиопоток для визуализации</param>
        /// <param name="outputPath">Путь для вывода визуализированного аудиопотока</param>
        /// <param name="size">Размер выходного видеопотока</param>
        /// <param name="pixelFormat">Формат пикселей выхода (по умолчанию yuv420p)</param>
        /// <param name="mode">Режим визуализации (по умолчанию bar)</param>
        /// <param name="amplitudeScale">Шкала частоты (по умолчанию lin)</param>
        /// <param name="frequencyScale">Шкала амплитуды (по умолчанию log)</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> VisualiseAudio(string inputPath, string outputPath, VideoSize size,
            PixelFormat pixelFormat = PixelFormat.yuv420p,
            VisualisationMode mode = VisualisationMode.bar,
            AmplitudeScale amplitudeScale = AmplitudeScale.lin,
            FrequencyScale frequencyScale = FrequencyScale.log,
            CancellationToken cancellationToken = default)
        {
            return await Conversion.VisualiseAudio(inputPath, outputPath, size, pixelFormat, mode, amplitudeScale, frequencyScale, cancellationToken);
        }

        /// <summary>
        ///     Зацикливает файл бесконечно в потоки RTSP с некоторыми параметрами по умолчанию, такими как: -re, -preset ultrafast
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="rtspServerUri">Uri RTSP сервера в формате: rtsp://127.0.0.1:8554/name</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> SendToRtspServer(string inputFilePath, Uri rtspServerUri, CancellationToken cancellationToken = default)
        {
            return await Conversion.SendToRtspServer(inputFilePath, rtspServerUri, cancellationToken);
        }

        /// <summary>
        ///     Отправляет рабочий стол бесконечно в потоки RTSP с некоторыми параметрами по умолчанию, такими как: -re, -preset ultrafast
        /// </summary>
        /// <param name="rtspServerUri">Uri RTSP сервера в формате: rtsp://127.0.0.1:8554/name</param>
        /// <returns>Объект IConversion</returns>
        public Task<IConversion> SendDesktopToRtspServer(Uri rtspServerUri, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Conversion.SendDesktopToRtspServer(rtspServerUri, cancellationToken));
        }
    }
}
