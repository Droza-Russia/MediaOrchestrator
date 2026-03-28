using System;

namespace MediaOrchestrator
{
    public enum FilterStreamKind
    {
        Video,
        Audio
    }

    public readonly struct FilterLabel : IEquatable<FilterLabel>
    {
        private FilterLabel(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static FilterLabel Named(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(ErrorMessages.FilterLabelNameMustBeProvided, nameof(name));
            }

            return new FilterLabel(name.Trim().Trim('[', ']'));
        }

        public static FilterLabel Parse(string label)
        {
            return Named(label);
        }

        public static FilterLabel Input(int inputIndex, FilterStreamKind streamKind, int streamIndex = 0)
        {
            return new FilterLabel($"{inputIndex}:{ToStreamCode(streamKind)}:{streamIndex}");
        }

        public static FilterLabel VideoInput(int inputIndex, int streamIndex = 0)
        {
            return Input(inputIndex, FilterStreamKind.Video, streamIndex);
        }

        public static FilterLabel AudioInput(int inputIndex, int streamIndex = 0)
        {
            return Input(inputIndex, FilterStreamKind.Audio, streamIndex);
        }

        public string AsReference()
        {
            return $"[{Value}]";
        }

        private static char ToStreamCode(FilterStreamKind streamKind)
        {
            switch (streamKind)
            {
                case FilterStreamKind.Video:
                    return 'v';
                case FilterStreamKind.Audio:
                    return 'a';
                default:
                    throw new ArgumentOutOfRangeException(nameof(streamKind), streamKind, null);
            }
        }

        public bool Equals(FilterLabel other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FilterLabel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return AsReference();
        }
    }
}
