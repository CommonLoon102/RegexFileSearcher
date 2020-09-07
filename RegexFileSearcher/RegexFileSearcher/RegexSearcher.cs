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
        private static readonly EnumerationOptions _options = new EnumerationOptions { IgnoreInaccessible = true };
        private static volatile bool _searchEnded;

        private readonly int _depth;
        private readonly string _searchDirectory;
        private readonly bool _recurseSubdirectories;
        private readonly bool _searchInCompressedFiles = true;
        private readonly Regex _filenameRegex;
        private readonly Regex _contentRegex;
        private readonly CancellationToken _cancellationToken;
        private readonly TreeGridItemCollection _itemCollection;

        private string _currentDirectory;

        // Waiting for init-only properties in C# 9
        public RegexSearcher(string searchDir,
                             int depth,
                             Regex fileRegex,
                             Regex contentRegex,
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

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                if (value != _currentDirectory)
                {
                    _currentDirectory = value;
                    OnCurrentDirectoryChanged();
                }
            }
        }

        public event Action<bool> SearchEnded;
        public event Action<string> CurrentDirectoryChanged;

        public void StartSearch()
        {
            if (_searchDirectory?.Trim()?.Length == 0
                || !Directory.Exists(_searchDirectory))
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

        protected void OnCurrentDirectoryChanged()
        {
            CurrentDirectoryChanged?.Invoke(CurrentDirectory);
        }

        protected void OnSearchEnded()
        {
            _searchEnded = true;
            SearchEnded?.Invoke(false);
        }

        private IEnumerable<IEnumerable<FilePath>> EnumerateFiles(string dir, int currentDepth)
        {
            if (!_recurseSubdirectories && currentDepth < 0)
            {
                yield break;
            }

            CurrentDirectory = dir;
            List<FilePath> filePaths = new List<FilePath>();
            try
            {
                // Although   IgnoreInaccessible  is  true  by  default,
                // it only applies when you use the 3 parameter overload
                filePaths.AddRange(Directory.EnumerateFiles(dir, "*", _options).Select(f => new FilePath(f)));
            }
            catch
            {
                // IO exceptions e.g. directory was removed during enumeration
            }

            if (!filePaths.Any())
            {
                yield break;
            }

            yield return filePaths;

            // Any direcotry path exception has already been handled above
            foreach (var subDir in Directory.EnumerateDirectories(dir, "*", _options))
            {
                foreach (var subFiles in EnumerateFiles(subDir, currentDepth - 1))
                {
                    yield return subFiles;
                }
            }

            if (_searchInCompressedFiles)
            {
                foreach (FilePath filePath in filePaths)
                {
                    yield return CompressedFileWalker.GetCompressedFiles(filePath);
                }
            }
        }

        private void MatchWith(Action<FilePath> matcher)
        {
            foreach (var files in EnumerateFiles(_searchDirectory, _depth))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    files
                    .AsParallel()
                    .WithCancellation(_cancellationToken)
                    .WithDegreeOfParallelism(Math.Max(1, Environment.ProcessorCount - 1))
                    .ForAll(fileName =>
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        matcher(fileName);
                    });
                }
                catch (OperationCanceledException)
                {
                    // Operation canceled by user
                }
            }
        }

        private void MatchOnFilename(FilePath filePath)
        {
            if (IsFileNameMatches(filePath))
            {
                Add(filePath);
            }
        }

        private void MatchOnContent(FilePath filePath)
        {
            try
            {
                string fileContent = filePath.GetFileContent();
                int count = _contentRegex.Matches(fileContent).Count;
                if (count > 0)
                {
                    Add(filePath, count);
                }
            }
            catch
            {
                // Regex timeout or IO exceptions
            }
        }

        private void MatchAll(FilePath filePath)
        {
            if (IsFileNameMatches(filePath))
            {
                MatchOnContent(filePath);
            }
        }

        private bool IsFileNameMatches(FilePath filePath)
        {
            string fileName = Path.GetFileName(filePath.Path);
            return _filenameRegex.IsMatch(fileName);
        }

        private void Add(FilePath filePath, int count = 0)
        {
            lock (_itemCollection)
            {
                if (!_searchEnded)
                {
                    _itemCollection.Add(new SearchResultEntry { Matches = count, FilePath = filePath });
                }
            }
        }
    }
}
