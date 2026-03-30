using System;

namespace MediaOrchestrator.Events
{
    /// <summary>
    ///     Enhanced progress reporter with cancellation request capability
    /// </summary>
    public interface IConversionProgressReporter
    {
        /// <summary>
        ///     Report progress
        /// </summary>
        void Report(ConversionProgressEventArgs args);

        /// <summary>
        ///     Request cancellation of the conversion
        /// </summary>
        void RequestCancellation();
    }

    /// <summary>
    ///     Adapter from IProgress to IConversionProgressReporter
    /// </summary>
    public sealed class ConversionProgressAdapter : IConversionProgressReporter
    {
        private readonly IProgress<ConversionProgressEventArgs> _progress;
        private readonly Action _onCancellationRequested;
        private bool _cancellationRequested;

        public ConversionProgressAdapter(IProgress<ConversionProgressEventArgs> progress, Action onCancellationRequested = null)
        {
            _progress = progress;
            _onCancellationRequested = onCancellationRequested;
            _cancellationRequested = false;
        }

        public void Report(ConversionProgressEventArgs args)
        {
            _progress?.Report(args);
        }

        public void RequestCancellation()
        {
            if (!_cancellationRequested)
            {
                _cancellationRequested = true;
                _onCancellationRequested?.Invoke();
            }
        }

        public bool IsCancellationRequested => _cancellationRequested;
    }
}