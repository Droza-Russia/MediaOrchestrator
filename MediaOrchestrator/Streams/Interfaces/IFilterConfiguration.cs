using System.Collections.Generic;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Конфигурация фильтра потока.
    /// </summary>
    internal interface IFilterConfiguration
    {
        /// <summary>
        ///     Тип фильтра.
        /// </summary>
        string FilterType { get; }

        /// <summary>
        ///     Тип блока фильтра в типизированном виде.
        /// </summary>
        FilterBlockType FilterBlockType { get; }

        /// <summary>
        ///     Номер потока для фильтрации.
        /// </summary>
        int StreamNumber { get; }

        /// <summary>
        ///     Фильтр с именами параметров и их значениями.
        /// </summary>
        Dictionary<string, string> Filters { get; }
    }
}
