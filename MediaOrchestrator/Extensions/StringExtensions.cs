using System.Linq;

namespace MediaOrchestrator.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Wraps a string in double quotes, removing any existing outer quotes first.
        ///     This ensures the string is properly quoted for use in command-line arguments.
        ///     Example: "path/with spaces" becomes ""path/with spaces"" (outer quotes stripped, then wrapped).
        /// </summary>
        /// <param name="output">The string to escape/quote.</param>
        /// <returns>The string wrapped in double quotes, or the original if null/empty.</returns>
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

        /// <summary>
        ///     Removes outer double or single quotes from a string if present.
        ///     This reverses the effect of Escape() by stripping the surrounding quotes.
        ///     Example: ""path/with spaces"" becomes "path/with spaces".
        /// </summary>
        /// <param name="output">The string to unescape/dequote.</param>
        /// <returns>The string with outer quotes removed, or the original if null/empty or not quoted.</returns>
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
