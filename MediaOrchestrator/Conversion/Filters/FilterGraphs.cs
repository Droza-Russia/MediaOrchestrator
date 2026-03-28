using System;
using System.Linq;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Публичные фабрики типизированных графов <c>-filter_complex</c>.
    /// </summary>
    public static class FilterGraphs
    {
        /// <summary>
        ///     Создает граф сведения нескольких аудиовходов через фильтр <c>amix</c>.
        /// </summary>
        /// <param name="inputCount">Количество аудиовходов.</param>
        /// <param name="durationMode">Режим определения длительности выходного потока.</param>
        /// <param name="normalize">Включить нормализацию уровня в <c>amix</c>.</param>
        /// <param name="outputLabelName">Имя выходной метки фильтра.</param>
        /// <returns>Типизированный граф фильтра.</returns>
        public static FilterGraph BuildAudioMix(
            int inputCount,
            AudioMixDurationMode durationMode = AudioMixDurationMode.Longest,
            bool normalize = false,
            string outputLabelName = "mixed")
        {
            if (inputCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(inputCount), inputCount, "At least two audio inputs are required.");
            }

            var builder = new FFmpegFilterGraphBuilder();
            var inputs = Enumerable.Range(0, inputCount)
                .Select(index => FilterLabel.AudioInput(index))
                .ToArray();
            var output = FilterLabel.Named(outputLabelName);

            builder.AddSegment(
                inputs,
                new[] { output },
                FFmpegFilters.Amix(inputCount, durationMode, normalize));

            return new FilterGraph(builder.Build(), output);
        }
    }
}
