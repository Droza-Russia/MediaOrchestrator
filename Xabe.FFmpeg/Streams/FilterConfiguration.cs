using System.Collections.Generic;
using System.Linq;

namespace Xabe.FFmpeg
{
    /// <inheritdoc />
    internal class FilterConfiguration : IFilterConfiguration
    {
        /// <inheritdoc />
        public string FilterType => FilterBlockType.ToArgumentName();

        /// <inheritdoc />
        public FilterBlockType FilterBlockType { get; set; }

        /// <inheritdoc />
        public int StreamNumber { get; set; }

        /// <inheritdoc />
        public Dictionary<string, string> Filters => TypedFilters.ToDictionary(x => x.Key.ToArgumentName(), x => x.Value);

        internal Dictionary<StreamFilterName, string> TypedFilters { get; set; } = new Dictionary<StreamFilterName, string>();
    }
}
