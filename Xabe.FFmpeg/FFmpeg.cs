using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xabe.FFmpeg.Exceptions;

namespace Xabe.FFmpeg
{
    public enum FileNameFilterMethod
    {
        Contains,
        Exact,
        StartWith
    }

    /// <summary> 
    ///     Обертка для FFmpeg
    /// </summary>
    public abstract partial class FFmpeg
    {
        private static string _ffmpegPath;
        private static string _ffprobePath;
        private static string _lastExecutablePath = Guid.NewGuid().ToString();

        private static readonly object _ffmpegPathLock = new object();
        private static readonly object _ffprobePathLock = new object();
        private static string _ffmpegExecutableName = "ffmpeg";
        private static string _ffprobeExecutableName = "ffprobe";

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
        internal static HardwareAccelerationProfile AutoDetectedHardwareAccelerationProfile { get; private set; }

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
        ///     Инициализирует новый FFmpeg. Ищет FFmpeg и FFprobe в PATH
        /// </summary>
        /// 
        protected FFmpeg()
        {
            FindAndValidateExecutables();
        }

        private void FindAndValidateExecutables()
        {
            if (!string.IsNullOrWhiteSpace(FFprobePath) &&
               !string.IsNullOrWhiteSpace(FFmpegPath) && _lastExecutablePath.Equals(ExecutablesPath))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(ExecutablesPath))
            {
                var files = new DirectoryInfo(ExecutablesPath).GetFiles();
                Func<string, string, IFormatProvider, bool> compareMethod;
                switch (FilterMethod)
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

                FFprobePath = files.FirstOrDefault(x => compareMethod(x.Name, _ffprobeExecutableName, FormatProvider) && IsExecutable(x.FullName))?.FullName;
                FFmpegPath = files.FirstOrDefault(x => compareMethod(x.Name, _ffmpegExecutableName, FormatProvider) && IsExecutable(x.FullName))?.FullName;

                var declaredFfprobe = files.FirstOrDefault(x => compareMethod(x.Name, _ffprobeExecutableName, FormatProvider))?.FullName;
                var declaredFfmpeg = files.FirstOrDefault(x => compareMethod(x.Name, _ffmpegExecutableName, FormatProvider))?.FullName;
                if (!string.IsNullOrWhiteSpace(declaredFfprobe) && string.IsNullOrWhiteSpace(FFprobePath))
                {
                    ThrowExecutableSignatureMismatch(declaredFfprobe);
                }

                if (!string.IsNullOrWhiteSpace(declaredFfmpeg) && string.IsNullOrWhiteSpace(FFmpegPath))
                {
                    ThrowExecutableSignatureMismatch(declaredFfmpeg);
                }

                EnsureExecutablePermission(FFprobePath);
                EnsureExecutablePermission(FFmpegPath);

                ValidateExecutables();
                _lastExecutablePath = ExecutablesPath;
                return;
            }

            TrySetExecutablesFromEnvironment();
            if (!string.IsNullOrWhiteSpace(FFprobePath) &&
                !string.IsNullOrWhiteSpace(FFmpegPath))
            {
                return;
            }

            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
            {
                var workingDirectory = Path.GetDirectoryName(entryAssembly.Location);
                TryFindInStartupBinariesDirectories(workingDirectory);
                if (FFmpegPath != null &&
                    FFprobePath != null)
                {
                    return;
                }

                FindProgramsFromPath(workingDirectory);

                if (FFmpegPath != null &&
                   FFprobePath != null)
                {
                    return;
                }
            }

            var paths = Environment.GetEnvironmentVariable("PATH")
                                        .Split(Path.PathSeparator);

            foreach (var path in paths)
            {
                FindProgramsFromPath(path);

                if (FFmpegPath != null &&
                   FFprobePath != null)
                {
                    break;
                }
            }

            ValidateExecutables();
        }

