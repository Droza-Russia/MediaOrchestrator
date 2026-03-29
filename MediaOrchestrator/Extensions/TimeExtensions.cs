using System;
using System.Globalization;
using System.Linq;

namespace MediaOrchestrator.Extensions
{
    /// <summary>
    ///     Extension methods
    /// </summary>
    public static class TimeExtensions
    {
        /// <summary>
        ///     Return ffmpeg formatted time
        /// </summary>
        /// <param name="ts">TimeSpan</param>
        /// <returns>MediaOrchestrator formated time</returns>
        public static string ToFFmpeg(this TimeSpan ts)
        {
            var milliseconds = ts.Milliseconds;
            var seconds = ts.Seconds;
            var minutes = ts.Minutes;
            var hours = (int)ts.TotalHours;

            return $"{hours:D}:{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
        }

        /// <summary>
        ///     Parse MediaOrchestrator formated time
        /// </summary>
        /// <param name="text">MediaOrchestrator time</param>
        /// <returns>TimeSpan</returns>
        public static TimeSpan ParseFFmpegTime(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return TimeSpan.Zero;
            }

            string[] parts;
            try
            {
                parts = text.Split(':').Reverse().ToArray();
            }
            catch
            {
                return TimeSpan.Zero;
            }

            if (parts.Length < 3)
            {
                return TimeSpan.Zero;
            }

            var milliseconds = 0;
            int seconds;
            var invariant = CultureInfo.InvariantCulture;
            try
            {
                if (parts[0].Contains('.'))
                {
                    var secondsSplit = parts[0].Split('.');
                    seconds = int.Parse(secondsSplit[0], invariant);
                    milliseconds = int.Parse(secondsSplit[1], invariant);
                }
                else
                {
                    seconds = int.Parse(parts[0], invariant);
                }

                var minutes = int.Parse(parts[1], invariant);
                var hours = int.Parse(parts[2], invariant);

                return new TimeSpan(0, hours, minutes, seconds, milliseconds);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
    }
}
