using System;

namespace MediaOrchestrator.Events
{
    /// <summary>
    ///     Info about conversion progress
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="args">Conversion info</param>
    public delegate void ConversionProgressEventHandler(object sender, ConversionProgressEventArgs args);

    /// <summary>
    ///     Conversion information with enhanced metrics
    /// </summary>
    public sealed class ConversionProgressEventArgs : EventArgs
    {
        /// <summary>
        ///     Basic constructor for backward compatibility
        /// </summary>
        public ConversionProgressEventArgs(TimeSpan duration, TimeSpan totalLength, int processId)
            : this(duration, totalLength, processId, null, null, 1.0)
        {
        }

        /// <summary>
        ///     Full constructor with enhanced metrics
        /// </summary>
        public ConversionProgressEventArgs(
            TimeSpan duration,
            TimeSpan totalLength,
            int processId,
            TimeSpan? estimatedTimeRemaining,
            double? speedMultiplier,
            double? currentBitrateMbps)
        {
            Duration = duration;
            TotalLength = totalLength;
            ProcessId = processId;
            EstimatedTimeRemaining = estimatedTimeRemaining;
            SpeedMultiplier = speedMultiplier;
            CurrentBitrateMbps = currentBitrateMbps;
        }

        /// <summary>
        ///     Current processing time
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        ///     Input movie length
        /// </summary>
        public TimeSpan TotalLength { get; }

        /// <summary>
        ///     Process id
        /// </summary>
        public long ProcessId { get; }

        /// <summary>
        ///     Estimated time remaining until completion
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; }

        /// <summary>
        ///     Current speed multiplier (e.g., 1.0 = realtime, 2.0 = 2x speed)
        /// </summary>
        public double? SpeedMultiplier { get; }

        /// <summary>
        ///     Current video bitrate in Mbps (if available)
        /// </summary>
        public double? CurrentBitrateMbps { get; }

        /// <summary>
        ///     Доля выполнения относительно оценки полной длины (0–100). При неизвестной длительности — 0.
        /// </summary>
        public int Percent
        {
            get
            {
                if (TotalLength.TotalSeconds <= 0 || Duration.TotalSeconds <= 0)
                {
                    return 0;
                }

                var ratio = Duration.TotalSeconds / TotalLength.TotalSeconds;
                return (int)(Math.Round(ratio, 2) * 100);
            }
        }

        /// <summary>
        ///     Indicates whether progress information is available
        /// </summary>
        public bool HasProgress => TotalLength.TotalSeconds > 0 && Duration.TotalSeconds > 0;

        /// <summary>
        ///     Human-readable progress string
        /// </summary>
        public string ProgressString
        {
            get
            {
                var percentStr = HasProgress ? $"{Percent}%" : "?%";
                var durationStr = FormatTimeSpan(Duration);
                var totalStr = FormatTimeSpan(TotalLength);

                if (EstimatedTimeRemaining.HasValue)
                {
                    var etaStr = FormatTimeSpan(EstimatedTimeRemaining.Value);
                    return $"{percentStr} ({durationStr}/{totalStr}) ETA:{etaStr}";
                }

                return $"{percentStr} ({durationStr}/{totalStr})";
            }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
            {
                return ts.ToString(@"h\:mm\:ss");
            }
            return ts.ToString(@"mm\:ss");
        }
    }
}