        private void TrySetExecutablesFromEnvironment()
        {
            var ffmpegFromEnv = Environment.GetEnvironmentVariable("FFMPEG_EXECUTABLE")
                               ?? Environment.GetEnvironmentVariable("FFMPEG_PATH");
            var ffprobeFromEnv = Environment.GetEnvironmentVariable("FFPROBE_EXECUTABLE")
                                ?? Environment.GetEnvironmentVariable("FFPROBE_PATH");

            if (!string.IsNullOrWhiteSpace(ffmpegFromEnv) && File.Exists(ffmpegFromEnv))
            {
                ValidateExecutableSignatureOrThrow(ffmpegFromEnv);
                FFmpegPath = ffmpegFromEnv;
                EnsureExecutablePermission(FFmpegPath);
            }

            if (!string.IsNullOrWhiteSpace(ffprobeFromEnv) && File.Exists(ffprobeFromEnv))
            {
                ValidateExecutableSignatureOrThrow(ffprobeFromEnv);
                FFprobePath = ffprobeFromEnv;
                EnsureExecutablePermission(FFprobePath);
            }

            if (!string.IsNullOrWhiteSpace(FFmpegPath) && !string.IsNullOrWhiteSpace(FFprobePath))
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

        private void TryFindInStartupBinariesDirectories(string startupDirectory)
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
                if (!string.IsNullOrWhiteSpace(FFmpegPath) && !string.IsNullOrWhiteSpace(FFprobePath))
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
        ///     Путь к исполняемому файлу FFmpeg
        /// </summary>
        protected string FFmpegPath
        {
            get
            {
                lock (_ffmpegPathLock)
                {
                    return _ffmpegPath;
                }
            }

            private set
            {
                lock (_ffmpegPathLock)
                {
                    _ffmpegPath = value;
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
                lock (_ffprobePathLock)
                {
                    return _ffprobePath;
                }
            }

            private set
            {
                lock (_ffprobePathLock)
                {
                    _ffprobePath = value;
                }
            }
        }

        private void ValidateExecutables()
        {
            if (FFmpegPath != null &&
               FFprobePath != null)
            {
                return;
            }

            var ffmpegDir = string.IsNullOrWhiteSpace(ExecutablesPath) ? string.Empty : string.Format(ExecutablesPath + " or ");
            var exceptionMessage =
                $"Не удалось найти FFmpeg в переменной окружения {ffmpegDir}PATH. " +
                $"Для работы этого пакета требуется установленный FFmpeg. Пожалуйста, " +
                $"добавьте его в переменную PATH или укажите путь к ДИРЕКТОРИИ с исполняемыми файлами " +
                $"FFmpeg в свойстве {nameof(FFmpeg)}.{nameof(ExecutablesPath)}";
            throw new FFmpegNotFoundException(exceptionMessage);
        }

        private bool IsExecutable(string file, OperatingSystemProvider systemProvider = null, OperatingSystemArchitectureProvider architectureProvider = null)
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

        private void FindProgramsFromPath(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            IEnumerable<FileInfo> files = new DirectoryInfo(path).GetFiles();

            FFprobePath = GetFullName(files, _ffprobeExecutableName);
            FFmpegPath = GetFullName(files, _ffmpegExecutableName);
            if (!string.IsNullOrWhiteSpace(FFprobePath) && !IsExecutable(FFprobePath))
            {
                FFprobePath = null;
            }

            if (!string.IsNullOrWhiteSpace(FFmpegPath) && !IsExecutable(FFmpegPath))
            {
                FFmpegPath = null;
            }
            EnsureExecutablePermission(FFprobePath);
            EnsureExecutablePermission(FFmpegPath);
        }

        internal static string GetFullName(IEnumerable<FileInfo> files, string fileName)
        {
            return files.FirstOrDefault(x => x.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)
                   || x.Name.Equals($"{fileName}.exe", StringComparison.InvariantCultureIgnoreCase))
                        ?.FullName;
        }

        private void EnsureExecutablePermission(string filePath)
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

        private void ValidateExecutableSignatureOrThrow(string filePath)
        {
            if (!IsExecutable(filePath))
            {
                ThrowExecutableSignatureMismatch(filePath);
            }
        }

        private void ThrowExecutableSignatureMismatch(string filePath)
        {
            throw new global::Xabe.FFmpeg.Exceptions.ExecutableSignatureMismatchException(string.Format(ErrorMessages.ExecutableSignatureMismatch, filePath));
        }

        /// <summary>
        ///     Повторно определяет профиль аппаратного ускорения по пути к ffmpeg (или сбрасывает при attemptDetect false / пути нет).
        /// </summary>
        internal static void RefreshAutoHardwareAccelerationProfile(string ffmpegExecutablePath, bool attemptDetect)
        {
            if (!attemptDetect || string.IsNullOrWhiteSpace(ffmpegExecutablePath) || !File.Exists(ffmpegExecutablePath))
            {
                AutoDetectedHardwareAccelerationProfile = null;
                return;
            }

            var os = new OperatingSystemProvider().GetOperatingSystem();
            HardwareAccelerationAutoDetector.TryDetect(ffmpegExecutablePath, os, out var profile);
            AutoDetectedHardwareAccelerationProfile = profile;
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
        ///     Имя видеокодека для FFmpeg с учётом автоаппаратного профиля (H.264/HEVC → NVENC/QSV/VAAPI и т.д.).
        /// </summary>
        public static string ResolveTranscodeVideoCodecToString(VideoCodec videoCodec)
        {
            if (videoCodec == VideoCodec.copy)
            {
                return "copy";
            }

            if (ApplyAutoHardwareAccelerationToConversions && AutoDetectedHardwareAccelerationProfile != null)
            {
                if (videoCodec == VideoCodec.h264 || videoCodec == VideoCodec.libx264)
                {
                    return AutoDetectedHardwareAccelerationProfile.H264Encoder;
                }

                if (videoCodec == VideoCodec.hevc)
                {
                    return AutoDetectedHardwareAccelerationProfile.HevcEncoder;
                }
            }

            return VideoCodecToFfmpegEncoderName(videoCodec);
        }

        /// <summary>
        ///     Имя аудиокодека для FFmpeg (аудио почти всегда программное; AAC и т.д. не подменяются по GPU).
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
        /// <param name="processPath">Путь к исполняемому файлу (FFmpeg, ffprobe)</param>
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
