using System;

namespace Vortex {
    public static class StringExtensions {
        public static string[] Split(this string source, string separator,
            StringSplitOptions options = StringSplitOptions.None)
            => source.Split (new[] { separator }, options);

        public static string Remove(this string source, string startString)
            => source.Remove (source.IndexOf (startString) + startString.Length);

        public static string Remove(this string source, char startChar)
            => source.Remove (source.IndexOf (startChar) + 1);

        public static bool Contains(this string source, char value)
            => source.IndexOf (value) > -1;
    }
}