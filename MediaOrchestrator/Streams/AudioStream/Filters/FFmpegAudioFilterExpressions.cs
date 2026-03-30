using System;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    internal static class FFmpegAudioFilterExpressions
    {
        internal static string Reverse()
        {
            return "areverse";
        }

        internal static string ChangeTempo(double multiplication)
        {
            if (multiplication < 0.5 || multiplication > 2.0)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplication), ErrorMessages.SpeedOutOfRange);
            }

            return multiplication.ToFFmpegFormat();
        }
    }
}
