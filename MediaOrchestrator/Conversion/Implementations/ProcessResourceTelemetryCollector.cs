using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator
{
    internal sealed class ProcessResourceTelemetryCollector
    {
        private readonly Process _process;
        private readonly string _hardwareAccelerator;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _samplingTask;
        private readonly int _processorCount;
        private readonly object _sync = new object();

        private long _peakWorkingSetBytes;
        private double _totalCpuUsagePercent;
        private int _cpuSamples;
        private double _peakCpuUsagePercent;
        private double _totalAcceleratorUsagePercent;
        private int _acceleratorSamples;
        private double _peakAcceleratorUsagePercent;

        internal ProcessResourceTelemetryCollector(Process process, string hardwareAccelerator)
        {
            _process = process;
            _hardwareAccelerator = hardwareAccelerator ?? string.Empty;
            _processorCount = Math.Max(1, Environment.ProcessorCount);
            _samplingTask = Task.Run(() => SampleLoopAsync(_cancellationTokenSource.Token));
        }

        internal ExecutionResourceMetrics Complete()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                _samplingTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }

            lock (_sync)
            {
                return new ExecutionResourceMetrics
                {
                    PeakWorkingSetBytes = _peakWorkingSetBytes,
                    AverageCpuUsagePercent = _cpuSamples == 0 ? 0 : _totalCpuUsagePercent / _cpuSamples,
                    PeakCpuUsagePercent = _peakCpuUsagePercent,
                    LogicalCoreCount = _processorCount,
                    AverageAcceleratorUsagePercent = _acceleratorSamples == 0 ? 0 : _totalAcceleratorUsagePercent / _acceleratorSamples,
                    PeakAcceleratorUsagePercent = _peakAcceleratorUsagePercent
                };
            }
        }

        private async Task SampleLoopAsync(CancellationToken cancellationToken)
        {
            TimeSpan previousTotalProcessorTime = TimeSpan.Zero;
            DateTime previousTimestampUtc = DateTime.UtcNow;
            bool hasPreviousCpuSnapshot = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                bool shouldContinue = CaptureSample(ref previousTotalProcessorTime, ref previousTimestampUtc, ref hasPreviousCpuSnapshot);
                if (!shouldContinue)
                {
                    break;
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            CaptureSample(ref previousTotalProcessorTime, ref previousTimestampUtc, ref hasPreviousCpuSnapshot);
        }

        private bool CaptureSample(ref TimeSpan previousTotalProcessorTime, ref DateTime previousTimestampUtc, ref bool hasPreviousCpuSnapshot)
        {
            try
            {
                if (_process == null)
                {
                    return false;
                }

                _process.Refresh();
            }
            catch
            {
                return false;
            }

            long workingSetBytes = 0;
            TimeSpan totalProcessorTime = TimeSpan.Zero;
            try
            {
                workingSetBytes = _process.WorkingSet64;
                totalProcessorTime = _process.TotalProcessorTime;
            }
            catch
            {
            }

            double cpuUsagePercent = 0;
            var nowUtc = DateTime.UtcNow;
            if (hasPreviousCpuSnapshot)
            {
                double elapsedMilliseconds = (nowUtc - previousTimestampUtc).TotalMilliseconds;
                if (elapsedMilliseconds > 0)
                {
                    cpuUsagePercent = Math.Max(0, (totalProcessorTime - previousTotalProcessorTime).TotalMilliseconds / (elapsedMilliseconds * _processorCount) * 100d);
                }
            }

            double acceleratorUsagePercent = 0;
            try
            {
                var usage = MediaOrchestrator.HardwareAcceleratorLoadProvider?.Invoke(_hardwareAccelerator, _process.Id);
                if (usage.HasValue)
                {
                    acceleratorUsagePercent = Math.Max(0, usage.Value);
                }
            }
            catch
            {
            }

            lock (_sync)
            {
                _peakWorkingSetBytes = Math.Max(_peakWorkingSetBytes, workingSetBytes);
                if (cpuUsagePercent > 0)
                {
                    _totalCpuUsagePercent += cpuUsagePercent;
                    _cpuSamples++;
                    _peakCpuUsagePercent = Math.Max(_peakCpuUsagePercent, cpuUsagePercent);
                }

                if (acceleratorUsagePercent > 0)
                {
                    _totalAcceleratorUsagePercent += acceleratorUsagePercent;
                    _acceleratorSamples++;
                    _peakAcceleratorUsagePercent = Math.Max(_peakAcceleratorUsagePercent, acceleratorUsagePercent);
                }
            }

            previousTotalProcessorTime = totalProcessorTime;
            previousTimestampUtc = nowUtc;
            hasPreviousCpuSnapshot = true;

            try
            {
                return !_process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}
