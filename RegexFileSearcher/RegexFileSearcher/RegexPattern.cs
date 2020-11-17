using System;
using System.Text.RegularExpressions;

namespace RegexFileSearcher
{
    internal class RegexPattern
    {
        private readonly string _pattern;

        public RegexPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException($"The parameter '{nameof(pattern)}' must not be null or empty string.", nameof(pattern));
            }

            _pattern = pattern;
        }

        public bool IsCompiled { get; init; }
        public bool IsIgnoreCase { get; init; }
        public bool IsMultiline { get; init; }
        public bool IsExplicitCapture { get; init; }
        public bool IsEcmaScript { get; init; }
        public bool IsIgnoreWhite { get; init; }
        public bool IsSingleLine { get; init; }
        public bool IsRightToLeft { get; init; }
        public bool IsCultureInvariant { get; init; }
        public int? TimeoutInSeconds { get; init; }

        public Regex Regex =>
            new(_pattern,
                RegexOptions,
                TimeoutInSeconds is not null
                ? TimeSpan.FromSeconds(TimeoutInSeconds.Value)
                : Regex.InfiniteMatchTimeout);

        private RegexOptions RegexOptions
        {
            get
            {
                RegexOptions regexOptions = RegexOptions.None;
                if (IsCompiled)
                    regexOptions |= RegexOptions.Compiled;
                if (IsCultureInvariant)
                    regexOptions |= RegexOptions.CultureInvariant;
                if (IsEcmaScript)
                    regexOptions |= RegexOptions.ECMAScript;
                if (IsExplicitCapture)
                    regexOptions |= RegexOptions.ExplicitCapture;
                if (IsIgnoreCase)
                    regexOptions |= RegexOptions.IgnoreCase;
                if (IsIgnoreWhite)
                    regexOptions |= RegexOptions.IgnorePatternWhitespace;
                if (IsMultiline)
                    regexOptions |= RegexOptions.Multiline;
                if (IsRightToLeft)
                    regexOptions |= RegexOptions.RightToLeft;
                if (IsSingleLine)
                    regexOptions |= RegexOptions.Singleline;

                return regexOptions;
            }
        }
    }
}
