using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    public enum FileNameFilterMethod
    {
        Contains,
        Exact,
        StartWith
    }

    /// <summary> 
    ///     Обертка для MediaOrchestrator
    /// </summary>
    public abstract partial class MediaOrchestrator
    {
        private static string _ffmpegPath;
        private static string _ffprobePath;

        /// <summary>
        /// null: резолв ещё не выполнялся; пустая строка: успешный авто-поиск без <see cref="ExecutablesPath"/>; иначе последний закэшированный каталог из <see cref="SetExecutablesPath"/>.
        /// </summary>
        private static string _lastExecutablePathMarker;
        private static string _lastHardwareAccelerationDetectionMarker;

        /// <summary>
        ///     Синхронизация путей к бинарникам, каталога ExecutablesPath, фильтра имён и профиля HW-ускорения.
        ///     Чтение путей — короткий read lock; полный поиск и смена конфигурации — write lock. Определение -hwaccels не держит write lock.
        /// </summary>
        private static readonly ReaderWriterLockSlim _executableConfigurationLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly object _hardwareAccelerationDetectionGate = new object();

        private static string _executablesPath;
        private static FileNameFilterMethod _filterMethod;
        private static IFormatProvider _formatProvider = CultureInfo.CurrentCulture;

        private static string _ffmpegExecutableName = "ffmpeg";
        private static string _ffprobeExecutableName = "ffprobe";

        private static HardwareAccelerationProfile _autoDetectedHardwareAccelerationProfile;
        internal static Func<string, CancellationToken, HardwareAccelerationProfile> HardwareAccelerationProfileDetector { get; set; } = DetectAutoHardwareAccelerationProfile;
        public static Func<string, int, double?> HardwareAcceleratorLoadProvider { get; set; }

        /// <summary>
        ///     Верхняя граница частоты кадров выходного видео. Не применяется к извлечению аудио, чисто аудио конвертации,
        ///     разбиению аудио по таймкодам и быстрому экспорту WAV.
        /// </summary>
        public static double? MaxOutputVideoFrameRate { get; set; }

        /// <summary>
        ///     Верхняя граница частоты дискретизации выходного аудио (Гц). Не применяется к сценариям без видео на выходе
        ///     и к извлечению/экспорту аудио (см. <see cref="MaxOutputVideoFrameRate"/>).
        /// </summary>
        public static int? MaxOutputAudioSampleRate { get; set; }

        /// <summary>
        ///     Верхняя граница числа каналов выходного аудио. Ограничения те же, что у <see cref="MaxOutputAudioSampleRate"/>.
        /// </summary>
        public static int? MaxOutputAudioChannels { get; set; }

        /// <summary>
        ///     Обнаруженный при инициализации профиль аппаратного ускорения (null, если автоопределение выключено или не удалось).
        /// </summary>
        internal static HardwareAccelerationProfile AutoDetectedHardwareAccelerationProfile
        {
            get
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    return _autoDetectedHardwareAccelerationProfile;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     True, если профиль аппаратного ускорения успешно определён.
        /// </summary>
        public static bool IsHardwareAccelerationProfileDetected => AutoDetectedHardwareAccelerationProfile != null;

        /// <summary>
        ///     Имя выбранного аппаратного ускорителя (например cuda, qsv, videotoolbox) или null.
        /// </summary>
        public static string DetectedHardwareAcceleratorName => AutoDetectedHardwareAccelerationProfile?.Hwaccel;

        /// <summary>
        ///     Включить автоматическое добавление -hwaccel / декодера для перекодирования видео (не используется при stream copy).
        /// </summary>
        public static bool ApplyAutoHardwareAccelerationToConversions { get; set; } = true;

        /// <summary>
        ///     Целевой видеокодек по умолчанию для транскодирования (например в перегрузках Transcode без явного кодека).
        /// </summary>
        public static VideoCodec DefaultTranscodeVideoCodec { get; set; } = VideoCodec.h264;

        /// <summary>
        ///     Целевой аудиокодек по умолчанию для транскодирования.
        /// </summary>
        public static AudioCodec DefaultTranscodeAudioCodec { get; set; } = AudioCodec.aac;

        /// <summary>
        ///     Инициализирует новый MediaOrchestrator. Ищет MediaOrchestrator и FFprobe в PATH
        /// </summary>
        /// 
        protected MediaOrchestrator()
        {
            EnsureExecutablePathsResolved(CancellationToken.None);
        }

        /// <summary>
        ///     Находит ffmpeg и ffprobe, если каталог не задан через <see cref="SetExecutablesPath"/>
        ///     (переменные окружения, PATH, каталоги рядом с приложением). Безопасно вызывать повторно.
        /// </summary>
        /// <param name="cancellationToken">Проверяется до начала резолва.</param>
        public static void EnsureExecutablesLocated(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureExecutablePathsResolved(cancellationToken);
        }

        private static void EnsureExecutablePathsResolved(CancellationToken cancellationToken)
        {
            _executableConfigurationLock.EnterReadLock();
            try
            {
                if (IsExecutableResolutionCacheValid())
                {
                    return;
                }
            }
            finally
            {
                _executableConfigurationLock.ExitReadLock();
            }

            _executableConfigurationLock.EnterWriteLock();
            try
            {
                if (IsExecutableResolutionCacheValid())
                {
                    return;
                }

                RunFindAndValidateExecutablesResolution();
            }
            finally
            {
                _executableConfigurationLock.ExitWriteLock();
            }

            EnsureHardwareAccelerationProfileResolved(cancellationToken);
        }

        private static bool IsExecutableResolutionCacheValid()
        {
            if (string.IsNullOrWhiteSpace(_ffprobePath) || string.IsNullOrWhiteSpace(_ffmpegPath))
            {
                return false;
            }

            if (_lastExecutablePathMarker == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_executablesPath))
            {
                return string.Equals(_lastExecutablePathMarker, _executablesPath, StringComparison.Ordinal);
            }

            return _lastExecutablePathMarker.Length == 0;
        }

        private static string GetCurrentHardwareAccelerationDetectionMarker()
        {
            if (string.IsNullOrWhiteSpace(_ffmpegPath) || _lastExecutablePathMarker == null)
            {
                return null;
            }

            return _lastExecutablePathMarker + "|" + _ffmpegPath;
        }

        private static bool IsHardwareAccelerationProfileCacheValid()
        {
            var currentMarker = GetCurrentHardwareAccelerationDetectionMarker();
            return !string.IsNullOrWhiteSpace(currentMarker) &&
                   string.Equals(_lastHardwareAccelerationDetectionMarker, currentMarker, StringComparison.Ordinal);
        }

        private static void EnsureHardwareAccelerationProfileResolved(CancellationToken cancellationToken)
        {
            string detectionMarker;
            string ffmpegExecutablePath;

            _executableConfigurationLock.EnterReadLock();
            try
            {
                if (IsHardwareAccelerationProfileCacheValid())
                {
                    return;
                }

                detectionMarker = GetCurrentHardwareAccelerationDetectionMarker();
                ffmpegExecutablePath = _ffmpegPath;
            }
            finally
            {
                _executableConfigurationLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(detectionMarker) || string.IsNullOrWhiteSpace(ffmpegExecutablePath))
            {
                return;
            }

            lock (_hardwareAccelerationDetectionGate)
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    if (IsHardwareAccelerationProfileCacheValid())
                    {
                        return;
                    }

                    detectionMarker = GetCurrentHardwareAccelerationDetectionMarker();
                    ffmpegExecutablePath = _ffmpegPath;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }

                if (string.IsNullOrWhiteSpace(detectionMarker) || string.IsNullOrWhiteSpace(ffmpegExecutablePath))
                {
                    return;
                }

                var profile = HardwareAccelerationProfileDetector(ffmpegExecutablePath, cancellationToken);

                _executableConfigurationLock.EnterWriteLock();
                try
                {
                    if (string.Equals(GetCurrentHardwareAccelerationDetectionMarker(), detectionMarker, StringComparison.Ordinal))
                    {
                        _autoDetectedHardwareAccelerationProfile = profile;
                        _lastHardwareAccelerationDetectionMarker = detectionMarker;
                    }
                }
                finally
                {
                    _executableConfigurationLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Вызывать только при удерживаемом write lock после успешного определения путей.
        /// </summary>
        private static void MarkExecutableResolutionCacheComplete()
        {
            _lastExecutablePathMarker = string.IsNullOrEmpty(_executablesPath) ? string.Empty : _executablesPath;
        }

        /// <summary>
        ///     Вызывать только при удерживаемом write lock <see cref="_executableConfigurationLock"/>.
        /// </summary>
        private static void RunFindAndValidateExecutablesResolution()
        {
            if (_formatProvider == null)
            {
                _formatProvider = CultureInfo.CurrentCulture;
            }

            if (!string.IsNullOrWhiteSpace(_executablesPath))
            {
                if (!Directory.Exists(_executablesPath))
                {
                    ValidateResolvedExecutablesPresentOrThrow();
                    return;
                }
                FileInfo[] files;
                try
                {
                    files = new DirectoryInfo(_executablesPath).GetFiles();
                }
                catch (UnauthorizedAccessException)
                {
                    throw new ExecutablesPathAccessDeniedException(string.Format(ErrorMessages.ExecutablesPathAccessDenied, _executablesPath));
                }
                catch (IOException)
                {
                    throw new ExecutablesPathAccessDeniedException(string.Format(ErrorMessages.ExecutablesPathAccessDenied, _executablesPath));
                }
                Func<string, string, IFormatProvider, bool> compareMethod;
                switch (_filterMethod)
                {
                    case FileNameFilterMethod.Contains:
                        compareMethod = (path, exec, provider) => path.ToString(provider).Contains(exec);
                        break;
                    case FileNameFilterMethod.Exact:
                        compareMethod = (path, exec, provider) => path.ToString(provider).Equals(exec);
                        break;
                    case FileNameFilterMethod.StartWith:
                        compareMethod = (path, exec, provider) => path.ToString(provider).StartsWith(exec);
                        break;
                    default:
                        compareMethod = (path, exec, provider) => path.ToString(provider).Contains(exec);
                        break;
                }

                SetResolvedFfprobePathDirect(files.FirstOrDefault(x => compareMethod(x.Name, _ffprobeExecutableName, _formatProvider) && IsExecutable(x.FullName))?.FullName);
                SetResolvedFfmpegPathDirect(files.FirstOrDefault(x => compareMethod(x.Name, _ffmpegExecutableName, _formatProvider) && IsExecutable(x.FullName))?.FullName);

                var declaredFfprobe = files.FirstOrDefault(x => compareMethod(x.Name, _ffprobeExecutableName, _formatProvider))?.FullName;
                var declaredFfmpeg = files.FirstOrDefault(x => compareMethod(x.Name, _ffmpegExecutableName, _formatProvider))?.FullName;
                if (!string.IsNullOrWhiteSpace(declaredFfprobe) && string.IsNullOrWhiteSpace(_ffprobePath))
                {
                    ThrowExecutableSignatureMismatch(declaredFfprobe);
                }

                if (!string.IsNullOrWhiteSpace(declaredFfmpeg) && string.IsNullOrWhiteSpace(_ffmpegPath))
                {
                    ThrowExecutableSignatureMismatch(declaredFfmpeg);
                }

                EnsureExecutablePermission(_ffprobePath);
                EnsureExecutablePermission(_ffmpegPath);

                ValidateResolvedExecutablesPresentOrThrow();
                MarkExecutableResolutionCacheComplete();
                return;
            }

            TrySetExecutablesFromEnvironment();
            if (!string.IsNullOrWhiteSpace(_ffprobePath) &&
                !string.IsNullOrWhiteSpace(_ffmpegPath))
            {
                MarkExecutableResolutionCacheComplete();
                return;
            }

            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
            {
                var workingDirectory = Path.GetDirectoryName(entryAssembly.Location);
                TryFindInStartupBinariesDirectories(workingDirectory);
                if (_ffmpegPath != null &&
                    _ffprobePath != null)
                {
                    MarkExecutableResolutionCacheComplete();
                    return;
                }

                FindProgramsFromPath(workingDirectory);

                if (_ffmpegPath != null &&
                    _ffprobePath != null)
                {
                    MarkExecutableResolutionCacheComplete();
                    return;
                }
            }

            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            var paths = string.IsNullOrEmpty(pathVariable)
                ? Array.Empty<string>()
                : pathVariable.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var path in paths)
            {
                FindProgramsFromPath(path);

                if (_ffmpegPath != null &&
                    _ffprobePath != null)
                {
                    break;
                }
            }

            ValidateResolvedExecutablesPresentOrThrow();
            MarkExecutableResolutionCacheComplete();
        }

        /// <summary>
        ///     Вызывать только при удерживаемом write lock <see cref="_executableConfigurationLock"/>.
        /// </summary>
        private static void SetResolvedFfmpegPathDirect(string value)
        {
            _ffmpegPath = value;
        }

        /// <summary>
        ///     Вызывать только при удерживаемом write lock <see cref="_executableConfigurationLock"/>.
        /// </summary>
        private static void SetResolvedFfprobePathDirect(string value)
        {
            _ffprobePath = value;
        }

        private static void TrySetExecutablesFromEnvironment()
        {
            var ffmpegFromEnv = Environment.GetEnvironmentVariable("FFMPEG_EXECUTABLE")
                               ?? Environment.GetEnvironmentVariable("FFMPEG_PATH");
            var ffprobeFromEnv = Environment.GetEnvironmentVariable("FFPROBE_EXECUTABLE")
                                ?? Environment.GetEnvironmentVariable("FFPROBE_PATH");

            if (!string.IsNullOrWhiteSpace(ffmpegFromEnv) && File.Exists(ffmpegFromEnv))
            {
                ValidateExecutableSignatureOrThrow(ffmpegFromEnv);
                SetResolvedFfmpegPathDirect(ffmpegFromEnv);
                EnsureExecutablePermission(_ffmpegPath);
            }

            if (!string.IsNullOrWhiteSpace(ffprobeFromEnv) && File.Exists(ffprobeFromEnv))
            {
                ValidateExecutableSignatureOrThrow(ffprobeFromEnv);
                SetResolvedFfprobePathDirect(ffprobeFromEnv);
                EnsureExecutablePermission(_ffprobePath);
            }

            if (!string.IsNullOrWhiteSpace(_ffmpegPath) && !string.IsNullOrWhiteSpace(_ffprobePath))
            {
                return;
            }

            var binariesDir = Environment.GetEnvironmentVariable("FFMPEG_BINARIES")
                              ?? Environment.GetEnvironmentVariable("FFMPEG_BINARIES_PATH");
            if (!string.IsNullOrWhiteSpace(binariesDir))
            {
                FindProgramsFromPath(binariesDir);
            }
        }

        private static void TryFindInStartupBinariesDirectories(string startupDirectory)
        {
            if (string.IsNullOrWhiteSpace(startupDirectory))
            {
                return;
            }

            var osFolder = GetCurrentOsFolderName();
            var searchRoots = new List<string> { startupDirectory };
            var current = startupDirectory;
            for (var level = 0; level < 3; level++)
            {
                var parent = Directory.GetParent(current)?.FullName;
                if (string.IsNullOrWhiteSpace(parent))
                {
                    break;
                }

                searchRoots.Add(parent);
                current = parent;
            }

            var candidates = new List<string>();
            foreach (var root in searchRoots.Distinct())
            {
                candidates.Add(Path.Combine(root, "ffmpeg-binaries", osFolder));
                candidates.Add(Path.Combine(root, "ffpmeg-binaries", osFolder));
                candidates.Add(Path.Combine(root, "ffmpeg-binaries"));
                candidates.Add(Path.Combine(root, "ffpmeg-binaries"));
            }

            foreach (var candidate in candidates.Distinct())
            {
                FindProgramsFromPath(candidate);
                if (!string.IsNullOrWhiteSpace(_ffmpegPath) && !string.IsNullOrWhiteSpace(_ffprobePath))
                {
                    return;
                }
            }
        }

        private static string GetCurrentOsFolderName()
        {
            var os = new OperatingSystemProvider().GetOperatingSystem();
            switch (os)
            {
                case OperatingSystem.Windows:
                    return "windows";
                case OperatingSystem.Osx:
                    return "macos";
                case OperatingSystem.Linux:
                    return "linux";
                default:
                    return "linux";
            }
        }

        /// <summary>
        ///     Путь к исполняемому файлу MediaOrchestrator
        /// </summary>
        protected string FFmpegPath
        {
            get
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    return _ffmpegPath;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Путь к исполняемому файлу FFprobe
        /// </summary>
        protected string FFprobePath
        {
            get
            {
                _executableConfigurationLock.EnterReadLock();
                try
                {
                    return _ffprobePath;
                }
                finally
                {
                    _executableConfigurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Вызывать только при удерживаемом write lock <see cref="_executableConfigurationLock"/>.
        /// </summary>
        private static void ValidateResolvedExecutablesPresentOrThrow()
        {
            if (_ffmpegPath != null &&
                _ffprobePath != null)
            {
                return;
            }

            var ffmpegDir = string.IsNullOrWhiteSpace(_executablesPath) ? string.Empty : string.Format(_executablesPath + " or ");
            var exceptionMessage =
                $"Не удалось найти MediaOrchestrator в переменной окружения {ffmpegDir}PATH. " +
                $"Для работы этого пакета требуется установленный MediaOrchestrator. Пожалуйста, " +
                $"добавьте его в переменную PATH или укажите путь к ДИРЕКТОРИИ с исполняемыми файлами " +
                $"MediaOrchestrator в свойстве {nameof(MediaOrchestrator)}.{nameof(ExecutablesPath)}";
            throw new ToolchainNotFoundException(exceptionMessage);
        }

        private static bool IsExecutable(string file, OperatingSystemProvider systemProvider = null, OperatingSystemArchitectureProvider architectureProvider = null)
        {
            try
            {
                using (var fileStream = File.OpenRead(file))
                {
                    var magicNumber = new byte[4];
                    var appMagicNumber = new byte[4];
                    fileStream.Read(magicNumber, 0, 4);
                    var sysProvider = systemProvider ?? new OperatingSystemProvider();
                    var archProvider = architectureProvider ?? new OperatingSystemArchitectureProvider();
                    var architecture = archProvider.GetArchitecture();

                    switch (sysProvider.GetOperatingSystem())
                    {
                        case OperatingSystem.Windows:
                            return magicNumber[0] == 0x4D && magicNumber[1] == 0x5A;
                        case OperatingSystem.Osx:
                            return magicNumber[0] == 0xCF && magicNumber[1] == 0xFA && magicNumber[2] == 0xED && magicNumber[3] == 0xFE;
                        case OperatingSystem.Linux:
                            if (architecture == OperatingSystemArchitecture.X86 || architecture == OperatingSystemArchitecture.X64)
                            {
                                return magicNumber[0] == 0x7F && magicNumber[1] == 0x45 && magicNumber[2] == 0x4C && magicNumber[3] == 0x46;
                            }
                            else
                            {
                                fileStream.Seek(0x30, SeekOrigin.Begin);
                                fileStream.Read(appMagicNumber, 0, 4);
                                return appMagicNumber[0] == 0x50 && appMagicNumber[1] == 0x4B && appMagicNumber[2] == 0x03 && appMagicNumber[3] == 0x04;
                            }
                        default:
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при проверке файла
            }

            return false;
        }

        private static void FindProgramsFromPath(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            IEnumerable<FileInfo> files = new DirectoryInfo(path).GetFiles();

            var ffprobeCandidate = GetFullName(files, _ffprobeExecutableName);
            var ffmpegCandidate = GetFullName(files, _ffmpegExecutableName);
            SetResolvedFfprobePathDirect(ffprobeCandidate);
            SetResolvedFfmpegPathDirect(ffmpegCandidate);
            if (!string.IsNullOrWhiteSpace(ffprobeCandidate) && !IsExecutable(ffprobeCandidate))
            {
                SetResolvedFfprobePathDirect(null);
            }

            if (!string.IsNullOrWhiteSpace(ffmpegCandidate) && !IsExecutable(ffmpegCandidate))
            {
                SetResolvedFfmpegPathDirect(null);
            }

            EnsureExecutablePermission(_ffprobePath);
            EnsureExecutablePermission(_ffmpegPath);
        }

        internal static string GetFullName(IEnumerable<FileInfo> files, string fileName)
        {
            return files.FirstOrDefault(x => x.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)
                   || x.Name.Equals($"{fileName}.exe", StringComparison.InvariantCultureIgnoreCase))
                        ?.FullName;
        }

        private static void EnsureExecutablePermission(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            try
            {
                var chmodProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                using (chmodProcess)
                {
                    chmodProcess.Start();
                    chmodProcess.WaitForExit();
                }
            }
            catch
            {
                // Ничего не делаем: если права выставить не удалось, ниже сработает стандартная проверка запуска.
            }
        }

        private static void ValidateExecutableSignatureOrThrow(string filePath)
        {
            if (!IsExecutable(filePath))
            {
                ThrowExecutableSignatureMismatch(filePath);
            }
        }

        private static void ThrowExecutableSignatureMismatch(string filePath)
        {
            throw new global::MediaOrchestrator.Exceptions.ExecutableSignatureMismatchException(string.Format(ErrorMessages.ExecutableSignatureMismatch, filePath));
        }

        /// <summary>
        ///     Повторно определяет профиль аппаратного ускорения по пути к ffmpeg (или сбрасывает при attemptDetect false / пути нет).
        /// </summary>
        /// <param name="cancellationToken">Отмена во время вызова <c>ffmpeg -hwaccels</c>.</param>
        internal static void RefreshAutoHardwareAccelerationProfile(string ffmpegExecutablePath, bool attemptDetect, CancellationToken cancellationToken = default)
        {
            HardwareAccelerationProfile profile = null;
            if (attemptDetect && !string.IsNullOrWhiteSpace(ffmpegExecutablePath) && File.Exists(ffmpegExecutablePath))
            {
                profile = DetectAutoHardwareAccelerationProfile(ffmpegExecutablePath, cancellationToken);
            }

            _executableConfigurationLock.EnterWriteLock();
            try
            {
                _autoDetectedHardwareAccelerationProfile = profile;
            }
            finally
            {
                _executableConfigurationLock.ExitWriteLock();
            }
        }

        private static HardwareAccelerationProfile DetectAutoHardwareAccelerationProfile(string ffmpegExecutablePath, CancellationToken cancellationToken)
        {
            var os = new OperatingSystemProvider().GetOperatingSystem();
            return HardwareAccelerationAutoDetector.TryDetect(ffmpegExecutablePath, os, cancellationToken, out var profile)
                ? profile
                : null;
        }

        /// <summary>
        ///     Задаёт видеокодек по умолчанию для транскодирования (синоним <see cref="DefaultTranscodeVideoCodec"/>).
        /// </summary>
        public static void SetDefaultVideoCodec(VideoCodec codec)
        {
            DefaultTranscodeVideoCodec = codec;
        }

        /// <summary>
        ///     Задаёт видеокодек по умолчанию для транскодирования.
        /// </summary>
        public static void SetDefaultTranscodeVideoCodec(VideoCodec codec)
        {
            DefaultTranscodeVideoCodec = codec;
        }

        /// <summary>
        ///     Задаёт аудиокодек по умолчанию для транскодирования.
        /// </summary>
        public static void SetDefaultAudioCodec(AudioCodec codec)
        {
            DefaultTranscodeAudioCodec = codec;
        }

        /// <summary>
        ///     Задаёт аудиокодек по умолчанию для транскодирования.
        /// </summary>
        public static void SetDefaultTranscodeAudioCodec(AudioCodec codec)
        {
            DefaultTranscodeAudioCodec = codec;
        }

        /// <summary>
        ///     Имя видеокодека для MediaOrchestrator с учётом автоаппаратного профиля (H.264/HEVC → NVENC/QSV/VAAPI и т.д.).
        /// </summary>
        public static string ResolveTranscodeVideoCodecToString(VideoCodec videoCodec)
        {
            if (videoCodec == VideoCodec.copy)
            {
                return "copy";
            }

            HardwareAccelerationProfile hwProfile;
            _executableConfigurationLock.EnterReadLock();
            try
            {
                hwProfile = _autoDetectedHardwareAccelerationProfile;
            }
            finally
            {
                _executableConfigurationLock.ExitReadLock();
            }

            if (ApplyAutoHardwareAccelerationToConversions && hwProfile != null)
            {
                if (videoCodec == VideoCodec.h264 || videoCodec == VideoCodec.libx264)
                {
                    return hwProfile.H264Encoder;
                }

                if (videoCodec == VideoCodec.hevc)
                {
                    return hwProfile.HevcEncoder;
                }
            }

            return VideoCodecToFfmpegEncoderName(videoCodec);
        }

        /// <summary>
        ///     Имя аудиокодека для MediaOrchestrator (аудио почти всегда программное; AAC и т.д. не подменяются по GPU).
        /// </summary>
        public static string ResolveTranscodeAudioCodecToString(AudioCodec audioCodec)
        {
            if (audioCodec == AudioCodec._4gv)
            {
                return "4gv";
            }

            if (audioCodec == AudioCodec._8svx_exp)
            {
                return "8svx_exp";
            }

            if (audioCodec == AudioCodec._8svx_fib)
            {
                return "8svx_fib";
            }

            return audioCodec.ToString();
        }

        private static string VideoCodecToFfmpegEncoderName(VideoCodec codec)
        {
            var input = codec.ToString();
            if (codec == VideoCodec._8bps)
            {
                input = "8bps";
            }
            else if (codec == VideoCodec._4xm)
            {
                input = "4xm";
            }
            else if (codec == VideoCodec._012v)
            {
                input = "012v";
            }

            return input;
        }

        /// <summary>
        ///     Запускает конвертацию
        /// </summary>
        /// <param name="args">Аргументы</param>
        /// <param name="processPath">Путь к исполняемому файлу (MediaOrchestrator, ffprobe)</param>
        /// <param name="priority">Приоритет процесса для запуска исполняемых файлов</param>
        /// <param name="standardInput">Следует ли перенаправлять стандартный ввод</param>
        /// <param name="standardOutput">Следует ли перенаправлять стандартный вывод</param>
        /// <param name="standardError">Следует ли перенаправлять стандартный вывод ошибок</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        protected Process RunProcess(
            string args,
            string processPath,
            ProcessPriorityClass? priority,
            bool standardInput = false,
            bool standardOutput = false,
            bool standardError = false)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = processPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = standardInput,
                    RedirectStandardOutput = standardOutput,
                    RedirectStandardError = standardError
                },
                EnableRaisingEvents = true
            };

            process.Start();

            try
            {
                process.PriorityClass = priority ?? Process.GetCurrentProcess().PriorityClass;
            }
            catch (Exception)
            {
            }

            return process;
        }
    }
}
