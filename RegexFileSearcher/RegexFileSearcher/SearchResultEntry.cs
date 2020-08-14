using Eto.Forms;

namespace RegexFileSearcher
{
    internal class SearchResultEntry : TreeGridItem
    {
        private const int selectColumnNumber = 0;
        private const int matchesColumnNumber = 2;
        private const int pathColumnNumber = 3;
        private const int numberOfColumns = 4;

        public SearchResultEntry()
        {
            base.Values = new object[numberOfColumns];
            IsSelected = false;
            Matches = 0;
            Path = "";
        }

        public bool IsSelected
        {
            get => (bool)Values[selectColumnNumber];
            set => Values[selectColumnNumber] = value;
        }

        public int Matches
        {
            get => (int)Values[matchesColumnNumber];
            set => Values[matchesColumnNumber] = value;
        }

        public string Path
        {
            get => (string)Values[pathColumnNumber];
            set => Values[pathColumnNumber] = value;
        }
    }
}