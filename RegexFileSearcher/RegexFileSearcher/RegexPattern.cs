using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexFileSearcher
{
    internal class RegexPattern
    {
        public string Pattern;
        public string ReplacementText;
        public bool IsCompiled;
        public bool IsIgnoreCase;
        public bool IsMultiline;
        public bool IsExplicitCapture;
        public bool IsEcmaScript;
        public bool IsIgnoreWhite;
        public bool IsSingleLine;
        public bool IsRightToLeft;
        public bool IsCultureInvariant;
        public int Timeout;

        public Regex Regex => new Regex(Pattern, RegexOptions, TimeSpan.FromSeconds(Timeout));

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
