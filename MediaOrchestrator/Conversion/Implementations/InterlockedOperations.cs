using System;
using System.Threading;

namespace MediaOrchestrator
{
    internal static class InterlockedOperations
    {
        public static void UpdateIfGreater(ref long location, long value)
        {
            long current, newValue;
            do
            {
                current = location;
                if (value <= current)
                {
                    return;
                }
                newValue = value;
            }
            while (Interlocked.CompareExchange(ref location, newValue, current) != current);
        }

        public static void UpdateIfGreater(ref double location, double value)
        {
            double current;

            do
            {
                current = location;
                if (value <= current)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref location, value, current) != current);
        }

        public static void BeginUpdate(
            ref double totalRef,
            ref int samplesRef,
            double newValue,
            out double newTotal,
            out int newSamples)
        {
            int currentSamples;
            double currentTotal;
            double tmpTotal;
            int tmpSamples;

            do
            {
                currentTotal = totalRef;
                currentSamples = samplesRef;
                tmpTotal = currentTotal + newValue;
                tmpSamples = currentSamples + 1;
            }
            while (
                Interlocked.CompareExchange(ref totalRef, tmpTotal, currentTotal) != currentTotal ||
                Interlocked.CompareExchange(ref samplesRef, tmpSamples, currentSamples) != currentSamples);

            newTotal = tmpTotal;
            newSamples = tmpSamples;
        }
    }
}