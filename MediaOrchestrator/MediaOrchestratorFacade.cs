using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics;
using MediaOrchestrator.Analytics.Models;
using MediaOrchestrator.Analytics.Reports;
using MediaOrchestrator.Analytics.Stores;
using MediaOrchestrator.Streams.SubtitleStream;

namespace MediaOrchestrator
{
    /// <summary> 
    ///     Обертка для MediaOrchestrator
    /// </summary>
    public abstract partial class MediaOrchestrator
    {
        /// <summary>
        ///     Директория, содержащая MediaOrchestrator и FFprobe
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
        ///     Метод фильтрации для поиска файлов MediaOrchestrator и FFprobe
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

        private static readonly object _mediaAnalysisStoreSync = new object();
        private static string _mediaAnalysisStoreDirectory = Path.Combine(Path.GetTempPath(), "media-orchestrator-media-analysis");
        private static IMediaAnalysisStore _mediaAnalysisStore = CreateMediaAnalysisStore(_mediaAnalysisStoreDirectory);

        /// <summary>
        ///     Включает накопление статистики по решениям analytics и адаптацию будущих решений на её основе.
        /// </summary>
        public static bool MediaAnalysisLearningEnabled { get; set; } = true;

        /// <summary>
        ///     Включает сжатие JSON файлов статистики (GZIP). Уменьшает размер хранилища, увеличивает CPU usage.
        /// </summary>
        public static bool MediaAnalysisStoreCompressionEnabled { get; set; } = false;

        private static CircuitBreaker _ffmpegCircuitBreaker = new CircuitBreaker();

        /// <summary>
        ///     Проверяет доступность ffmpeg операций через Circuit Breaker.
        /// </summary>
        public static bool IsFfmpegOperationAllowed => _ffmpegCircuitBreaker.IsAllowed;

        internal static IMediaAnalysisStore MediaAnalysisStore
        {
            get
            {
                lock (_mediaAnalysisStoreSync)
                {
                    return _mediaAnalysisStore;
                }
            }
        }

