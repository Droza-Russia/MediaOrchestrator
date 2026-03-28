using System;
using System.Collections.Generic;

namespace MediaOrchestrator
{
    public sealed class FilterGraph
    {
        public FilterGraph(string expression, params FilterLabel[] outputs)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException(ErrorMessages.FilterGraphExpressionMustBeProvided, nameof(expression));
            }

            Expression = expression;
            Outputs = outputs ?? Array.Empty<FilterLabel>();
        }

        public string Expression { get; }

        public IReadOnlyList<FilterLabel> Outputs { get; }
    }
}
