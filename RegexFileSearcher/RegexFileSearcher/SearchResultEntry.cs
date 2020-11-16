using Eto.Forms;

namespace RegexFileSearcher
{
    internal class SearchResultEntry : TreeGridItem
    {
        private const int selectColumnNumber = 0;
        private const int matchesColumnNumber = 2;
        private const int pathColumnNumber = 3;
        private const int numberOfColumns = 4;

        public SearchResultEntry(int matches, FilePath filePath)
        {
            Values = new object[numberOfColumns];
            IsSelected = false;
            Matches = matches;
            FilePath = filePath;
        }

        public bool IsSelected
        {
            get => (bool)Values[selectColumnNumber];
            set => Values[selectColumnNumber] = value;
        }

        public int Matches
        {
            get => (int)Values[matchesColumnNumber];
            private init => Values[matchesColumnNumber] = value;
        }

        public FilePath FilePath
        {
            get => (FilePath)Values[pathColumnNumber];
            private init => Values[pathColumnNumber] = value;
        }
    }
}