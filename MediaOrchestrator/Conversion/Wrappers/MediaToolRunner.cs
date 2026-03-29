using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using MediaOrchestrator.Events;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    // ReSharper disable once InconsistentNaming

    /// <inheritdoc />
    /// <summary>
    ///     Обертка для MediaOrchestrator
    /// </summary>
    internal class MediaToolRunner : MediaOrchestrator
    {
        private const string TIME_FORMAT_PATTERN = @"\w\w:\w\w:\w\w";
        private static readonly Regex _timeFormatRegex = new Regex(TIME_FORMAT_PATTERN, RegexOptions.Compiled);
        private List<string> _outputLog;
        private TimeSpan _totalTime;
        private bool _wasKilled = false;
        private DateTime _progressStartTime;
        private double _lastProgressDurationSeconds;
        internal Analytics.Models.ExecutionResourceMetrics LastExecutionResourceMetrics { get; private set; }

        /// <summary>
        ///     Срабатывает при изменении прогресса MediaOrchestrator
        /// </summary>
        internal event ConversionProgressEventHandler OnProgress;

        /// <summary>
        ///     Срабатывает, когда процесс MediaOrchestrator выводит что-либо
        /// </summary>
        internal event DataReceivedEventHandler OnDataReceived;

        /// <summary>
        ///     Срабатывает, когда процесс MediaOrchestrator записывает видеоданные в stdout
        /// </summary>
        internal event VideoDataEventHandler OnVideoDataReceived;

        internal Task<bool> RunProcess(
            string args,
            CancellationToken cancellationToken,
            ProcessPriorityClass? priority,
            Stream inputStream = null)
        {
            return Task.Factory.StartNew(() =>
            {
                _outputLog = new List<string>();
                var pipedOutput = OnVideoDataReceived != null;
                var process = RunProcess(args, FFmpegPath, priority, inputStream != null, pipedOutput, true);
                var resourceCollector = new ProcessResourceTelemetryCollector(process, ResolveHardwareAccelerator(args));
                Task inputCopyTask = null;
                if (inputStream != null)
                {
                    inputCopyTask = Task.Run(async () =>
                    {
                        try
                        {
                            await inputStream.CopyToAsync(process.StandardInput.BaseStream, 81920, cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            try
                            {
                                process.StandardInput.Close();
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }, cancellationToken);
                }
                var processId = process.Id;
                using (process)
                {
                    process.ErrorDataReceived += (sender, e) => ProcessOutputData(e, args, processId);
                    process.BeginErrorReadLine();
                    if (pipedOutput)
                    {
                        Task.Run(() => ProcessVideoData(process, cancellationToken), cancellationToken);
                    }

                    var ctr = cancellationToken.Register(async () =>
                    {
                        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                        {
                            try
                            {
                                process.StandardInput.Write("q");
                                await Task.Delay(1000 * 5);

                                if (!process.HasExited)
                                {
                                    process.CloseMainWindow();
                                    process.Kill();
                                    _wasKilled = true;
                                }
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    });

                    using (ctr)
                    {
                        using (var processEnded = new ManualResetEvent(false))
                        {
                            processEnded.SetSafeWaitHandle(new SafeWaitHandle(process.Handle, false));
                            var index = WaitHandle.WaitAny(new[] { processEnded, cancellationToken.WaitHandle });

                            // Если сигнал пришел от токена отмены вызывающей стороны, закрываем окно
                            if (index == 1
                                && !process.HasExited)
                            {
                                process.CloseMainWindow();
                                process.Kill();
                                _wasKilled = true;
                            }
                            else if (index == 0 && !process.HasExited)
                            {
                                // Обходной путь для Linux: https://github.com/dotnet/corefx/issues/35544
                                process.WaitForExit();
                            }
                        }

                        EnsureProcessFullyExited(process);
                        LastExecutionResourceMetrics = resourceCollector.Complete();
                        inputCopyTask?.GetAwaiter().GetResult();

                        cancellationToken.ThrowIfCancellationRequested();
                        if (_wasKilled)
                        {
                            throw new ConversionException(ErrorMessages.FailedToStopProcess, args);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return false;
                        }

                        var output = string.Join(Environment.NewLine, _outputLog.ToArray());
                        var exceptionsCatcher = new MediaToolExceptionCatcher();
                        exceptionsCatcher.CatchFFmpegErrors(output, args);

                        if (process.ExitCode != 0 && _outputLog.Any() && !_outputLog.Last().Contains("dummy"))
                        {
                            if (MediaToolExceptionCatcher.OutputIndicatesInsufficientDiskSpace(output))
                            {
                                throw new InsufficientDiskSpaceException(ErrorMessages.InsufficientDiskSpace, output, args);
                            }

                            throw new ConversionException(output, args);
                        }
                    }
                }

                return true;
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <summary>
        ///     Дожидается завершения процесса после Kill, чтобы файлы и pipe не оставались заблокированными.
        /// </summary>
        private static void EnsureProcessFullyExited(Process process)
        {
            if (process.HasExited)
            {
                return;
            }

            try
            {
                process.WaitForExit(120_000);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ProcessOutputData(DataReceivedEventArgs e, string args, int processId)
        {
            if (e.Data == null)
            {
                return;
            }

            OnDataReceived?.Invoke(this, e);

            _outputLog.Add(e.Data);

            if (OnProgress == null)
            {
                return;
            }

            CalculateTime(e, args, processId);
        }

        private void ProcessVideoData(Process process, CancellationToken cancellationToken)
        {
            var br = new BinaryReader(process.StandardOutput.BaseStream);
            byte[] buffer;

            while ((buffer = br.ReadBytes(4096)).Length > 0)
            {
                var args = new VideoDataEventArgs(buffer);
                OnVideoDataReceived?.Invoke(this, args);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void CalculateTime(DataReceivedEventArgs e, string args, int processId)
        {
            if (e.Data.Contains("Duration: N/A"))
            {
                return;
            }

            if (e.Data.Contains("Duration"))
            {
                GetDuration(e, _timeFormatRegex, args);
            }
            else if (e.Data.Contains("size"))
            {
                Match match = _timeFormatRegex.Match(e.Data);
                var ts = GetTimeSpanValue(match);
                if (ts.TotalMilliseconds > 0)
                {
                    var progressArgs = CreateEnhancedProgressArgs(ts, _totalTime, processId, e.Data);
                    OnProgress(this, progressArgs);
                }
            }
        }

        private void GetDuration(DataReceivedEventArgs e, Regex regex, string args)
        {
            var t = GetArgumentValue("-t", args);
            if (!string.IsNullOrWhiteSpace(t) && t != "1")
            {
                _totalTime = TimeSpan.Parse(t);
                return;
            }

            Match match = regex.Match(e.Data);
            if (!match.Success)
            {
                return;
            }

            _totalTime = _totalTime.Add(TimeSpan.Parse(match.Value));

            var ss = GetArgumentValue("-ss", args);
            if (!string.IsNullOrWhiteSpace(ss))
            {
                _totalTime -= TimeSpan.Parse(ss);
            }
        }

        private string GetArgumentValue(string option, string args)
        {
            var words = args.Split(' ')
                                     .ToList();
            var index = words.IndexOf(option);
            if (index >= 0)
            {
                return words[index + 1];
            }

            return string.Empty;
        }

        private TimeSpan GetTimeSpanValue(Match match)
        {
            if (match.Success)
            {
                var ists = TimeSpan.TryParse(match.Value, out var outts);
                if (ists)
                {
                    return outts;
                }
                else
                {
                    return GetTimeSpanValue(match.NextMatch());
                }
            }
            else
            {
                return new TimeSpan(0, 0, 0);
            }
        }

        private static string ResolveHardwareAccelerator(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                return string.Empty;
            }

            if (args.IndexOf("cuda", StringComparison.OrdinalIgnoreCase) >= 0 ||
                args.IndexOf("_nvenc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                args.IndexOf("_cuvid", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "cuda";
            }

            if (args.IndexOf("_qsv", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "qsv";
            }

            if (args.IndexOf("_vaapi", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "vaapi";
            }

            if (args.IndexOf("_videotoolbox", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "videotoolbox";
            }

            if (args.IndexOf("_amf", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "amf";
            }

            return string.Empty;
        }

        private ConversionProgressEventArgs CreateEnhancedProgressArgs(TimeSpan duration, TimeSpan totalLength, int processId, string ffmpegOutputLine)
        {
            if (_progressStartTime == DateTime.MinValue)
            {
                _progressStartTime = DateTime.UtcNow;
                _lastProgressDurationSeconds = 0;
            }

            TimeSpan? estimatedTimeRemaining = null;
            double? speedMultiplier = null;
            double? currentBitrateMbps = null;

            if (duration.TotalSeconds > 0 && _lastProgressDurationSeconds > 0)
            {
                var progressDelta = duration.TotalSeconds - _lastProgressDurationSeconds;
                var timeDelta = (DateTime.UtcNow - _progressStartTime).TotalSeconds;

                if (progressDelta > 0 && timeDelta > 0)
                {
                    speedMultiplier = progressDelta / timeDelta;

                    if (speedMultiplier.HasValue && speedMultiplier.Value > 0.1)
                    {
                        var remainingDuration = totalLength.TotalSeconds - duration.TotalSeconds;
                        if (remainingDuration > 0)
                        {
                            var etaSeconds = remainingDuration / speedMultiplier.Value;
                            estimatedTimeRemaining = TimeSpan.FromSeconds(etaSeconds);
                        }
                    }
                }
            }

            _lastProgressDurationSeconds = duration.TotalSeconds;

            if (totalLength.TotalSeconds > 0)
            {
                currentBitrateMbps = ParseBitrateFromOutput(ffmpegOutputLine);
            }

            return new ConversionProgressEventArgs(
                duration,
                totalLength,
                processId,
                estimatedTimeRemaining,
                speedMultiplier,
                currentBitrateMbps);
        }

        private double? ParseBitrateFromOutput(string outputLine)
        {
            if (string.IsNullOrWhiteSpace(outputLine))
            {
                return null;
            }

            try
            {
                var bitrateMatch = System.Text.RegularExpressions.Regex.Match(
                    outputLine,
                    @"bitrate\s*=\s*(\d+\.?\d*)\s*(\w+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (bitrateMatch.Success && bitrateMatch.Groups.Count >= 3)
                {
                    var value = double.Parse(bitrateMatch.Groups[1].Value);
                    var unit = bitrateMatch.Groups[2].Value.ToUpperInvariant();

                    switch (unit)
                    {
                        case "MB/S":
                        case "MBPS":
                            return value;
                        case "KB/S":
                        case "KBPS":
                            return value / 1000.0;
                        case "B/S":
                        case "BPS":
                            return value / 1000000.0;
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
