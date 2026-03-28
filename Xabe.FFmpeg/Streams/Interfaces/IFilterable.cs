using System.Collections.Generic;

namespace MediaOrchestrator
{
    internal interface IFilterable
    {
        IEnumerable<IFilterConfiguration> GetFilters();
    }
}
