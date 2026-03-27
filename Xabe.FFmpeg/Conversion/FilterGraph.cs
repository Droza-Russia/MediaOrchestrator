using System;
using System.Collections.Generic;

namespace Xabe.FFmpeg
{
    public sealed class FilterGraph
    {
        public FilterGraph(string expression, params FilterLabel[] outputs)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Filter graph expression must be provided.", nameof(expression));
            }

            Expression = expression;
            Outputs = outputs ?? Array.Empty<FilterLabel>();
        }

        public string Expression { get; }

        public IReadOnlyList<FilterLabel> Outputs { get; }
    }
}
