using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Eto.Forms;

namespace RegexFileSearcher
{
    internal class RegexSearcher
    {
        public bool IsSearching { get; private set; }
        public string CurrentDirectory { get; set; }
        private readonly int Depth;
        private readonly string SearchDirectory;
        private readonly bool RecurseSubdirectories;
        private readonly Regex FilenameRegex, ContentRegex;
        private readonly CancellationToken CancellationToken;
        private readonly TreeGridItemCollection ItemCollection;

        // waiting for init-only properties in C# 9
        public RegexSearcher(string searchDir, int depth,
                             Regex fileRegex, Regex contentRegex,
                             TreeGridItemCollection itemCollection,
                             CancellationToken token)
        {
            SearchDirectory = searchDir;
            Depth = depth;
            RecurseSubdirectories = depth < 0;
            FilenameRegex = fileRegex;
            ContentRegex = contentRegex;
            ItemCollection = itemCollection;
            CancellationToken = token;
        }
        public void StartSearch()
        {
            if (SearchDirectory?.Trim()?.Length == 0)
                return;

            if (!Directory.Exists(SearchDirectory))
                return;

            IsSearching = true;
            switch (FilenameRegex, ContentRegex)
            {
                case (null, null):
                    break;
                case (_, null):
                    MatchWith(FilenameMatcher);
                    break;
                case (null, _):
                    MatchWith(ContentMatcher);
                    break;
                default:
                    MatchWith(AllMatcher);
                    break;
            }
            IsSearching = false;
        }
        private static readonly EnumerationOptions options = new EnumerationOptions { IgnoreInaccessible = true };
        private IEnumerable<IEnumerable<string>> EnumerateFiles(string dir, int currentDepth)
        {
            if (!RecurseSubdirectories && currentDepth < 0)
                yield break;

            CurrentDirectory = dir;

            // although  IgnoreInaccessible  is true by default
            // it only applies when you use the proper overload
            yield return Directory.EnumerateFiles(dir, "*", options);
            foreach (var subDir in Directory.EnumerateDirectories(dir, "*", options))
            {
                foreach (var subFiles in EnumerateFiles(subDir, currentDepth - 1))
                    yield return subFiles;
            }
        }
        private void MatchWith(Action<string> matcher)
        {
            try
            {
                EnumerateFiles(SearchDirectory, Depth)
                .AsParallel()
                .WithCancellation(CancellationToken)
                .ForAll(files =>
                {
                    if (CancellationToken.IsCancellationRequested)
                        return;

                    files.AsParallel().ForAll(matcher);
                });
            }
            catch
            {
            }
        }
        private void FilenameMatcher(string fileName)
        {
            try
            {
                fileName = Path.GetFileName(fileName);
                if (!FilenameRegex.IsMatch(fileName))
                    return;
                Add(fileName);
            }
            catch
            {
            }
        }
        private void ContentMatcher(string fileName)
        {
            try
            {
                var fi = new FileInfo(fileName);
                using var reader = fi.OpenText();
                int count = 0;

                // if less than 50 MB
                if (fi.Length < 1024 * 1024 * 50)
                {
                    string input = reader.ReadToEnd();
                    count = ContentRegex.Matches(input).Count;
                }
                else
                {
                    // 10 MB read buffer
                    const int BUFFER_SIZE = 1024 * 1024 * 10;
                    var readBuffer = new char[BUFFER_SIZE];
                    while (reader.ReadBlock(readBuffer) != 0)
                    {
                        count += ContentRegex.Matches(new string(readBuffer)).Count;
                    }
                }
                Add(fileName, count);
            }
            catch
            {
            }
        }
        private void AllMatcher(string fileName)
        {
            try
            {
                if (!FilenameRegex.IsMatch(Path.GetFileName(fileName)))
                    return;

                var fi = new FileInfo(fileName);
                using var reader = fi.OpenText();
                int count = 0;

                if (fi.Length < 1024 * 1024 * 50)
                {
                    string input = reader.ReadToEnd();
                    count = ContentRegex.Matches(input).Count;
                }
                else
                {
                    const int BUFFER_SIZE = 1024 * 1024 * 5;
                    var readBuffer = new char[BUFFER_SIZE];
                    while (reader.ReadBlock(readBuffer) != 0)
                    {
                        count += ContentRegex.Matches(new string(readBuffer)).Count;
                    }
                }
                Add(fileName, count);
            }
            catch
            {
            }
        }
        private void Add(string fileName, int count = 0)
        {
            // false: selected @ column 0
            // null:  custom cell for open link button @ column 1
            ItemCollection.Add(new TreeGridItem(false, null, count, fileName));
        }
    }
}