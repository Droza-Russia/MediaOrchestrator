using System.Collections.Generic;

namespace MediaOrchestrator
{
    internal class ConversionParameter
    {
        public ConversionParameter(string parameter, ParameterPosition position = ParameterPosition.PostInput)
        {
            var trimmedParameter = parameter.Trim();
            Parameter = $"{trimmedParameter} ";

            var separatorIndex = trimmedParameter.IndexOf(' ');
            Key = separatorIndex > 0 ? trimmedParameter.Substring(0, separatorIndex) : trimmedParameter;
            Position = position;
        }

        public string Parameter { get; set; }
        public string Key { get; }
        public ParameterPosition Position { get; set; } = ParameterPosition.PostInput;

        public override bool Equals(object obj)
        {
            return obj is ConversionParameter parameter &&
                   Key == parameter.Key &&
                   Position == parameter.Position &&
                   Key != "-i";
        }

        public override int GetHashCode()
        {
            var hashCode = 495346454;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Key);
            hashCode = (hashCode * -1521134295) + Position.GetHashCode();
            return hashCode;
        }
    }
}
