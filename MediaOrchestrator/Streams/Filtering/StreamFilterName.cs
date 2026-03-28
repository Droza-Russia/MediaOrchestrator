using System;

namespace MediaOrchestrator
{
    internal enum StreamFilterName
    {
        Atempo,
        DrawText,
        Overlay,
        SetPts,
        Subtitles
    }

    internal static class StreamFilterNameExtensions
    {
        internal static string ToArgumentName(this StreamFilterName filterName)
        {
            switch (filterName)
            {
                case StreamFilterName.Atempo:
                    return "atempo";
                case StreamFilterName.DrawText:
                    return "drawtext";
                case StreamFilterName.Overlay:
                    return "overlay";
                case StreamFilterName.SetPts:
                    return "setpts";
                case StreamFilterName.Subtitles:
                    return "subtitles";
                default:
                    throw new ArgumentOutOfRangeException(nameof(filterName), filterName, null);
            }
        }
    }
}
