using System.Linq;

namespace MediaOrchestrator.Extensions
{
    public static class StringExtensions
    {
        public static string Escape(this string output)
        {
            if (string.IsNullOrEmpty(output))
            {
                return output;
            }

            var lastChar = output[output.Length - 1];
            var firstChar = output[0];

            if ((lastChar == '"' && firstChar == '"') || (lastChar == '\'' && firstChar == '\''))
            {
                output = output.Substring(1, output.Length - 2);
            }

            return $"\"{output}\"";
        }

        public static string Unescape(this string output)
        {
            if (string.IsNullOrEmpty(output) || output.Length < 2)
            {
                return output;
            }

            var lastChar = output[output.Length - 1];
            var firstChar = output[0];

            if ((lastChar == '"' && firstChar == '"') || (lastChar == '\'' && firstChar == '\''))
            {
                return output.Substring(1, output.Length - 2);
            }

            return output;
        }
    }
}
