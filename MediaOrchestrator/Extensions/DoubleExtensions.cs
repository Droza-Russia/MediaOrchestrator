using System;
using System.Globalization;

namespace MediaOrchestrator.Extensions
{
    public static class DoubleExtensions
    {
        public static string ToFFmpegFormat(this double number, int decimalPlaces = 1)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                number = 0;
            }

            decimalPlaces = Math.Max(0, Math.Min(10, decimalPlaces));
            return string.Format(CultureInfo.GetCultureInfo("en-US"), $"{{0:N{decimalPlaces}}}", number);
        }
    }
}
