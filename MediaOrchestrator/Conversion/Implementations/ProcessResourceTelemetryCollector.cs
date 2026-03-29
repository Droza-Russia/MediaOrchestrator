using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator
{
    internal sealed class ProcessResourceTelemetryCollector : IDisposable
    {
        private readonly Process _process;
        private readonly string _hardwareAccelerator;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly int _processorCount;
        private readonly Task _samplingTask;
        private volatile bool _isDisposed;

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
            if (_isDisposed)
            {
                return CreateEmptyMetrics();
            }

            _cancellationTokenSource.Cancel();
            try
            {
                _samplingTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                Trace.TraceWarning("ProcessResourceTelemetryCollector stop cancelled");
            }
            catch (Exception ex)
            {
                Trace.TraceError("ProcessResourceTelemetryCollector stop failed: {0}", ex.Message);
            }

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

        private ExecutionResourceMetrics CreateEmptyMetrics()
        {
            return new ExecutionResourceMetrics
            {
                PeakWorkingSetBytes = 0,
                AverageCpuUsagePercent = 0,
                PeakCpuUsagePercent = 0,
                LogicalCoreCount = _processorCount,
                AverageAcceleratorUsagePercent = 0,
                PeakAcceleratorUsagePercent = 0
            };
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
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

            if (_isDisposed)
            {
                return false;
            }

            InterlockedOperations.UpdateIfGreater(ref _peakWorkingSetBytes, workingSetBytes);
            if (cpuUsagePercent > 0)
            {
                InterlockedOperations.BeginUpdate(ref _totalCpuUsagePercent, ref _cpuSamples, cpuUsagePercent, out var newTotal, out var newSamples);
                _totalCpuUsagePercent = newTotal;
                _cpuSamples = newSamples;
                InterlockedOperations.UpdateIfGreater(ref _peakCpuUsagePercent, cpuUsagePercent);
            }

            if (acceleratorUsagePercent > 0)
            {
                InterlockedOperations.BeginUpdate(ref _totalAcceleratorUsagePercent, ref _acceleratorSamples, acceleratorUsagePercent, out var newTotal, out var newSamples);
                _totalAcceleratorUsagePercent = newTotal;
                _acceleratorSamples = newSamples;
                InterlockedOperations.UpdateIfGreater(ref _peakAcceleratorUsagePercent, acceleratorUsagePercent);
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
