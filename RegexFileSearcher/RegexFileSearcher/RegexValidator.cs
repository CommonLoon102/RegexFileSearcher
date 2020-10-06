using System;
using System.Text.RegularExpressions;

namespace RegexFileSearcher
{
    public class RegexValidator
    {
        public static bool IsValidRegex(string pattern, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException e)
            {
                errorMessage = e.Message;
                return false;
            }
            return true;
        }
    }
}