        public static string MediaAnalysisStoreDirectory
        {
            get
            {
                lock (_mediaAnalysisStoreSync)
                {
                    return _mediaAnalysisStoreDirectory;
                }
            }
        }

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
        ///     Получает MediaInfo из файла, потока или массива байт.
        /// </summary>
        public static async Task<IMediaInfo> GetMediaInfo(
            MediaSource source,
            CancellationToken cancellationToken = default,
            bool waitUntilFileStable = false,
            TimeSpan? stabilityQuietPeriod = null,
            TimeSpan? maximumWaitForStable = null)
        {
            var prepared = await MediaIoBridge.PrepareInputAsync(source, cancellationToken).ConfigureAwait(false);
            try
            {
                return await GetMediaInfo(prepared.Path, cancellationToken, waitUntilFileStable, stabilityQuietPeriod, maximumWaitForStable).ConfigureAwait(false);
            }
            finally
            {
                await prepared.CleanupAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Устанавливает путь к директории, содержащей MediaOrchestrator и FFprobe
        /// </summary>
        /// <param name="directoryWithFFmpegAndFFprobe"></param>
        /// <param name="ffmpegExeutableName">Имя исполняемого файла MediaOrchestrator</param>
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
        ///     Задаёт глобальные лимиты параметров выхода (без смены пути к MediaOrchestrator). Null сбрасывает соответствующий лимит.
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
        ///     Настраивает каталог для персистентного хранения аналитики и статистики media-processing.
        /// </summary>
        public static void SetMediaAnalysisStoreDirectory(string directory = null)
        {
            lock (_mediaAnalysisStoreSync)
            {
                _mediaAnalysisStoreDirectory = string.IsNullOrWhiteSpace(directory)
                    ? Path.Combine(Path.GetTempPath(), "media-orchestrator-media-analysis")
                    : new DirectoryInfo(directory).FullName;
                _mediaAnalysisStore = CreateMediaAnalysisStore(_mediaAnalysisStoreDirectory);
            }
        }

        /// <summary>
        ///     Очищает сохранённую статистику media analytics.
        /// </summary>
        public static void ClearMediaAnalysisStore()
        {
            MediaAnalysisStore.ClearAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Принудительно сбрасывает накопленную в памяти analytics-статистику в persistent store.
        ///     Полезно вызывать перед graceful shutdown сервиса.
        /// </summary>
        public static void FlushMediaAnalysisStore(CancellationToken cancellationToken = default)
        {
            FlushMediaAnalysisStoreAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Асинхронно сбрасывает накопленную в памяти analytics-статистику в persistent store.
        /// </summary>
        public static Task FlushMediaAnalysisStoreAsync(CancellationToken cancellationToken = default)
        {
            var cachedStore = MediaAnalysisStore as CachedMediaAnalysisStore;
            if (cachedStore == null)
            {
                return Task.CompletedTask;
            }

            return cachedStore.FlushPendingAsync(cancellationToken);
        }

        /// <summary>
        ///     Возвращает агрегированный отчёт по media analytics для веб-интерфейсов, мониторинга и Grafana.
        /// </summary>
        public static MediaAnalyticsReport GetMediaAnalyticsReport(MediaAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            return GetMediaAnalyticsReportAsync(query, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Асинхронно возвращает агрегированный отчёт по media analytics для веб-интерфейсов, мониторинга и Grafana.
        /// </summary>
        public static async Task<MediaAnalyticsReport> GetMediaAnalyticsReportAsync(MediaAnalyticsQuery query = null, CancellationToken cancellationToken = default)
        {
            var records = await MediaAnalysisStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return MediaAnalyticsReportBuilder.Build(records, query);
        }

        private static readonly object _operationDurationCacheSync = new object();
        private static OperationDurationLruCache _operationDurationCache;

        private static OperationDurationLruCache OperationDurationCache
        {
            get
            {
                lock (_operationDurationCacheSync)
                {
                    if (_operationDurationCache == null)
                        _operationDurationCache = new OperationDurationLruCache(capacity: 500, safetyFactor: 2.0);
                    return _operationDurationCache;
                }
            }
        }

        /// <summary>
        ///     Получает адаптивный timeout для операции на основе исторических данных.
        ///     Если исторических данных нет - возвращает defaultTimeout.
        /// </summary>
        /// <param name="operationKey">Ключ операции ( buildOperationKey )</param>
        /// <param name="defaultTimeout">Timeout по умолчанию, если нет исторических данных</param>
        /// <returns>Адаптивный или default timeout</returns>
        public static TimeSpan GetAdaptiveTimeout(string operationKey, TimeSpan defaultTimeout)
        {
            return OperationDurationCache.GetEstimatedTimeout(operationKey, defaultTimeout);
        }

        /// <summary>
        ///     Создаёт CancellationToken с адаптивным timeout на основе исторических данных.
        /// </summary>
        /// <param name="operationKey">Ключ операции</param>
        /// <param name="defaultTimeout">Timeout по умолчанию</param>
        /// <param name="linkedToken">Токен для связывания (опционально)</param>
        /// <returns> CancellationTokenSource с адаптивным timeout</returns>
        public static CancellationTokenSource CreateAdaptiveCancellationTokenSource(
            string operationKey,
            TimeSpan defaultTimeout,
            CancellationToken linkedToken = default)
        {
            var adaptiveTimeout = OperationDurationCache.GetEstimatedTimeout(operationKey, defaultTimeout);
            var timeoutCts = new CancellationTokenSource(adaptiveTimeout);

            if (linkedToken.CanBeCanceled)
            {
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken, timeoutCts.Token);
                timeoutCts.Dispose();
                return linkedCts;
            }

            return timeoutCts;
        }

        /// <summary>
        ///     Записывает фактическую продолжительность операции для обновления адаптивного timeout.
        ///     Вызывается после завершения операции.
        /// </summary>
        /// <param name="operationKey">Ключ операции</param>
        /// <param name="actualDuration">Фактическая продолжительность</param>
        /// <param name="succeeded">true если операция завершилась успешно</param>
        public static void RecordOperationDuration(string operationKey, TimeSpan actualDuration, bool succeeded)
        {
            OperationDurationCache.RecordDuration(operationKey, actualDuration, succeeded);
        }

        /// <summary>
        ///     Очищает кэш адаптивных timeout.
        /// </summary>
        public static void ClearAdaptiveTimeoutCache()
        {
            OperationDurationCache.Clear();
        }

        /// <summary>
        ///     Возвращает количество записей в кэше продолжительности операций.
        /// </summary>
        public static int GetOperationDurationCacheCount()
        {
            return OperationDurationCache.Count;
        }

        /// <summary>
        ///     Создаёт ключ операции для LRU кэша продолжительности.
        /// </summary>
        public static string BuildOperationKey(
            string inputPath,
            string outputPath,
            ProcessingScenario scenario,
            MediaProcessingStrategy strategy,
            string videoCodec = null,
            string audioCodec = null,
            double? videoDurationSeconds = null,
            bool usesHardwareAcceleration = false)
        {
            return OperationDurationLruCache.BuildOperationKey(
                inputPath,
                outputPath,
                scenario,
                strategy,
                videoCodec,
                audioCodec,
                videoDurationSeconds,
                usesHardwareAcceleration);
        }

        private static IMediaAnalysisStore CreateMediaAnalysisStore(string directoryPath)
        {
            var store = new FileMediaAnalysisStore(directoryPath, MediaAnalysisStoreCompressionEnabled);
            return new CachedMediaAnalysisStore(store, null, 1000, TimeSpan.FromHours(1));
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

        private static IConversion AttachManagedMediaIo(
            IConversion conversion,
            Func<CancellationToken, Task> onSuccessAsync,
            Func<Task> onFinallyAsync)
        {
            var concrete = conversion as Conversion;
            if (concrete != null)
            {
                concrete.AttachLifecycleHandlers(onSuccessAsync, onFinallyAsync);
            }

            return conversion;
        }

        private sealed class SharedCleanupScope
        {
            private readonly Func<Task> _cleanupAsync;
            private int _remaining;

            internal SharedCleanupScope(int remaining, Func<Task> cleanupAsync)
            {
                _remaining = remaining;
                _cleanupAsync = cleanupAsync ?? (() => Task.CompletedTask);
            }

            internal Task ReleaseAsync()
            {
                if (Interlocked.Decrement(ref _remaining) == 0)
                {
                    return _cleanupAsync();
                }

                return Task.CompletedTask;
            }
        }

        private static async Task<IConversion> PrepareSingleInputOutputAsync(
            MediaSource input,
            MediaDestination output,
            Func<string, string, CancellationToken, Task<IConversion>> factory,
            CancellationToken cancellationToken)
        {
            var preparedInput = await MediaIoBridge.PrepareInputAsync(input, cancellationToken).ConfigureAwait(false);
            var preparedOutput = MediaIoBridge.PrepareOutput(output);
            try
            {
                var conversion = await factory(preparedInput.Path, preparedOutput.Path, cancellationToken).ConfigureAwait(false);
                return AttachManagedMediaIo(
                    conversion,
                    preparedOutput.FinalizeAsync,
                    async () =>
                    {
                        await preparedInput.CleanupAsync().ConfigureAwait(false);
                        await preparedOutput.CleanupAsync().ConfigureAwait(false);
                    });
            }
            catch
            {
                await preparedInput.CleanupAsync().ConfigureAwait(false);
                await preparedOutput.CleanupAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static async Task<IConversion> PrepareDoubleInputOutputAsync(
            MediaSource input1,
            MediaSource input2,
            MediaDestination output,
            Func<string, string, string, CancellationToken, Task<IConversion>> factory,
            CancellationToken cancellationToken)
        {
            var preparedInput1 = await MediaIoBridge.PrepareInputAsync(input1, cancellationToken).ConfigureAwait(false);
            var preparedInput2 = await MediaIoBridge.PrepareInputAsync(input2, cancellationToken).ConfigureAwait(false);
            var preparedOutput = MediaIoBridge.PrepareOutput(output);
            try
            {
                var conversion = await factory(preparedInput1.Path, preparedInput2.Path, preparedOutput.Path, cancellationToken).ConfigureAwait(false);
                return AttachManagedMediaIo(
                    conversion,
                    preparedOutput.FinalizeAsync,
                    async () =>
                    {
                        await preparedInput1.CleanupAsync().ConfigureAwait(false);
                        await preparedInput2.CleanupAsync().ConfigureAwait(false);
                        await preparedOutput.CleanupAsync().ConfigureAwait(false);
                    });
            }
            catch
            {
                await preparedInput1.CleanupAsync().ConfigureAwait(false);
                await preparedInput2.CleanupAsync().ConfigureAwait(false);
                await preparedOutput.CleanupAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static async Task<IConversion> PrepareManyInputsOutputAsync(
            IEnumerable<MediaSource> inputs,
            MediaDestination output,
            Func<string, CancellationToken, string[], Task<IConversion>> factory,
            CancellationToken cancellationToken)
        {
            var preparedInputs = new List<MediaIoBridge.PreparedMediaInput>();
            var preparedOutput = MediaIoBridge.PrepareOutput(output);
            try
            {
                foreach (var input in inputs)
                {
                    preparedInputs.Add(await MediaIoBridge.PrepareInputAsync(input, cancellationToken).ConfigureAwait(false));
                }

                var conversion = await factory(preparedOutput.Path, cancellationToken, preparedInputs.Select(x => x.Path).ToArray()).ConfigureAwait(false);
                return AttachManagedMediaIo(
                    conversion,
                    preparedOutput.FinalizeAsync,
                    async () =>
                    {
                        foreach (var preparedInput in preparedInputs)
                        {
                            await preparedInput.CleanupAsync().ConfigureAwait(false);
                        }

                        await preparedOutput.CleanupAsync().ConfigureAwait(false);
                    });
            }
            catch
            {
                foreach (var preparedInput in preparedInputs)
                {
                    await preparedInput.CleanupAsync().ConfigureAwait(false);
                }

                await preparedOutput.CleanupAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static async Task<IReadOnlyList<IConversion>> PrepareSplitAudioOutputsAsync(
            MediaSource input,
            MediaDirectoryDestination outputDirectory,
            IEnumerable<TimeSpan> timecodes,
            AudioCodec audioCodec,
            long bitrate,
            int sampleRate,
            CancellationToken cancellationToken)
        {
            var preparedInput = await MediaIoBridge.PrepareInputAsync(input, cancellationToken).ConfigureAwait(false);
            var preparedDirectory = MediaIoBridge.PrepareDirectoryOutput(outputDirectory);
            try
            {
                var conversions = await Conversion.SplitAudioByTimecodesAsync(
                    preparedInput.Path,
                    preparedDirectory.Path,
                    timecodes,
                    audioCodec,
                    bitrate,
                    sampleRate,
                    cancellationToken).ConfigureAwait(false);

                var cleanupScope = new SharedCleanupScope(
                    conversions.Count,
                    async () =>
                    {
                        await preparedInput.CleanupAsync().ConfigureAwait(false);
                        await preparedDirectory.CleanupAsync().ConfigureAwait(false);
                    });

                foreach (var conversion in conversions)
                {
                    var fileName = Path.GetFileName(conversion.OutputFilePath);
                    AttachManagedMediaIo(
                        conversion,
                        async ct =>
                        {
                            if (outputDirectory.Kind == MediaDirectoryDestination.DirectoryDestinationKind.Memory)
                            {
                                outputDirectory.SetFile(fileName, await MediaIoBridge.ReadFileBytesAsync(conversion.OutputFilePath, ct).ConfigureAwait(false));
                            }
                        },
                        cleanupScope.ReleaseAsync);
                }

                return conversions;
            }
            catch
            {
                await preparedInput.CleanupAsync().ConfigureAwait(false);
                await preparedDirectory.CleanupAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static async Task<IConversion> PrepareDirectoryOutputAsync(
            Func<string, CancellationToken, Task<IConversion>> factory,
            MediaDirectoryDestination outputDirectory,
            string outputFileName,
            CancellationToken cancellationToken)
        {
            var preparedDirectory = MediaIoBridge.PrepareDirectoryOutput(outputDirectory);
            try
            {
                var outputPath = Path.Combine(preparedDirectory.Path, outputFileName);
                var conversion = await factory(outputPath, cancellationToken).ConfigureAwait(false);
                return AttachManagedMediaIo(
                    conversion,
                    preparedDirectory.FinalizeAsync,
                    preparedDirectory.CleanupAsync);
            }
            catch
            {
                await preparedDirectory.CleanupAsync().ConfigureAwait(false);
                throw;
            }
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

        public Task<IConversion> ExtractAudio(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, ExtractAudio, cancellationToken);
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

        public Task<IConversion> ExtractAudio(
            MediaSource input,
            MediaDestination output,
            AudioCodec audioCodec = AudioCodec.mp3,
            long? bitrate = null,
            int? sampleRate = null,
            CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => ExtractAudio(inputPath, outputPath, audioCodec, bitrate, sampleRate, ct),
                cancellationToken);
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

        public Task<IConversion> ConvertToWav(MediaSource input, MediaDestination output, int sampleRate = 16000, int channels = 1, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => ConvertToWav(inputPath, outputPath, sampleRate, channels, ct),
                cancellationToken);
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

        public Task<IConversion> NormalizeAudioForTranscription(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, NormalizeAudioForTranscription, cancellationToken);
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

        public Task<IConversion> NormalizeAudioForTranscription(
            MediaSource input,
            MediaDestination output,
            TranscriptionAudioSettings settings,
            CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => NormalizeAudioForTranscription(inputPath, outputPath, settings, ct),
                cancellationToken);
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

        public Task<IConversion> AddAudio(MediaSource video, MediaSource audio, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareDoubleInputOutputAsync(video, audio, output, AddAudio, cancellationToken);
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

        public Task<IReadOnlyList<IConversion>> SplitAudioByTimecodes(
            MediaSource input,
            MediaDirectoryDestination outputDirectory,
            IEnumerable<TimeSpan> timecodes,
            AudioCodec audioCodec = AudioCodec.mp3,
            long bitrate = 192000,
            int sampleRate = 44100,
            CancellationToken cancellationToken = default)
        {
            return PrepareSplitAudioOutputsAsync(input, outputDirectory, timecodes, audioCodec, bitrate, sampleRate, cancellationToken);
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

        public Task<IConversion> ToMp4(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, ToMp4, cancellationToken);
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

        public Task<IConversion> ToTs(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, ToTs, cancellationToken);
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

        public Task<IConversion> ToOgv(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, ToOgv, cancellationToken);
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

        public Task<IConversion> ToWebM(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, ToWebM, cancellationToken);
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

        public Task<IConversion> RemuxToWebM(MediaSource input, MediaDestination output, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => RemuxToWebM(inputPath, outputPath, keepSubtitles, ct),
                cancellationToken);
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

        public Task<IConversion> ToGif(MediaSource input, MediaDestination output, int loop, int delay = 0, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => ToGif(inputPath, outputPath, loop, delay, ct),
                cancellationToken);
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

        public Task<IConversion> ConvertWithHardware(MediaSource input, MediaDestination output, HardwareAccelerator hardwareAccelerator, VideoCodec decoder, VideoCodec encoder, int device = 0, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => ConvertWithHardware(inputPath, outputPath, hardwareAccelerator, decoder, encoder, device, ct),
                cancellationToken);
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

        public Task<IConversion> BurnSubtitle(MediaSource input, MediaDestination output, MediaSource subtitles, CancellationToken cancellationToken = default)
        {
            return PrepareDoubleInputOutputAsync(
                input,
                subtitles,
                output,
                (inputPath, subtitlePath, outputPath, ct) => BurnSubtitle(inputPath, outputPath, subtitlePath, ct),
                cancellationToken);
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

        public Task<IConversion> AddSubtitle(MediaSource input, MediaDestination output, MediaSource subtitle, string language = null, CancellationToken cancellationToken = default)
        {
            return PrepareDoubleInputOutputAsync(
                input,
                subtitle,
                output,
                (inputPath, subtitlePath, outputPath, ct) => AddSubtitle(inputPath, outputPath, subtitlePath, language, ct),
                cancellationToken);
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

        public Task<IConversion> AddSubtitle(MediaSource input, MediaDestination output, MediaSource subtitle, SubtitleCodec subtitleCodec, string language = null, CancellationToken cancellationToken = default)
        {
            return PrepareDoubleInputOutputAsync(
                input,
                subtitle,
                output,
                (inputPath, subtitlePath, outputPath, ct) => AddSubtitle(inputPath, outputPath, subtitlePath, subtitleCodec, language, ct),
                cancellationToken);
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

        public Task<IConversion> SetWatermark(MediaSource input, MediaDestination output, MediaSource watermarkImage, Position position, CancellationToken cancellationToken = default)
        {
            return PrepareDoubleInputOutputAsync(
                input,
                watermarkImage,
                output,
                (inputPath, imagePath, outputPath, ct) => SetWatermark(inputPath, outputPath, imagePath, position, ct),
                cancellationToken);
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

        public Task<IConversion> BurnRightSideTextLabel(
            MediaSource input,
            MediaDestination output,
            string text,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null,
            CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => BurnRightSideTextLabel(inputPath, outputPath, text, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath, ct),
                cancellationToken);
        }

        /// <summary>
        ///     Вшивает у правого края динамическое время: по умолчанию по PTS (ЧЧ:ММ:СС), опционально локальное время системы.
        ///     Для таймкода с полем «кадр» и заданным fps используйте метод <c>BurnRightSideSmpteTimecode(...)</c>.
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

        public Task<IConversion> BurnRightSidePtsTimeLabel(
            MediaSource input,
            MediaDestination output,
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
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => BurnRightSidePtsTimeLabel(inputPath, outputPath, prefix, suffix, useLocalWallClock, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath, ct),
                cancellationToken);
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

        public Task<IConversion> BurnRightSideSmpteTimecode(
            MediaSource input,
            MediaDestination output,
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
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => BurnRightSideSmpteTimecode(inputPath, outputPath, startTimecode, frameRate, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath, ct),
                cancellationToken);
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

        public Task<IConversion> ExtractVideo(MediaSource input, MediaDestination output, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(input, output, ExtractVideo, cancellationToken);
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

        public Task<IConversion> Snapshot(MediaSource input, MediaDestination output, TimeSpan captureTime, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => Snapshot(inputPath, outputPath, captureTime, ct),
                cancellationToken);
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

        public Task<IConversion> ChangeSize(MediaSource input, MediaDestination output, int width, int height, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => ChangeSize(inputPath, outputPath, width, height, ct),
                cancellationToken);
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

        public Task<IConversion> ChangeSize(MediaSource input, MediaDestination output, VideoSize size, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => ChangeSize(inputPath, outputPath, size, ct),
                cancellationToken);
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

        public Task<IConversion> Split(MediaSource input, MediaDestination output, TimeSpan startTime, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => Split(inputPath, outputPath, startTime, duration, ct),
                cancellationToken);
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

        public Task<IConversion> SaveM3U8Stream(Uri uri, MediaDirectoryDestination outputDirectory, string playlistFileName = "stream.m3u8", TimeSpan? duration = null, CancellationToken cancellationToken = default)
        {
            var fileName = string.IsNullOrWhiteSpace(playlistFileName) ? "stream.m3u8" : playlistFileName;
            return PrepareDirectoryOutputAsync(
                (outputPath, ct) => SaveM3U8Stream(uri, outputPath, duration, ct),
                outputDirectory,
                fileName,
                cancellationToken);
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

        public Task<IConversion> SaveAudioStream(MediaSource input, MediaDestination output, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => SaveAudioStream(inputPath, outputPath, outputFormat, ct),
                cancellationToken);
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

        public Task<IConversion> RemuxStream(MediaSource input, MediaDestination output, bool keepSubtitles = false, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => RemuxStream(inputPath, outputPath, keepSubtitles, outputFormat, ct),
                cancellationToken);
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

        public IConversion StreamFromStdin(Stream inputStream, MediaDestination output, Format? outputFormat = null)
        {
            var preparedOutput = MediaIoBridge.PrepareOutput(output);
            var conversion = Conversion.StreamFromStdin(inputStream, preparedOutput.Path, outputFormat);
            return AttachManagedMediaIo(conversion, preparedOutput.FinalizeAsync, preparedOutput.CleanupAsync);
        }

        /// <summary>
        ///     Сохраняет только аудиодорожку из stdin за счёт подстановки map 0:a.
        /// </summary>
        public IConversion StreamAudioFromStdin(Stream inputStream, string outputPath, Format? outputFormat = null)
        {
            return Conversion.StreamAudioFromStdin(inputStream, outputPath, outputFormat);
        }

        public IConversion StreamAudioFromStdin(Stream inputStream, MediaDestination output, Format? outputFormat = null)
        {
            var preparedOutput = MediaIoBridge.PrepareOutput(output);
            var conversion = Conversion.StreamAudioFromStdin(inputStream, preparedOutput.Path, outputFormat);
            return AttachManagedMediaIo(conversion, preparedOutput.FinalizeAsync, preparedOutput.CleanupAsync);
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

        public Task<IConversion> Concatenate(MediaDestination output, params MediaSource[] inputVideos)
        {
            return Concatenate(output, default, inputVideos);
        }

        public async Task<IConversion> Concatenate(string output, CancellationToken cancellationToken, params string[] inputVideos)
        {
            return await Conversion.Concatenate(output, cancellationToken, inputVideos);
        }

        public Task<IConversion> Concatenate(MediaDestination output, CancellationToken cancellationToken, params MediaSource[] inputVideos)
        {
            return PrepareManyInputsOutputAsync(inputVideos, output, Conversion.Concatenate, cancellationToken);
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

        public Task<IConversion> Convert(MediaSource input, MediaDestination output, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => Convert(inputPath, outputPath, keepSubtitles, ct),
                cancellationToken);
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

        public Task<IConversion> Transcode(MediaSource input, MediaDestination output, VideoCodec videoCodec, AudioCodec audioCodec, SubtitleCodec subtitleCodec, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => Transcode(inputPath, outputPath, videoCodec, audioCodec, subtitleCodec, keepSubtitles, ct),
                cancellationToken);
        }

        /// <summary>
        ///     Транскодирует файл с кодеками по умолчанию (<see cref="MediaOrchestrator.DefaultTranscodeVideoCodec"/>, <see cref="MediaOrchestrator.DefaultTranscodeAudioCodec"/>, mov_text для субтитров).
        /// </summary>
        /// <param name="inputFilePath">Путь к входному файлу.</param>
        /// <param name="outputFilePath">Путь к выходному файлу.</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры.</param>
        /// <returns>Объект IConversion.</returns>
        public async Task<IConversion> Transcode(string inputFilePath, string outputFilePath, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return await Conversion.TranscodeAsync(inputFilePath, outputFilePath, MediaOrchestrator.DefaultTranscodeVideoCodec, MediaOrchestrator.DefaultTranscodeAudioCodec, SubtitleCodec.mov_text, keepSubtitles, cancellationToken);
        }

        public Task<IConversion> Transcode(MediaSource input, MediaDestination output, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => Transcode(inputPath, outputPath, keepSubtitles, ct),
                cancellationToken);
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

        public Task<IConversion> VisualiseAudio(MediaSource input, MediaDestination output, VideoSize size,
            PixelFormat pixelFormat = PixelFormat.yuv420p,
            VisualisationMode mode = VisualisationMode.bar,
            AmplitudeScale amplitudeScale = AmplitudeScale.lin,
            FrequencyScale frequencyScale = FrequencyScale.log,
            CancellationToken cancellationToken = default)
        {
            return PrepareSingleInputOutputAsync(
                input,
                output,
                (inputPath, outputPath, ct) => VisualiseAudio(inputPath, outputPath, size, pixelFormat, mode, amplitudeScale, frequencyScale, ct),
                cancellationToken);
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

        public async Task<IConversion> SendToRtspServer(MediaSource input, Uri rtspServerUri, CancellationToken cancellationToken = default)
        {
            var preparedInput = await MediaIoBridge.PrepareInputAsync(input, cancellationToken).ConfigureAwait(false);
            try
            {
                var conversion = await SendToRtspServer(preparedInput.Path, rtspServerUri, cancellationToken).ConfigureAwait(false);
                return AttachManagedMediaIo(conversion, _ => Task.CompletedTask, preparedInput.CleanupAsync);
            }
            catch
            {
                await preparedInput.CleanupAsync().ConfigureAwait(false);
                throw;
            }
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
