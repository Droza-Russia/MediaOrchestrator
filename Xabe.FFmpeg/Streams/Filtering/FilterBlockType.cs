using System;

namespace Xabe.FFmpeg
{
    internal enum FilterBlockType
    {
        AudioFilter,
        ComplexFilter
    }

    internal static class FilterBlockTypeExtensions
    {
        internal static string ToArgumentName(this FilterBlockType filterBlockType)
        {
            switch (filterBlockType)
            {
                case FilterBlockType.AudioFilter:
                    return "-filter:a";
                case FilterBlockType.ComplexFilter:
                    return "-filter_complex";
                default:
                    throw new ArgumentOutOfRangeException(nameof(filterBlockType), filterBlockType, null);
            }
        }
    }
}
