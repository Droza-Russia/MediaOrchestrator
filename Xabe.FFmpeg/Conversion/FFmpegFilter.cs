using System;
using System.Collections.Generic;
using System.Linq;

namespace Xabe.FFmpeg
{
    internal sealed class FFmpegFilter
    {
        internal FFmpegFilter(string name, params FFmpegFilterArgument[] arguments)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Filter name must be provided.", nameof(name));
            }

            Name = name;
            Arguments = arguments ?? Array.Empty<FFmpegFilterArgument>();
        }

        internal string Name { get; }

        internal IReadOnlyList<FFmpegFilterArgument> Arguments { get; }

        internal string Render()
        {
            if (!Arguments.Any())
            {
                return Name;
            }

            return $"{Name}={string.Join(":", Arguments.Select(argument => argument.Render()))}";
        }
    }
}
