using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xabe.FFmpeg
{
    internal sealed class FFmpegFilterGraphBuilder
    {
        private readonly List<string> _segments = new List<string>();

        internal FFmpegFilterGraphBuilder Add(string segment)
        {
            if (!string.IsNullOrWhiteSpace(segment))
            {
                _segments.Add(segment.Trim());
            }

            return this;
        }

        internal FFmpegFilterGraphBuilder AddRange(IEnumerable<string> segments)
        {
            if (segments == null)
            {
                return this;
            }

            foreach (var segment in segments)
            {
                Add(segment);
            }

            return this;
        }

        internal FFmpegFilterGraphBuilder AddChain(FilterLabel input, FilterLabel output, params FFmpegFilter[] filters)
        {
            return AddSegment(new[] { input }, new[] { output }, filters);
        }

        internal FFmpegFilterGraphBuilder AddSegment(
            IEnumerable<FilterLabel> inputs,
            IEnumerable<FilterLabel> outputs,
            params FFmpegFilter[] filters)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            if (outputs == null)
            {
                throw new ArgumentNullException(nameof(outputs));
            }

            if (filters == null || filters.Length == 0)
            {
                throw new ArgumentException("At least one filter must be provided.", nameof(filters));
            }

            var inputList = inputs.ToArray();
            var outputList = outputs.ToArray();
            var expression = string.Concat(inputList.Select(label => label.AsReference())) +
                             string.Join(",", filters.Select(filter => filter.Render()));

            if (outputList.Any())
            {
                expression += " " + string.Join(" ", outputList.Select(label => label.AsReference()));
            }

            return Add(expression);
        }

        internal string Build()
        {
            return string.Join("; ", _segments);
        }
    }
}
