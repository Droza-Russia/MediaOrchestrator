namespace Xabe.FFmpeg
{
    internal sealed class FFmpegFilterArgument
    {
        private FFmpegFilterArgument(string name, string value)
        {
            Name = name;
            Value = value;
        }

        internal string Name { get; }

        internal string Value { get; }

        internal static FFmpegFilterArgument Named(string name, string value)
        {
            return new FFmpegFilterArgument(name, value);
        }

        internal static FFmpegFilterArgument Positional(string value)
        {
            return new FFmpegFilterArgument(null, value);
        }

        internal string Render()
        {
            return string.IsNullOrWhiteSpace(Name) ? Value : $"{Name}={Value}";
        }
    }
}
