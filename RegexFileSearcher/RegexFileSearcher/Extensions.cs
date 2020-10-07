using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RegexFileSearcher
{
    internal static class Extensions
    {
        public static IEnumerable<FilePath> NotZippedFiles(this IEnumerable<FilePath> filePaths)
        {
            return filePaths.Where(fp => !IsZipFile(fp.Path));
        }

        public static bool IsZipFile(this string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension == ".zip";
        }
    }
}
