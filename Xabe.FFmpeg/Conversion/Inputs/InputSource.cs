using System;

namespace Xabe.FFmpeg
{
    public sealed class InputSource
    {
        private InputSource(string value, string format = null, TimeSpan? duration = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(ErrorMessages.InputSourceValueMustBeProvided, nameof(value));
            }

            Value = value;
            Format = format;
            Duration = duration;
        }

        public string Value { get; }

        public string Format { get; }

        public TimeSpan? Duration { get; }

        public static InputSource File(string path)
        {
            return new InputSource(path);
        }

        public static InputSource Lavfi(string filterGraph, TimeSpan? duration = null)
        {
            return new InputSource(filterGraph, "lavfi", duration);
        }
    }
}
