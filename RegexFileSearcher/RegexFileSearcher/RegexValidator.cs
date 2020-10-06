using System;
using System.Text.RegularExpressions;

namespace RegexFileSearcher
{
    public class RegexValidator
    {
        public static bool IsRegexValid(string pattern, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                Regex.IsMatch("", pattern);
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
