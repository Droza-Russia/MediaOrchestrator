using System.Collections.Generic;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Конфигурация фильтра потока.
    /// </summary>
    public interface IFilterConfiguration
    {
        /// <summary>
        ///     Тип фильтра.
        /// </summary>
        string FilterType { get; }

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