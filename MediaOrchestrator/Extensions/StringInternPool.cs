using System;
using System.Collections.Concurrent;

namespace MediaOrchestrator.Extensions
{
    internal static class StringInternPool
    {
        private static readonly ConcurrentDictionary<string, string> _internedStrings = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        public static string Intern(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return _internedStrings.GetOrAdd(value, v => v);
        }

        public static string InternReadOnly(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return string.Intern(value);
        }

        public static void Clear()
        {
            _internedStrings.Clear();
        }

        public static int Count => _internedStrings.Count;
    }
}