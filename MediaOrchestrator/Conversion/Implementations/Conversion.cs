using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Models;
using MediaOrchestrator.Events;
using MediaOrchestrator.Exceptions;
using MediaOrchestrator.Extensions;
using MediaOrchestrator.Streams;
using MediaOrchestrator.Streams.Collections;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Реализует процесс конвертации и позволяет выстраивать параметры MediaOrchestrator.
    /// </summary>
    public partial class Conversion : IConversion
    {
        private readonly object _builderLock = new object();
        private readonly Dictionary<string, int> _inputFileMap = new Dictionary<string, int>();
        private readonly ParametersList<ConversionParameter> _parameters = new ParametersList<ConversionParameter>();
        private readonly IDictionary<ParameterPosition, List<string>> _userDefinedParameters = new Dictionary<ParameterPosition, List<string>>();
        private readonly List<IStream> _streams = new List<IStream>();
        private readonly List<string> _pipeInputs = new List<string>();
        private readonly IAudioConversionSettings _audioSettings;
        private readonly IVideoConversionSettings _videoSettings;

        private string _output;
        private bool _hasInputBuilder = false;

        private ProcessPriorityClass? _priority = null;
        private MediaToolRunner _ffmpeg;
        private Func<string, string> _buildInputFileName = null;
        private Func<string, string> _buildOutputFileName = null;
        private Stream _inputPipeStream;

        private readonly bool _suppressGlobalOutputLimits;
        private readonly bool _suppressAutoHardwareAcceleration;
        private bool _manualHardwareAcceleration;
        private IProgress<ConversionProgressEventArgs> _progressReporter;
        private MediaAnalysisSession _analyticsSession;
        private Func<CancellationToken, Task> _onSuccessAsync;
        private Func<Task> _onFinallyAsync;
        private CancellationTokenSource _internalCts;

        public Conversion()
            : this(suppressGlobalOutputLimits: false, suppressAutoHardwareAcceleration: false)
        {
        }

        internal Conversion(bool suppressGlobalOutputLimits, bool suppressAutoHardwareAcceleration = false)
        {
            _suppressGlobalOutputLimits = suppressGlobalOutputLimits;
            _suppressAutoHardwareAcceleration = suppressAutoHardwareAcceleration;
            _userDefinedParameters[ParameterPosition.PostInput] = new List<string>();
            _userDefinedParameters[ParameterPosition.PreInput] = new List<string>();
            _audioSettings = new AudioConversionSettings(this);
            _videoSettings = new VideoConversionSettings(this);
        }

        internal IConversion AttachAnalyticsSession(MediaAnalysisSession analyticsSession)
        {
            _analyticsSession = analyticsSession;
            return this;
        }

        internal IConversion AttachLifecycleHandlers(Func<CancellationToken, Task> onSuccessAsync, Func<Task> onFinallyAsync)
        {
            _onSuccessAsync = onSuccessAsync;
            _onFinallyAsync = onFinallyAsync;
            return this;
        }

        /// <summary>
        ///     Собирает строку аргументов MediaOrchestrator, основываясь на заданных параметрах и потоках.
        /// </summary>
        /// <returns>Строка параметров для запуска процесса MediaOrchestrator.</returns>
        public string Build()
        {
            lock (_builderLock)
            {
                var builder = new StringBuilder();

                if (_buildOutputFileName == null)
                {
                    _buildOutputFileName = (number) => { return _output; };
                }

                ApplyAutoHardwareDecodeAcceleration();

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
                builder.Append(GetGlobalOutputLimitParameters());
                builder.Append(GetParameters(ParameterPosition.PostInput));
                builder.Append(string.Join(" ", _userDefinedParameters[ParameterPosition.PostInput].Select(x => x.Trim())) + " ");
                builder.Append(_buildOutputFileName("_%03d"));

                return builder.ToString();
            }
        }

        /// <summary>
        ///     Событие обновления прогресса MediaOrchestrator.
        /// </summary>
        public event ConversionProgressEventHandler OnProgress;

        /// <summary>
        ///     Событие, возникающее при выводе текста MediaOrchestrator.
        /// </summary>
        public event DataReceivedEventHandler OnDataReceived;

        /// <summary>
        ///     Событие, возникающее при получении видеоданных из pipe (требует PipeOutput()).
        /// </summary>
        public event VideoDataEventHandler OnVideoDataReceived;

        /// <inheritdoc />
        public IConversion SetProgressReporter(IProgress<ConversionProgressEventArgs> progressReporter)
        {
            _progressReporter = progressReporter;
            return this;
        }

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
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <param name="progress">Репортер прогресса на это выполнение; при null используется <see cref="SetProgressReporter"/>.</param>
        /// <returns>Результат конвертации.</returns>
        public Task<IConversionResult> Start(CancellationToken cancellationToken = default, IProgress<ConversionProgressEventArgs> progress = null)
        {
            return Start(Build(), cancellationToken, progress);
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (_ffmpeg == null)
            {
                throw new InvalidOperationException("Conversion has not been started.");
            }

            if (_internalCts != null)
            {
                _internalCts.Cancel();
            }
        }

        /// <summary>
        ///     Запускает MediaOrchestrator с заданными параметрами и токеном отмены.
        /// </summary>
        /// <param name="parameters">Строка параметров для MediaOrchestrator.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <param name="progress">Репортер прогресса на это выполнение; при null используется <see cref="SetProgressReporter"/>.</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversionResult> Start(string parameters, CancellationToken cancellationToken = default, IProgress<ConversionProgressEventArgs> progress = null)
        {
            if (_ffmpeg != null)
            {
                throw new InvalidOperationException(ErrorMessages.ConversionAlreadyStarted);
            }

            if (!cancellationToken.CanBeCanceled)
            {
                _internalCts = new CancellationTokenSource();
                cancellationToken = _internalCts.Token;
            }

            DateTime startTime = DateTime.Now;

            var reporter = progress ?? _progressReporter;
            ConversionProgressEventHandler forwardProgress = null;
            if (reporter != null)
            {
                forwardProgress = (_, args) => reporter.Report(args);
            }

            _ffmpeg = new MediaToolRunner();
            try
            {
                _ffmpeg.OnProgress += OnProgress;
                if (forwardProgress != null)
                {
                    _ffmpeg.OnProgress += forwardProgress;
                }

                _ffmpeg.OnDataReceived += OnDataReceived;
                _ffmpeg.OnVideoDataReceived += OnVideoDataReceived;
                CreateOutputDirectoryIfNotExists();
                try
                {
                    await _ffmpeg.RunProcess(parameters, cancellationToken, _priority, _inputPipeStream);
                    if (_onSuccessAsync != null)
                    {
                        await _onSuccessAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ShouldDeletePartialOutputOnFailure(ex))
                {
                    TryDeletePartialOutputFile();
                    await ReportAnalyticsExecutionAsync(parameters, startTime, DateTime.Now, succeeded: false, failureType: ex.GetType().FullName).ConfigureAwait(false);
                    throw;
                }
            }
            finally
            {
                if (forwardProgress != null)
                {
                    _ffmpeg.OnProgress -= forwardProgress;
                }

                _ffmpeg.OnProgress -= OnProgress;
                _ffmpeg.OnDataReceived -= OnDataReceived;
                _ffmpeg.OnVideoDataReceived -= OnVideoDataReceived;
                _ffmpeg = null;
                if (_onFinallyAsync != null)
                {
                    try
                    {
                        await _onFinallyAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }

                if (_internalCts != null)
                {
                    _internalCts.Dispose();
                    _internalCts = null;
                }
            }

            var result = new ConversionResult
            {
                StartTime = startTime,
                EndTime = DateTime.Now,
                Arguments = parameters
            };
            await ReportAnalyticsExecutionAsync(parameters, result.StartTime, result.EndTime, succeeded: true, failureType: null).ConfigureAwait(false);
            return result;
        }

        private async Task ReportAnalyticsExecutionAsync(
            string parameters,
            DateTime startTime,
            DateTime endTime,
            bool succeeded,
            string failureType)
        {
            if (_analyticsSession == null)
            {
                return;
            }

            try
            {
                await MediaOrchestrator.Analytics
                    .ReportExecutionAsync(_analyticsSession, startTime, endTime, parameters, succeeded, failureType, _ffmpeg?.LastExecutionResourceMetrics)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Analytics persistence must not break media processing.
            }
        }

        private void CreateOutputDirectoryIfNotExists()
        {
            if (OutputFilePath == null || OutputPipeDescriptor != null)
            {
                return;
            }

            var directoryPath = Path.GetDirectoryName(OutputFilePath.Unescape());
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new OutputDirectoryNotWritableException(string.Format(ErrorMessages.OutputDirectoryIsNotWritable, directoryPath), ex);
            }
            catch (IOException ex)
            {
                throw new OutputPathAccessDeniedException(string.Format(ErrorMessages.OutputPathAccessDenied, OutputFilePath.Unescape()), ex);
            }
        }

        private static bool ShouldDeletePartialOutputOnFailure(Exception ex)
        {
            return ex is OperationCanceledException || ex is ConversionException;
        }

        private void TryDeletePartialOutputFile()
        {
            if (OutputPipeDescriptor != null)
            {
                return;
            }

            var path = OutputFilePath?.Unescape();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            FileHelper.SafeDeleteFile(path);
        }

        /// <summary>
        ///     Указывает длительность анализа входного потока MediaOrchestrator.
        /// </summary>
        /// <param name="duration">Продолжительность анализа.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetAnalysisDuration(TimeSpan duration)
        {
            // MediaOrchestrator ожидает микросекунды (1 tick = 100 наносекунд, 10 ticks = 1 микросекунда)
            long microseconds = duration.Ticks / 10;

            _parameters.Add(new ConversionParameter(FFmpegHardwareAccelerationArguments.SetAnalysisDuration(microseconds), ParameterPosition.PostInput));
            return this;
        }


        /// <summary>
        ///     Добавляет произвольный параметр к команде MediaOrchestrator.
        /// </summary>
        /// <param name="parameter">Строка параметра.</param>
        /// <param name="parameterPosition">Позиция параметра относительно входных файлов.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion AddParameter(string parameter, ParameterPosition parameterPosition = ParameterPosition.PostInput)
        {
            _userDefinedParameters[parameterPosition].Add(parameter);
            return this;
        }

        /// <inheritdoc />
        public IConversion DisableVideo()
        {
            return AddParameter(FFmpegVideoArguments.DisableOutputFlag);
        }

        /// <inheritdoc />
        public IConversion MapAllStreams()
        {
            return AddParameter(FFmpegContainerArguments.MapAllStreamsValue);
        }

        /// <inheritdoc />
        public IConversion MapAudioStreams()
        {
            return AddParameter(FFmpegAudioArguments.MapStreamsValue);
        }

        /// <inheritdoc />
        public IConversion MapAudioStream(int inputIndex = 0, int audioStreamIndex = 0)
        {
            if (inputIndex < 0)
            {
                throw new StreamIndexOutOfRangeException(nameof(inputIndex), ErrorMessages.StreamIndexOutOfRange);
            }

            if (audioStreamIndex < 0)
            {
                throw new StreamIndexOutOfRangeException(nameof(audioStreamIndex), ErrorMessages.StreamIndexOutOfRange);
            }

            return AddParameter(FFmpegConversionArguments.MapAudioStream(inputIndex, audioStreamIndex));
        }

        /// <inheritdoc />
        public IConversion CopyAllCodecs()
        {
            return AddParameter(FFmpegContainerArguments.CopyAllCodecsValue);
        }

        /// <inheritdoc />
        public IConversion CopyAudioCodec()
        {
            return AddParameter(FFmpegAudioArguments.CopyCodecValue);
        }

        /// <inheritdoc />
        public IConversion SetAudioCodec(AudioCodec codec)
        {
            return AddParameter(FFmpegAudioArguments.SetCodec(codec));
        }

        /// <inheritdoc />
        public IConversion SetAudioSampleRate(int sampleRate)
        {
            return AddParameter(FFmpegAudioArguments.SetSampleRate(sampleRate));
        }

        /// <inheritdoc />
        public IConversion SetAudioChannels(int channels)
        {
            return AddParameter(FFmpegAudioArguments.SetChannels(channels));
        }

        /// <inheritdoc />
        public IConversion DisableSubtitles()
        {
            return AddParameter("-sn");
        }

        /// <inheritdoc />
        public IConversion EnableDeviceListing()
        {
            return AddParameter(FFmpegInputArguments.ListDevicesValue);
        }

        /// <inheritdoc />
        public IConversion AddInput(string inputPath)
        {
            return AddInput(InputSource.File(inputPath));
        }

        /// <inheritdoc />
        public IConversion AddInput(InputSource inputSource)
        {
            if (inputSource == null)
            {
                throw new ArgumentNullException(nameof(inputSource));
            }

            if (inputSource.Duration.HasValue)
            {
                AddParameter($"-t {inputSource.Duration.Value.ToFFmpeg()}", ParameterPosition.PreInput);
            }

            if (!string.IsNullOrWhiteSpace(inputSource.Format))
            {
                AddParameter(FFmpegInputArguments.SetInputFormat(inputSource.Format), ParameterPosition.PreInput);
            }

            return AddParameter(FFmpegInputArguments.AddInput(inputSource.Value), ParameterPosition.PreInput);
        }

        /// <inheritdoc />
        public IConversion AddLavfiInput(string filterGraph, TimeSpan? inputDuration = null)
        {
            return AddInput(InputSource.Lavfi(filterGraph, inputDuration));
        }

        /// <inheritdoc />
        [Obsolete("Use UseFilterGraph(FilterGraph) to avoid raw string filter graphs.", false)]
        public IConversion SetFilterComplex(string filterGraph)
        {
            return UseFilterGraph(new FilterGraph(filterGraph));
        }

        /// <inheritdoc />
        public IConversion UseFilterGraph(FilterGraph filterGraph)
        {
            if (filterGraph == null)
            {
                throw new ArgumentNullException(nameof(filterGraph));
            }

            return AddParameter($"-filter_complex \"{filterGraph.Expression}\"");
        }

        /// <inheritdoc />
        [Obsolete("Use MapFilterOutput(FilterLabel) to avoid raw string filter labels.", false)]
        public IConversion MapFilterOutput(string label)
        {
            return MapFilterOutput(FilterLabel.Parse(label));
        }

        /// <inheritdoc />
        public IConversion MapFilterOutput(FilterLabel label)
        {
            return AddParameter(FFmpegConversionArguments.MapFilterOutput(label));
        }

        /// <inheritdoc />
        public IConversion MapFilterOutputs(params FilterLabel[] labels)
        {
            if (labels == null)
            {
                throw new ArgumentNullException(nameof(labels));
            }

            foreach (var label in labels)
            {
                MapFilterOutput(label);
            }

            return this;
        }

        /// <inheritdoc />
        public IConversion SetAspectRatio(string ratio)
        {
            if (string.IsNullOrWhiteSpace(ratio))
            {
                throw new ArgumentException(ErrorMessages.AspectRatioMustBeProvided, nameof(ratio));
            }

            return AddParameter($"-aspect {ratio}");
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetHashFormat(hashFormat), ParameterPosition.PostInput));
            return this;
        }

        /// <summary>
        ///     Выбирает пресет MediaOrchestrator, влияющий на скорость и качество.
        /// </summary>
        /// <param name="preset">Предустановка кодирования.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetPreset(ConversionPreset preset)
        {
            _parameters.Add(new ConversionParameter(FFmpegEncodingArguments.SetPreset(preset), ParameterPosition.PostInput));
            return this;
        }

        /// <inheritdoc />
        public IConversion SetTune(ConversionTune tune)
        {
            _parameters.Add(new ConversionParameter(FFmpegEncodingArguments.SetTune(tune), ParameterPosition.PostInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetSeek(seek.Value), ParameterPosition.PostInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetDuration(time.Value), ParameterPosition.PreInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetDuration(time.Value), ParameterPosition.PostInput));
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetThreads(Math.Min(threads, 16))));
            return this;
        }

        /// <summary>
        ///     Указывает точное количество потоков MediaOrchestrator.
        /// </summary>
        /// <param name="threadsCount">Число нитей.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion UseMultiThread(int threadsCount)
        {
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetThreads(threadsCount)));
            return this;
        }

        /// <summary>
        ///     Устанавливает путь к выходному файлу.
        /// </summary>
        /// <param name="outputPath">Путь к файлу.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetOutput(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(outputPath));
            }

            OutputFilePath = new FileInfo(outputPath).FullName;
            _output = outputPath.Escape();
            return this;
        }

        /// <summary>
        ///     Перенаправляет вывод MediaOrchestrator в pipe.
        /// </summary>
        /// <param name="descriptor">Выбранный дескриптор pipe.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion PipeOutput(PipeDescriptor descriptor = PipeDescriptor.stdout)
        {
            SetOutput(FFmpegConversionArguments.PipeSpecifier(descriptor));
            OutputPipeDescriptor = descriptor;
            return this;
        }

        /// <summary>
        ///     Передаёт входные данные в MediaOrchestrator через pipe.
        /// </summary>
        /// <param name="inputStream">Поток, из которого читаются данные.</param>
        /// <param name="inputSpecifier">Спецификатор pipe (например, pipe:0).</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion PipeInput(Stream inputStream, string inputSpecifier = "pipe:0")
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException(ErrorMessages.StreamMustBeReadable, nameof(inputStream));
            }

            if (string.IsNullOrWhiteSpace(inputSpecifier))
            {
                throw new ArgumentException(ErrorMessages.InputSpecifierMustBeProvided, nameof(inputSpecifier));
            }

            if (_inputPipeStream != null)
            {
                throw new InvalidOperationException(ErrorMessages.InputPipeAlreadyConfigured);
            }

            _inputPipeStream = inputStream;
            _pipeInputs.Add(inputSpecifier);
            return this;
        }

        /// <summary>
        ///     Устанавливает битрейт для видеопотоков и соответствующие параметры.
        /// </summary>
        /// <param name="bitrate">Целевой битрейт.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetVideoBitrate(long bitrate)
        {
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetVideoBitrate(bitrate), ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetMinRate(bitrate), ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetMaxRate(bitrate), ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetBufferSize(bitrate), ParameterPosition.PostInput));

            if (HasH264Stream())
            {
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetX264OptionsForCbr(), ParameterPosition.PostInput));
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetAudioBitrate(bitrate), ParameterPosition.PostInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.UseShortest(), ParameterPosition.PostInput));
            }
            else
            {
                _parameters.Remove(new ConversionParameter(FFmpegExecutionArguments.UseShortest(), ParameterPosition.PostInput));
            }

            return this;
        }

        /// <summary>
        ///     Устанавливает приоритет запускаемого процесса MediaOrchestrator.
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SelectEveryNthFrame(frameNo), ParameterPosition.PostInput));
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SelectNthFrame(frameNo), ParameterPosition.PostInput));
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetStartNumber(startNumber), ParameterPosition.PreInput));
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
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetFrameRate(frameRate), ParameterPosition.PreInput));
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetOutputFrameRate(frameRate), ParameterPosition.PreInput));
            return this;
        }

        /// <summary>
        ///     Устанавливает частоту кадров выходного видео (-framerate и -r после входов).
        /// </summary>
        /// <param name="frameRate">Желаемая частота.</param>
        /// <returns>Текущий объект IConversion.</returns>
        public IConversion SetFrameRate(double frameRate)
        {
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetFrameRate(frameRate), ParameterPosition.PostInput));
            _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetOutputFrameRate(frameRate), ParameterPosition.PostInput));
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

            IEnumerable<IGrouping<FilterBlockType, IFilterConfiguration>> filterGroups = configurations.GroupBy(configuration => configuration.FilterBlockType);
            foreach (IGrouping<FilterBlockType, IFilterConfiguration> filterGroup in filterGroups)
            {
                builder.Append(FFmpegConversionArguments.MapFilterBlock(filterGroup.Key));
                var isFirstFilter = true;
                foreach (IFilterConfiguration configuration in filterGroup)
                {
                    foreach (KeyValuePair<string, string> filter in configuration.Filters)
                    {
                        if (!isFirstFilter)
                        {
                            builder.Append(";");
                        }

                        var map = FFmpegConversionArguments.MapFilterInput(configuration.StreamNumber);
                        var value = FFmpegConversionArguments.RenderNamedFilter(filter.Key, filter.Value);
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
                    builder.Append(FFmpegConversionArguments.MapPrimaryInputVideo());
                }

                foreach (var source in stream.GetSource())
                {
                    if (_hasInputBuilder)
                    {
                        // Если у нас есть построитель входных данных, нам нужно добавить единицу к индексу входного файла, чтобы учесть вход, созданный нашим построителем входных данных.
                        builder.Append(FFmpegConversionArguments.MapStream(_inputFileMap[source] + 1, stream.Index));
                    }
                    else
                    {
                        builder.Append(FFmpegConversionArguments.MapStream(_inputFileMap[source], stream.Index));
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
            _inputFileMap.Clear();
            foreach (var source in _pipeInputs)
            {
                _inputFileMap[source] = index++;
                builder.Append(FFmpegConversionArguments.AddEscapedInput(source));
            }

            foreach (var source in _streams.SelectMany(x => x.GetSource()).Distinct())
            {
                _inputFileMap[source] = index++;
                builder.Append(FFmpegConversionArguments.AddEscapedInput(source));
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

        internal static IConversion New(bool suppressGlobalOutputLimits = false, bool suppressAutoHardwareAcceleration = false)
        {
            var conversion = new Conversion(suppressGlobalOutputLimits, suppressAutoHardwareAcceleration);
            return conversion
                .SetOverwriteOutput(false);
        }

        private void ApplyAutoHardwareDecodeAcceleration()
        {
            if (_suppressAutoHardwareAcceleration)
            {
                return;
            }

            if (_manualHardwareAcceleration)
            {
                return;
            }

            if (!MediaOrchestrator.ApplyAutoHardwareAccelerationToConversions)
            {
                return;
            }

            var profile = MediaOrchestrator.AutoDetectedHardwareAccelerationProfile;
            if (profile == null)
            {
                return;
            }

            if (ConversionUsesStreamCopy())
            {
                return;
            }

            if (!HasExplicitVideoReencode())
            {
                return;
            }

            if (ConversionAlreadyHasHwaccel())
            {
                return;
            }

            if (!AllVideoOutputsAreH264HevcOrCompatibleHwEncoder())
            {
                return;
            }

            _parameters.Add(new ConversionParameter(FFmpegConversionArguments.SetHardwareAcceleration(profile.Hwaccel), ParameterPosition.PreInput));
            UseMultiThread(false);
        }

        private bool AllVideoOutputsAreH264HevcOrCompatibleHwEncoder()
        {
            var anyVideoEncode = false;
            foreach (IVideoStream stream in _streams.OfType<IVideoStream>())
            {
                if (!(stream is VideoStream vs) || vs.IsOutputCodecCopy)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(vs.SelectedOutputCodec))
                {
                    continue;
                }

                anyVideoEncode = true;
                if (!IsH264HevcOrHardwareEncoderFamily(vs.SelectedOutputCodec))
                {
                    return false;
                }
            }

            return anyVideoEncode;
        }

        private static bool IsH264HevcOrHardwareEncoderFamily(string encoder)
        {
            var c = encoder.ToLowerInvariant();
            return c.Contains("h264") || c.Contains("hevc") || c.Contains("x264") || c.Contains("x265") ||
                   c.Contains("nvenc") || c.Contains("qsv") || c.Contains("vaapi") || c.Contains("amf") ||
                   c.Contains("videotoolbox") || c.Contains("cuvid");
        }

        private bool ConversionAlreadyHasHwaccel()
        {
            foreach (ConversionParameter p in _parameters)
            {
                if (p.Position == ParameterPosition.PreInput && p.Parameter != null &&
                    p.Parameter.IndexOf("-hwaccel", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ConversionUsesStreamCopy()
        {
            foreach (string p in _userDefinedParameters[ParameterPosition.PostInput])
            {
                if (p != null &&
                    p.IndexOf("copy", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    p.IndexOf("-c", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            foreach (ConversionParameter p in _parameters)
            {
                if (p.Parameter != null &&
                    p.Parameter.IndexOf("copy", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    p.Parameter.IndexOf("-c", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasExplicitVideoReencode()
        {
            foreach (IVideoStream stream in _streams.OfType<IVideoStream>())
            {
                if (!(stream is VideoStream vs))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(vs.SelectedOutputCodec))
                {
                    continue;
                }

                if (vs.IsOutputCodecCopy)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private string GetGlobalOutputLimitParameters()
        {
            if (_suppressGlobalOutputLimits)
            {
                return string.Empty;
            }

            var maxFps = MediaOrchestrator.MaxOutputVideoFrameRate;
            var maxAr = MediaOrchestrator.MaxOutputAudioSampleRate;
            var maxAc = MediaOrchestrator.MaxOutputAudioChannels;
            if (!maxFps.HasValue && !maxAr.HasValue && !maxAc.HasValue)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var videos = _streams.OfType<IVideoStream>().ToList();
            var audios = _streams.OfType<IAudioStream>().ToList();

            if (maxFps is double capFps && capFps > 0 && videos.Count > 0)
            {
                var maxSrcFps = videos.Where(v => v.Framerate > 0.01).Select(v => v.Framerate).DefaultIfEmpty(capFps).Max();
                var targetFps = Math.Min(maxSrcFps, capFps);
                sb.Append($" {FFmpegVideoArguments.SetFrameRate(targetFps)} ");
            }

            if (maxAr is int capAr && capAr > 0 && audios.Count > 0)
            {
                var maxSrcRate = audios.Where(a => a.SampleRate > 0).Select(a => a.SampleRate).DefaultIfEmpty(capAr).Max();
                var targetAr = Math.Min(maxSrcRate, capAr);
                sb.Append($" {FFmpegAudioArguments.SetSampleRate(targetAr)} ");
            }

            if (maxAc is int capAc && capAc > 0 && audios.Count > 0)
            {
                var maxSrcCh = audios.Where(a => a.Channels > 0).Select(a => a.Channels).DefaultIfEmpty(capAc).Max();
                var targetAc = Math.Min(maxSrcCh, capAc);
                sb.Append($" {FFmpegAudioArguments.SetChannels(targetAc)} ");
            }

            return sb.ToString();
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
            _manualHardwareAcceleration = true;
            _parameters.Add(new ConversionParameter(FFmpegHardwareAccelerationArguments.SetHardwareAcceleration(hardwareAccelerator), ParameterPosition.PreInput));
            _parameters.Add(new ConversionParameter(FFmpegHardwareAccelerationArguments.SetVideoDecoder(decoder), ParameterPosition.PreInput));

            _parameters.Add(new ConversionParameter(FFmpegHardwareAccelerationArguments.SetVideoEncoder(encoder), ParameterPosition.PostInput));

            if (device != 0)
            {
                _parameters.Add(new ConversionParameter(FFmpegHardwareAccelerationArguments.SetHardwareAccelerationDevice(device), ParameterPosition.PreInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.OverwriteOutput(), ParameterPosition.PostInput));
                _parameters.Remove(new ConversionParameter($"-n", ParameterPosition.PostInput));
            }
            else
            {
                _parameters.Remove(new ConversionParameter($"-y", ParameterPosition.PostInput));
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.PreserveOutput(), ParameterPosition.PostInput));
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
                _parameters.Add(new ConversionParameter(FFmpegInputArguments.SetInputFormat(format), ParameterPosition.PreInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetOutputFormat(format), ParameterPosition.PostInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetPixelFormat(pixelFormat), ParameterPosition.PostInput));
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
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetVideoSyncAuto(), ParameterPosition.PostInput));
            }
            else
            {
                _parameters.Add(new ConversionParameter(FFmpegExecutionArguments.SetVideoSync(method), ParameterPosition.PostInput));
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
            stream.AddParameter(FFmpegExecutionArguments.SetFrameRate(framerate, 4), ParameterPosition.PreInput);
            stream.AddParameter(FFmpegExecutionArguments.SetDesktopOffsetX(xOffset), ParameterPosition.PreInput);
            stream.AddParameter(FFmpegExecutionArguments.SetDesktopOffsetY(yOffset), ParameterPosition.PreInput);

            if (videoSize != null)
            {
                stream.AddParameter(FFmpegExecutionArguments.SetDesktopVideoSize(videoSize), ParameterPosition.PreInput);
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

