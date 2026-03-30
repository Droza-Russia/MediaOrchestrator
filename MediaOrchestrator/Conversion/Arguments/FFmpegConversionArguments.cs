using System;
using MediaOrchestrator.Extensions;

namespace MediaOrchestrator
{
    internal static class FFmpegConversionArguments
    {
        internal static string MapAudioStream(int inputIndex, int audioStreamIndex)
        {
            return $"-map {inputIndex}:a:{audioStreamIndex}";
        }

        internal static string MapStream(int inputIndex, int streamIndex)
        {
            return $"-map {inputIndex}:{streamIndex} ";
        }

        internal static string MapPrimaryInputVideo()
        {
            return "-map 0:0 ";
        }

        internal static string MapFilterBlock(string filterType)
        {
            return $"{filterType} \"";
        }

        internal static string MapFilterBlock(FilterBlockType filterBlockType)
        {
            return $"{filterBlockType.ToArgumentName()} \"";
        }

        internal static string MapFilterOutput(FilterLabel label)
        {
            return $"-map \"{label.AsReference()}\"";
        }

        internal static string MapFilterInput(int streamNumber)
        {
            return $"[{streamNumber}]";
        }

        internal static string RenderNamedFilter(string filterName, string filterValue)
        {
            return string.IsNullOrEmpty(filterValue) ? $"{filterName} " : $"{filterName}={filterValue}";
        }

        internal static string PipeSpecifier(PipeDescriptor descriptor)
        {
            return $"pipe:{(int)descriptor}";
        }

        internal static string AddEscapedInput(string source)
        {
            return $"-i {source.Escape()} ";
        }

        internal static string SetHardwareAcceleration(string hardwareAcceleration)
        {
            return $"-hwaccel {hardwareAcceleration}";
        }
    }
}
