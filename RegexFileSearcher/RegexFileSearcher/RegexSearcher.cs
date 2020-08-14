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
        private readonly int _depth;
        private readonly string _searchDirectory;
        private readonly bool _recurseSubdirectories;
        private readonly Regex _filenameRegex, _contentRegex;
        private readonly CancellationToken _cancellationToken;
        private readonly TreeGridItemCollection _itemCollection;
        private static readonly EnumerationOptions options = new EnumerationOptions { IgnoreInaccessible = true };
        private static volatile bool _searchEnded;
        // Waiting for init-only properties in C# 9
        public RegexSearcher(string searchDir, int depth,
                             Regex fileRegex, Regex contentRegex,
                             TreeGridItemCollection itemCollection,
                             CancellationToken token)
        {
            _searchDirectory = searchDir;
            _depth = depth;
            _recurseSubdirectories = depth < 0;
            _filenameRegex = fileRegex;
            _contentRegex = contentRegex;
            _itemCollection = itemCollection;
            _cancellationToken = token;
            _searchEnded = false;
        }
        public string CurrentDirectory { get; private set; }
        public event Action<bool> SearchEnded;
        public event Action<string> CurrentDirectoryChanged;
        public void StartSearch()
        {
            if (_searchDirectory?.Trim()?.Length == 0 || !Directory.Exists(_searchDirectory))
            {
                OnSearchEnded();
                return;
            }

            switch (_filenameRegex, _contentRegex)
            {
                case (null, null):
                    break;
                case (_, null):
                    MatchWith(MatchOnFilename);
                    break;
                case (null, _):
                    MatchWith(MatchOnContent);
                    break;
                default:
                    MatchWith(MatchAll);
                    break;
            }
            OnSearchEnded();
        }
        protected void OnCurrentDirectoryChanged() => CurrentDirectoryChanged?.Invoke(CurrentDirectory);
        protected void OnSearchEnded()
        {
            _searchEnded = true;
            SearchEnded?.Invoke(false);
        }
        private IEnumerable<IEnumerable<string>> EnumerateFiles(string dir, int currentDepth)
        {
            if (!_recurseSubdirectories && currentDepth < 0)
                yield break;

            CurrentDirectory = dir;
            OnCurrentDirectoryChanged();
            // Although  IgnoreInaccessible  is true by default,
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
                foreach (var files in EnumerateFiles(_searchDirectory, _depth))
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    files
                    .AsParallel()
                    .WithCancellation(_cancellationToken)
                    .WithDegreeOfParallelism(Math.Max(1, Environment.ProcessorCount - 1))
                    .ForAll(matcher);
                }
            }
            catch
            {
            }
        }
        private void MatchOnFilename(string fileName)
        {
            try
            {
                fileName = Path.GetFileName(fileName);
                if (_cancellationToken.IsCancellationRequested)
                    return;
                if (!_filenameRegex.IsMatch(fileName))
                    return;

                Add(fileName);
            }
            catch
            {
            }
        }
        private void MatchOnContent(string fileName)
        {
            try
            {
                var fi = new FileInfo(fileName);
                using var fileReader = fi.OpenText();
                int count = 0;
                if (fi.Length <= 50 * 1024 * 1024)
                {
                    count = _contentRegex.Matches(fileReader.ReadToEnd()).Count;
                }
                else
                {
                    const int BUFFER_SIZE = 1024 * 1024 * 10;
                    var buffer = new char[BUFFER_SIZE];
                    while (fileReader.ReadBlock(buffer, 0, BUFFER_SIZE) != 0)
                    {
                        if (_cancellationToken.IsCancellationRequested)
                            break;
                        // Expensive  and   allocating.
                        // Waiting for regex span APIs.
                        count += _contentRegex.Matches(new string(buffer)).Count;
                    }
                }
                if (count > 0)
                    Add(fileName, count);
            }
            catch
            {
            }
        }
        private void MatchAll(string fileName)
        {
            try
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;
                if (!_filenameRegex.IsMatch(Path.GetFileName(fileName)))
                    return;

                var fi = new FileInfo(fileName);
                using var fileReader = fi.OpenText();
                int count = 0;
                if (fi.Length < 50 * 1024 * 1024)
                {
                    count = _contentRegex.Matches(fileReader.ReadToEnd()).Count;
                }
                else
                {
                    const int BUFFER_SIZE = 1024 * 1024 * 10;
                    var buffer = new char[BUFFER_SIZE];
                    while (fileReader.ReadBlock(buffer, 0, BUFFER_SIZE) != 0)
                    {
                        if (_cancellationToken.IsCancellationRequested)
                            break;
                        count += _contentRegex.Matches(new string(buffer)).Count;
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
            lock (_itemCollection)
            {
                if (!_searchEnded)
                {
                    // false: selected CheckBox @ column 0
                    // null:  custom cell for open LinkButton @ column 1
                    _itemCollection.Add(new TreeGridItem(false, null, count, fileName));
                }
            }
        }
    }
}
