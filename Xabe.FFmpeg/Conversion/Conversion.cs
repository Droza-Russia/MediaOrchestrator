using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg.Events;
using Xabe.FFmpeg.Streams;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Реализует процесс конвертации и позволяет выстраивать параметры FFmpeg.
    /// </summary>
    public partial class Conversion : IConversion
    {
        private readonly object _builderLock = new object();
        private readonly Dictionary<string, int> _inputFileMap = new Dictionary<string, int>();
        private readonly ParametersList<ConversionParameter> _parameters = new ParametersList<ConversionParameter>();
        private readonly IDictionary<ParameterPosition, List<string>> _userDefinedParameters = new Dictionary<ParameterPosition, List<string>>();
        private readonly List<IStream> _streams = new List<IStream>();
        private readonly IAudioConversionSettings _audioSettings;
        private readonly IVideoConversionSettings _videoSettings;

        private string _output;
        private bool _hasInputBuilder = false;

        private ProcessPriorityClass? _priority = null;
        private FFmpegWrapper _ffmpeg;
        private Func<string, string> _buildInputFileName = null;
        private Func<string, string> _buildOutputFileName = null;

        public Conversion()
        {
            _userDefinedParameters[ParameterPosition.PostInput] = new List<string>();
            _userDefinedParameters[ParameterPosition.PreInput] = new List<string>();
            _audioSettings = new AudioConversionSettings(this);
            _videoSettings = new VideoConversionSettings(this);
        }

        /// <summary>
        ///     Собирает строку аргументов FFmpeg, основываясь на заданных параметрах и потоках.
        /// </summary>
        /// <returns>Строка параметров для запуска процесса FFmpeg.</returns>
        public string Build()
        {
            lock (_builderLock)
            {
                var builder = new StringBuilder();

                if (_buildOutputFileName == null)
                {
                    _buildOutputFileName = (number) => { return _output; };
                }

                builder.Append(string.Join(" ", _userDefinedParameters[ParameterPosition.PreInput].Select(x => x.Trim())) + " ");
                builder.Append(GetParameters(ParameterPosition.PreInput));
                builder.Append(GetStreamsPreInputs());

                if (_buildInputFileName == null)
                {
                    builder.Append(GetInputs());
                }
                else
                {
                    _hasInputBuilder = true;
                    builder.Append(_buildInputFileName("_%03d"));
                    builder.Append(GetInputs());
                }

                builder.Append(GetStreamsPostInputs());
                builder.Append(GetFilters());
                builder.Append(GetMap());
                builder.Append(GetParameters(ParameterPosition.PostInput));
                builder.Append(string.Join(" ", _userDefinedParameters[ParameterPosition.PostInput].Select(x => x.Trim())) + " ");
                builder.Append(_buildOutputFileName("_%03d"));

                return builder.ToString();
            }
        }

        /// <summary>
        ///     Событие обновления прогресса FFmpeg.
        /// </summary>
        public event ConversionProgressEventHandler OnProgress;

        /// <summary>
        ///     Событие, возникающее при выводе текста FFmpeg.
        /// </summary>
        public event DataReceivedEventHandler OnDataReceived;

        /// <summary>
        ///     Событие, возникающее при получении видеоданных из pipe (требует PipeOutput()).
        /// </summary>
        public event VideoDataEventHandler OnVideoDataReceived;

        /// <summary>
        ///     Путь к выходному файлу.
        /// </summary>
        public string OutputFilePath { get; private set; }

        /// <summary>
        ///     Дескриптор канала вывода.
        /// </summary>
        public PipeDescriptor? OutputPipeDescriptor { get; private set; }

        /// <summary>
        ///     Раздел аудио-настроек конвертации.
        /// </summary>
        public IAudioConversionSettings Audio => _audioSettings;

        /// <summary>
        ///     Раздел видео-настроек конвертации.
        /// </summary>
        public IVideoConversionSettings Video => _videoSettings;

        /// <summary>
        ///     Перечисление всех потоков, добавленных в конвертацию.
        /// </summary>
        public IEnumerable<IStream> Streams => _streams;

        /// <summary>
        ///     Запускает конвертацию с текущими параметрами.
        /// </summary>
        /// <returns>Результат конвертации.</returns>
        public Task<IConversionResult> Start()
        {
            return Start(Build());
        }

        /// <summary>
        ///     Запускает конвертацию с возможностью отмены.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат конвертации.</returns>
        public Task<IConversionResult> Start(CancellationToken cancellationToken)
        {
            return Start(Build(), cancellationToken);
        }

        /// <summary>
        ///     Запускает FFmpeg с указанными параметрами.
        /// </summary>
        /// <param name="parameters">Строка параметров для FFmpeg.</param>
        /// <returns>Результат конвертации.</returns>
        public Task<IConversionResult> Start(string parameters)
        {
            return Start(parameters, new CancellationToken());
        }

        /// <summary>
        ///     Запускает FFmpeg с заданными параметрами и токеном отмены.
        /// </summary>
        /// <param name="parameters">Строка параметров для FFmpeg.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversionResult> Start(string parameters, CancellationToken cancellationToken)
        {
            if (_ffmpeg != null)
            {
                throw new InvalidOperationException("Конвертация уже была запущена. ");
            }

            DateTime startTime = DateTime.Now;

            _ffmpeg = new FFmpegWrapper();
            try
            {
                _ffmpeg.OnProgress += OnProgress;
                _ffmpeg.OnDataReceived += OnDataReceived;
                _ffmpeg.OnVideoDataReceived += OnVideoDataReceived;
                CreateOutputDirectoryIfNotExists();
                await _ffmpeg.RunProcess(parameters, cancellationToken, _priority);
            }
            finally
            {
                _ffmpeg.OnProgress -= OnProgress;
                _ffmpeg.OnDataReceived -= OnDataReceived;
                _ffmpeg.OnVideoDataReceived -= OnVideoDataReceived;
                _ffmpeg = null;
            }

            return new ConversionResult
            {
                StartTime = startTime,
                EndTime = DateTime.Now,
                Arguments = parameters
            };
        }

        private void CreateOutputDirectoryIfNotExists()
        {
            if (OutputFilePath == null || OutputPipeDescriptor != null)
            {
                return;
            }

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(OutputFilePath.Unescape())))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(OutputFilePath.Unescape()));
                }
            }
            catch (IOException)
            {
            }
        }

        /// <summary>
        ///     Указывает длительность анализа входного потока FFmpeg.
        /// </summary>
        /// <param name="duration">Продолжительность анализа.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetAnalysisDuration(TimeSpan duration)
        {
            // FFmpeg ожидает микросекунды (1 tick = 100 наносекунд, 10 ticks = 1 микросекунда)
            long microseconds = duration.Ticks / 10;

            _parameters.Add(new ConversionParameter($"-analyzeduration {microseconds}", ParameterPosition.PostInput));
            return this;
        }


        /// <summary>
        ///     Добавляет произвольный параметр к команде FFmpeg.
        /// </summary>
        /// <param name="parameter">Строка параметра.</param>
        /// <param name="parameterPosition">Позиция параметра относительно входных файлов.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion AddParameter(string parameter, ParameterPosition parameterPosition = ParameterPosition.PostInput)
        {
            _userDefinedParameters[parameterPosition].Add(parameter);
            return this;
        }

        /// <summary>
        ///     Добавляет один или несколько потоков к конвертации.
        /// </summary>
        /// <param name="streams">Потоки для добавления.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion AddStream<T>(params T[] streams) where T : IStream
        {
            foreach (T stream in streams)
            {
                if (stream != null)
                {
                    _streams.Add(stream);
                }
            }

            return this;
        }

        /// <summary>
        ///     Добавляет коллекцию потоков к конвертации.
        /// </summary>
        /// <param name="streams">Коллекция потоков.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion AddStream(IEnumerable<IStream> streams)
        {
            foreach (var stream in streams)
            {
                AddStream(stream);
            }

            return this;
        }

        /// <summary>
        ///     Устанавливает формат хеша для выходного потока в виде перечисления.
        /// </summary>
        /// <param name="hashFormat">Выбранный формат хеша.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetHashFormat(Hash hashFormat = Hash.SHA256)
        {
            var format = hashFormat.ToString();
            if (hashFormat == Hash.SHA512_256)
            {
                format = "SHA512/256";
            }
            else if (hashFormat == Hash.SHA512_224)
            {
                format = "SHA512/224";
            }

            SetOutputFormat(Format.hash);
            return SetHashFormat(format);
        }

        /// <summary>
        ///     Устанавливает формат хеша на основе строкового обозначения.
        /// </summary>
        /// <param name="hashFormat">Строковое представление формата хеша.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetHashFormat(string hashFormat)
        {
            _parameters.Add(new ConversionParameter($"-hash {hashFormat}", ParameterPosition.PostInput));
            return this;
        }

        /// <summary>
        ///     Выбирает пресет FFmpeg, влияющий на скорость и качество.
        /// </summary>
        /// <param name="preset">Предустановка кодирования.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetPreset(ConversionPreset preset)
        {
            _parameters.Add(new ConversionParameter($"-preset {preset.ToString().ToLower()}", ParameterPosition.PostInput));
            return this;
        }

        /// <summary>
        ///     Перемещает указатель времени в выходном файле (-ss).
        /// </summary>
        /// <param name="seek">Позиция начала.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetSeek(TimeSpan? seek)
        {
            if (seek.HasValue)
            {
                _parameters.Add(new ConversionParameter($"-ss {seek.Value.ToFFmpeg()}", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Ограничивает длительность входных данных (-t до входа).
        /// </summary>
        /// <param name="time">Продолжительность входа.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetInputTime(TimeSpan? time)
        {
            if (time.HasValue)
            {
                _parameters.Add(new ConversionParameter($"-t {time.Value.ToFFmpeg()}", ParameterPosition.PreInput));
            }

            return this;
        }

        /// <summary>
        ///     Ограничивает длительность выходного файла (-t после входа).
        /// </summary>
        /// <param name="time">Продолжительность выхода.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetOutputTime(TimeSpan? time)
        {
            if (time.HasValue)
            {
                _parameters.Add(new ConversionParameter($"-t {time.Value.ToFFmpeg()}", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Использует многопоточность, ограниченную 16 потоками.
        /// </summary>
        /// <param name="multiThread">Разрешить использование всех ядер.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion UseMultiThread(bool multiThread)
        {
            var threads = multiThread ? Environment.ProcessorCount : 1;
            _parameters.Add(new ConversionParameter($"-threads {Math.Min(threads, 16)}"));
            return this;
        }

        /// <summary>
        ///     Указывает точное количество потоков FFmpeg.
        /// </summary>
        /// <param name="threadsCount">Число нитей.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion UseMultiThread(int threadsCount)
        {
            _parameters.Add(new ConversionParameter($"-threads {threadsCount}"));
            return this;
        }

        /// <summary>
        ///     Устанавливает путь к выходному файлу.
        /// </summary>
        /// <param name="outputPath">Путь к файлу.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetOutput(string outputPath)
        {
            OutputFilePath = new FileInfo(outputPath).FullName;
            _output = outputPath.Escape();
            return this;
        }

        /// <summary>
        ///     Перенаправляет вывод FFmpeg в pipe.
        /// </summary>
        /// <param name="descriptor">Выбранный дескриптор pipe.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion PipeOutput(PipeDescriptor descriptor = PipeDescriptor.stdout)
        {
            SetOutput($"pipe:{(int)descriptor}");
            OutputPipeDescriptor = descriptor;
            return this;
        }

        /// <summary>
        ///     Устанавливает битрейт для видеопотоков и соответствующие параметры.
        /// </summary>
        /// <param name="bitrate">Целевой битрейт.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetVideoBitrate(long bitrate)
        {
            _parameters.Add(new ConversionParameter($"-b:v {bitrate}", ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter($"-minrate {bitrate}", ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter($"-maxrate {bitrate}", ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter($"-bufsize {bitrate}", ParameterPosition.PostInput));

            if (HasH264Stream())
            {
                _parameters.Add(new ConversionParameter($"-x264opts nal-hrd=cbr:force-cfr=1", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Устанавливает битрейт аудиопотоков.
        /// </summary>
        /// <param name="bitrate">Целевой битрейт для аудио.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetAudioBitrate(long bitrate)
        {
            _parameters.Add(new ConversionParameter($"-b:a {bitrate}", ParameterPosition.PostInput));
            return this;
        }

        /// <summary>
        ///     Завершает конвертацию по достижении самого короткого потока (-shortest).
        /// </summary>
        /// <param name="useShortest">Признак включения опции.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion UseShortest(bool useShortest)
        {
            if (useShortest)
            {
                _parameters.Add(new ConversionParameter($"-shortest", ParameterPosition.PostInput));
            }
            else
            {
                _parameters.Remove(new ConversionParameter($"-shortest", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Устанавливает приоритет запускаемого процесса FFmpeg.
        /// </summary>
        /// <param name="priority">Приоритет процесса.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetPriority(ProcessPriorityClass? priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>
        ///     Извлекает каждый frameNo-й кадр и записывает его как изображение.
        /// </summary>
        /// <param name="frameNo">Интервал выборки.</param>
        /// <param name="buildOutputFileName">Функция генерации имени файла.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion ExtractEveryNthFrame(int frameNo, Func<string, string> buildOutputFileName)
        {
            _buildOutputFileName = buildOutputFileName;
            OutputFilePath = buildOutputFileName("");
            _parameters.Add(new ConversionParameter($"-vf select='not(mod(n\\,{frameNo}))'", ParameterPosition.PostInput));
            SetVideoSyncMethod(VideoSyncMethod.vfr);

            return this;
        }

        /// <summary>
        ///     Извлекает конкретный кадр по индексу.
        /// </summary>
        /// <param name="frameNo">Номер кадра.</param>
        /// <param name="buildOutputFileName">Функция генерации имени файла.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion ExtractNthFrame(int frameNo, Func<string, string> buildOutputFileName)
        {
            _buildOutputFileName = buildOutputFileName;
            _parameters.Add(new ConversionParameter($"-vf select='eq(n\\,{frameNo})'", ParameterPosition.PostInput));
            OutputFilePath = buildOutputFileName("");
            SetVideoSyncMethod(VideoSyncMethod.passthrough);
            return this;
        }

        /// <summary>
        ///     Собирает видео из изображений, начиная с указанного номера.
        /// </summary>
        /// <param name="startNumber">Номер первого изображения.</param>
        /// <param name="buildInputFileName">Генератор имен входных файлов.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion BuildVideoFromImages(int startNumber, Func<string, string> buildInputFileName)
        {
            _buildInputFileName = buildInputFileName;
            _parameters.Add(new ConversionParameter($"-start_number {startNumber}", ParameterPosition.PreInput));
            return this;
        }

        /// <summary>
        ///     Собирает видео из заданного списка изображений.
        /// </summary>
        /// <param name="imageFiles">Список файлов изображений.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion BuildVideoFromImages(IEnumerable<string> imageFiles)
        {
            var builder = new InputBuilder();
            _buildInputFileName = builder.PrepareInputFiles(imageFiles.ToList(), out _);

            return this;
        }

        /// <summary>
        ///     Устанавливает частоту кадров для входного потока (-framerate и -r до входа).
        /// </summary>
        /// <param name="frameRate">Желаемая частота.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetInputFrameRate(double frameRate)
        {
            _parameters.Add(new ConversionParameter($"-framerate {frameRate.ToFFmpegFormat(3)}", ParameterPosition.PreInput));
            _parameters.Add(new ConversionParameter($"-r {frameRate.ToFFmpegFormat(3)}", ParameterPosition.PreInput));
            return this;
        }

        /// <summary>
        ///     Устанавливает частоту кадров выходного видео (-framerate и -r после входов).
        /// </summary>
        /// <param name="frameRate">Желаемая частота.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetFrameRate(double frameRate)
        {
            _parameters.Add(new ConversionParameter($"-framerate {frameRate.ToFFmpegFormat(3)}", ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter($"-r {frameRate.ToFFmpegFormat(3)}", ParameterPosition.PostInput));
            return this;
        }

        private string GetStreamsPostInputs()
        {
            var builder = new StringBuilder();
            foreach (IStream stream in _streams)
            {
                builder.Append(stream.BuildParameters(ParameterPosition.PostInput));
            }

            return builder.ToString();
        }

        private string GetStreamsPreInputs()
        {
            var builder = new StringBuilder();
            foreach (IStream stream in _streams)
            {
                builder.Append(stream.BuildParameters(ParameterPosition.PreInput));
            }

            return builder.ToString();
        }

        private string GetFilters()
        {
            var builder = new StringBuilder();
            var configurations = new List<IFilterConfiguration>();
            foreach (IStream stream in _streams)
            {
                if (stream is IFilterable filterable)
                {
                    configurations.AddRange(filterable.GetFilters());
                }
            }

            IEnumerable<IGrouping<string, IFilterConfiguration>> filterGroups = configurations.GroupBy(configuration => configuration.FilterType);
            foreach (IGrouping<string, IFilterConfiguration> filterGroup in filterGroups)
            {
                builder.Append($"{filterGroup.Key} \"");
                var isFirstFilter = true;
                foreach (IFilterConfiguration configuration in filterGroup)
                {
                    foreach (KeyValuePair<string, string> filter in configuration.Filters)
                    {
                        if (!isFirstFilter)
                        {
                            builder.Append(";");
                        }

                        var map = $"[{configuration.StreamNumber}]";
                        var value = string.IsNullOrEmpty(filter.Value) ? $"{filter.Key} " : $"{filter.Key}={filter.Value}";
                        builder.Append($"{map} {value} ");
                        isFirstFilter = false;
                    }
                }

                builder.Append("\" ");
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Создает карту для включенных потоков, включая InputBuilder при необходимости
        /// </summary>
        /// <returns>Аргумент карты</returns>
        private string GetMap()
        {
            var builder = new StringBuilder();
            foreach (IStream stream in _streams)
            {
                if (_hasInputBuilder) // Если у нас есть построитель входных данных, мы всегда хотим сопоставить первый видеопоток, так как он будет создан нашим построителем входных данных
                {
                    builder.Append($"-map 0:0 ");
                }

                foreach (var source in stream.GetSource())
                {
                    if (_hasInputBuilder)
                    {
                        // Если у нас есть построитель входных данных, нам нужно добавить единицу к индексу входного файла, чтобы учесть вход, созданный нашим построителем входных данных.
                        builder.Append($"-map {_inputFileMap[source] + 1}:{stream.Index} ");
                    }
                    else
                    {
                        builder.Append($"-map {_inputFileMap[source]}:{stream.Index} ");
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Создает строку параметров
        /// </summary>
        /// <param name="forPosition">Позиция для параметров</param>
        /// <returns>Параметры</returns>
        private string GetParameters(ParameterPosition forPosition)
        {
            IEnumerable<ConversionParameter> parameters = _parameters?.Where(x => x.Position == forPosition);
            if (parameters != null &&
                parameters.Any())
            {
                return string.Join(string.Empty, parameters.Select(x => x.Parameter));
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///     Создает строку входных данных для всех потоков
        /// </summary>
        /// <returns>Аргумент входных данных</returns>
        private string GetInputs()
        {
            var builder = new StringBuilder();
            var index = 0;
            foreach (var source in _streams.SelectMany(x => x.GetSource()).Distinct())
            {
                _inputFileMap[source] = index++;
                builder.Append($"-i {source.Escape()} ");
            }

            return builder.ToString();
        }

        private bool HasH264Stream()
        {
            foreach (IStream stream in _streams)
            {
                if (stream is IVideoStream s)
                {
                    if (s.Codec == "libx264" ||
                        s.Codec == VideoCodec.h264.ToString())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static IConversion New()
        {
            var conversion = new Conversion();
            return conversion
                .SetOverwriteOutput(false);
        }

        /// <summary>
        ///     Включает аппаратное ускорение с использованием перечислений кодеков.
        /// </summary>
        /// <param name="hardwareAccelerator">Аппаратный ускоритель.</param>
        /// <param name="decoder">Кодек декодирования.</param>
        /// <param name="encoder">Кодек кодирования.</param>
        /// <param name="device">Номер устройства (по умолчанию 0).</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion UseHardwareAcceleration(HardwareAccelerator hardwareAccelerator, VideoCodec decoder, VideoCodec encoder, int device = 0)
        {
            return UseHardwareAcceleration($"{hardwareAccelerator}", decoder.ToString(), encoder.ToString(), device);
        }

        /// <summary>
        ///     Включает аппаратное ускорение с использованием строковых идентификаторов кодеков.
        /// </summary>
        /// <param name="hardwareAccelerator">Имя аппаратного ускорителя.</param>
        /// <param name="decoder">Кодек декодирования.</param>
        /// <param name="encoder">Кодек кодирования.</param>
        /// <param name="device">Номер устройства (по умолчанию 0).</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion UseHardwareAcceleration(string hardwareAccelerator, string decoder, string encoder, int device = 0)
        {
            _parameters.Add(new ConversionParameter($"-hwaccel {hardwareAccelerator}", ParameterPosition.PreInput));
            _parameters.Add(new ConversionParameter($"-c:v {decoder}", ParameterPosition.PreInput));

            _parameters.Add(new ConversionParameter($"-c:v {encoder?.ToString()}", ParameterPosition.PostInput));

            if (device != 0)
            {
                _parameters.Add(new ConversionParameter($"-hwaccel_device {device}", ParameterPosition.PreInput));
            }

            UseMultiThread(false);
            return this;
        }

        /// <summary>
        ///     Определяет поведение перезаписи выходного файла.
        /// </summary>
        /// <param name="overwrite">Перезаписывать файл, если он существует.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetOverwriteOutput(bool overwrite)
        {
            if (overwrite)
            {
                _parameters.Add(new ConversionParameter($"-y", ParameterPosition.PostInput));
                _parameters.Remove(new ConversionParameter($"-n", ParameterPosition.PostInput));
            }
            else
            {
                _parameters.Remove(new ConversionParameter($"-y", ParameterPosition.PostInput));
                _parameters.Add(new ConversionParameter($"-n", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Задает формат входного файла через перечисление Format.
        /// </summary>
        /// <param name="inputFormat">Формат входных данных.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetInputFormat(Format inputFormat)
        {
            var format = inputFormat.ToString();
            switch (inputFormat)
            {
                case Format._3dostr:
                    format = "3dostr";
                    break;
                case Format._3g2:
                    format = "3g2";
                    break;
                case Format._3gp:
                    format = "3gp";
                    break;
                case Format._4xm:
                    format = "4xm";
                    break;
                default:
                    break;
            }

            return SetInputFormat(format);
        }

        /// <summary>
        ///     Задает формат входного файла через строковое имя.
        /// </summary>
        /// <param name="format">Строковое название формата.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetInputFormat(string format)
        {
            if (format != null)
            {
                _parameters.Add(new ConversionParameter($"-f {format}", ParameterPosition.PreInput));
            }

            return this;
        }

        /// <summary>
        ///     Задает формат выходного файла через перечисление Format.
        /// </summary>
        /// <param name="outputFormat">Формат выходных данных.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetOutputFormat(Format outputFormat)
        {
            var format = outputFormat.ToString();
            switch (outputFormat)
            {
                case Format._3dostr:
                    format = "3dostr";
                    break;
                case Format._3g2:
                    format = "3g2";
                    break;
                case Format._3gp:
                    format = "3gp";
                    break;
                case Format._4xm:
                    format = "4xm";
                    break;
                default:
                    break;
            }

            return SetOutputFormat(format);
        }

        /// <summary>
        ///     Задает формат выходного файла через строковое имя.
        /// </summary>
        /// <param name="format">Строковое название формата.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetOutputFormat(string format)
        {
            if (format != null)
            {
                _parameters.Add(new ConversionParameter($"-f {format}", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Устанавливает формат пикселей выходного видео через перечисление.
        /// </summary>
        /// <param name="pixelFormat">Формат пикселей.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetPixelFormat(PixelFormat pixelFormat)
        {
            var format = pixelFormat.ToString();
            switch (pixelFormat)
            {
                case PixelFormat._0bgr:
                    format = "0bgr";
                    break;
                case PixelFormat._0rgb:
                    format = "0rgb";
                    break;
                default:
                    break;
            }

            return SetPixelFormat(format);
        }

        /// <summary>
        ///     Устанавливает формат пикселей выходного видео через строковое имя.
        /// </summary>
        /// <param name="pixelFormat">Строковое обозначение формата пикселей.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetPixelFormat(string pixelFormat)
        {
            if (pixelFormat != null)
            {
                _parameters.Add(new ConversionParameter($"-pix_fmt {pixelFormat}", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Задает метод синхронизации видео (-vsync).
        /// </summary>
        /// <param name="method">Метод синхронизации.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetVideoSyncMethod(VideoSyncMethod method)
        {
            if (method == VideoSyncMethod.auto)
            {
                _parameters.Add(new ConversionParameter($"-vsync -1", ParameterPosition.PostInput));
            }
            else
            {
                _parameters.Add(new ConversionParameter($"-vsync {method}", ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Добавляет поток захвата рабочего стола по параметрам.
        /// </summary>
        /// <param name="videoSize">Размер окна захвата.</param>
        /// <param name="framerate">Частота кадров.</param>
        /// <param name="xOffset">Смещение по X.</param>
        /// <param name="yOffset">Смещение по Y.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion AddDesktopStream(string videoSize = null, double framerate = 30, int xOffset = 0, int yOffset = 0)
        {
            var stream = new VideoStream() { Index = _streams.Any() ? _streams.Max(x => x.Index) + 1 : 0 };
            stream.AddParameter($"-framerate {framerate.ToFFmpegFormat(4)}", ParameterPosition.PreInput);
            stream.AddParameter($"-offset_x {xOffset}", ParameterPosition.PreInput);
            stream.AddParameter($"-offset_y {yOffset}", ParameterPosition.PreInput);

            if (videoSize != null)
            {
                stream.AddParameter($"-video_size {videoSize}", ParameterPosition.PreInput);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                stream.SetInputFormat(Format.gdigrab);
                stream.Path = "desktop";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                stream.SetInputFormat(Format.avfoundation);
                stream.Path = "1:1";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                stream.SetInputFormat(Format.x11grab);
                stream.Path = ":0.0+0,0";
            }

            _streams.Add(stream);
            return this;
        }

        /// <summary>
        ///     Добавляет поток захвата рабочего стола с типизированным размером.
        /// </summary>
        /// <param name="videoSize">Размер из перечисления VideoSize.</param>
        /// <param name="framerate">Частота кадров.</param>
        /// <param name="xOffset">Смещение по X.</param>
        /// <param name="yOffset">Смещение по Y.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion AddDesktopStream(VideoSize videoSize, double framerate = 30, int xOffset = 0, int yOffset = 0)
        {
            return AddDesktopStream(videoSize.ToFFmpegFormat(), framerate, xOffset, yOffset);
        }
    }
}
