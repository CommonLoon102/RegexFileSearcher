using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Eto.Forms;

namespace RegexFileSearcher
{
    internal class RegexSearcher
    {
        public event Action<bool> SearchEnded;
        protected void OnSearchEnded()
        {
            _searchEnded = true;
            SearchEnded?.Invoke(false);
        }
        public event Action<string> CurrentDirectoryChanged;
        protected void OnCurrentDirectoryChanged() => CurrentDirectoryChanged?.Invoke(CurrentDirectory);
        public string CurrentDirectory { get; private set; }
        private readonly int Depth;
        private readonly string SearchDirectory;
        private readonly bool RecurseSubdirectories;
        private readonly Regex FilenameRegex, ContentRegex;
        private readonly CancellationToken CancellationToken;
        private readonly TreeGridItemCollection ItemCollection;
        public static readonly object collectionLocker = new object();
        private static volatile bool _searchEnded;
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
            _searchEnded = false;
        }
        public void StartSearch()
        {
            if (SearchDirectory?.Trim()?.Length == 0 || !Directory.Exists(SearchDirectory))
            {
                OnSearchEnded();
                return;
            }

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
            OnSearchEnded();
        }
        private static readonly EnumerationOptions options = new EnumerationOptions { IgnoreInaccessible = true };
        private IEnumerable<IEnumerable<string>> EnumerateFiles(string dir, int currentDepth)
        {
            if (!RecurseSubdirectories && currentDepth < 0)
                yield break;

            CurrentDirectory = dir;
            OnCurrentDirectoryChanged();
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
                foreach (var files in EnumerateFiles(SearchDirectory, Depth))
                {
                    if (CancellationToken.IsCancellationRequested)
                        return;

                    files
                    .AsParallel()
                    .WithCancellation(CancellationToken)
                    .WithDegreeOfParallelism(Math.Max(1, Environment.ProcessorCount - 1))
                    .Select(x => x)
                    .ForAll(matcher);
                }
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
                if (CancellationToken.IsCancellationRequested)
                    return;
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
                using var fileReader = fi.OpenText();
                int count = 0;
                if (fi.Length <= 50 * 1024 * 1024)
                {
                    count = ContentRegex.Matches(fileReader.ReadToEnd()).Count;
                }
                else
                {
                    const int BUFFER_SIZE = 1024 * 1024 * 10;
                    var buffer = new char[BUFFER_SIZE];
                    while (fileReader.ReadBlock(buffer, 0, BUFFER_SIZE) != 0)
                    {
                        if (CancellationToken.IsCancellationRequested)
                            break;
                        // expensive  and  allocating
                        // waiting for regex span APIs
                        count += ContentRegex.Matches(new string(buffer)).Count;
                    }
                }
                if (count > 0)
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
                if (CancellationToken.IsCancellationRequested)
                    return;
                if (!FilenameRegex.IsMatch(Path.GetFileName(fileName)))
                    return;

                var fi = new FileInfo(fileName);
                using var fileReader = fi.OpenText();
                int count = 0;
                if (fi.Length < 50 * 1024 * 1024)
                {
                    count = ContentRegex.Matches(fileReader.ReadToEnd()).Count;
                }
                else
                {
                    const int BUFFER_SIZE = 1024 * 1024 * 10;
                    var buffer = new char[BUFFER_SIZE];
                    while (fileReader.ReadBlock(buffer, 0, BUFFER_SIZE) != 0)
                    {
                        if (CancellationToken.IsCancellationRequested)
                            break;
                        count += ContentRegex.Matches(new string(buffer)).Count;
                    }
                }
                if (count > 0)
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
            lock (collectionLocker)
            {
                if (!_searchEnded)
                    ItemCollection.Add(new TreeGridItem(false, null, count, fileName));
            }
        }
    }
}