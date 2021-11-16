using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RegexFileSearcher
{
    public partial class MainForm : Form
    {
        private const int FileNameRegexTimeoutInSeconds = 5;

        private readonly TreeGridItemCollection _itemCollection = new();

        private CancellationTokenSource _cancellationTokenSource;
        private Timer _updateTimer;
        private Color _defaultPanelBackgroundColor;
        private bool _matchNumberOrdering;
        private volatile bool _searchEnded = true;

        public MainForm()
            : this(initializeControls: true)
        {
            InitializeSubdirectoryPicker();
            InitializeResultExplorer();

            txtFileNameRegex.TextChanged += OnTextBoxChangedRegex;
            txtContentRegex.TextChanged += OnTextBoxChangedRegex;
            _defaultPanelBackgroundColor = txtFileNameRegex.Parent.Parent.BackgroundColor;
        }

        private void OnTextBoxChangedRegex(object sender, EventArgs e)
        {
            bool isFileNameRegexValid = ValidateRegex(txtFileNameRegex);
            bool isContentRegexValid = ValidateRegex(txtContentRegex);
            btnStartSearch.Enabled = isFileNameRegexValid && isContentRegexValid;

            bool ValidateRegex(TextBox textBox)
            {
                bool isRegexValid = RegexValidator.IsRegexValid(textBox.Text, out string errorMessage);
                textBox.ToolTip = isRegexValid ? null : errorMessage;
                textBox.Parent.Parent.BackgroundColor = isRegexValid
                    ? _defaultPanelBackgroundColor
                    : Colors.LightSalmon;
                return isRegexValid;
            }
        }

        private int SearchDepth => int.Parse(cboSubdirectories.SelectedKey);

        private Regex FileNameRegex =>
            string.IsNullOrEmpty(txtFileNameRegex.Text)
            ? null
            : new RegexPattern(txtFileNameRegex.Text)
            {
                IsCompiled = chkCompiled.Checked ?? false,
                IsCultureInvariant = chkCultureInvariant.Checked ?? false,
                IsEcmaScript = chkEcmaScript.Checked ?? false,
                IsExplicitCapture = chkExplicitCapture.Checked ?? false,
                IsIgnoreWhite = chkIgnoreWhite.Checked ?? false,
                IsIgnoreCase = chkIgnoreCase.Checked ?? false,
                IsMultiline = chkMultiline.Checked ?? false,
                IsRightToLeft = chkRightToLeft.Checked ?? false,
                IsSingleLine = chkSingleLine.Checked ?? false,
                TimeoutInSeconds = FileNameRegexTimeoutInSeconds
            }.Regex;

        private Regex ContentRegex =>
            string.IsNullOrEmpty(txtContentRegex.Text)
            ? null
            : new RegexPattern(txtContentRegex.Text)
            {
                IsCompiled = chkContentCompiled.Checked ?? false,
                IsCultureInvariant = chkContentCultureInvariant.Checked ?? false,
                IsEcmaScript = chkContentEcmaScript.Checked ?? false,
                IsExplicitCapture = chkContentExplicitCapture.Checked ?? false,
                IsIgnoreWhite = chkContentIgnoreWhite.Checked ?? false,
                IsIgnoreCase = chkContentIgnoreCase.Checked ?? false,
                IsMultiline = chkContentMultiline.Checked ?? false,
                IsRightToLeft = chkContentRightToLeft.Checked ?? false,
                IsSingleLine = chkContentSingleLine.Checked ?? false,
                TimeoutInSeconds = (int)nudContentTimeout.Value
            }.Regex;

        private RegexSearcher CreateNewSearcher() =>
            new(fpSearchPath.FilePath,
                FileNameRegex,
                ContentRegex,
                _itemCollection,
                _cancellationTokenSource.Token)
        {
            SearchDepth = SearchDepth,
            SearchInZipFiles = chkSearchInZipFiles.Checked ?? false,
            MaxFileSize = (int)nudMaxFileSize.Value * 1024 * 1024
        };

        private void InitializeSubdirectoryPicker()
        {
            cboSubdirectories.SuspendLayout();
            cboSubdirectories.Items.Add("All (unlimited depth)", "-1");
            cboSubdirectories.Items.Add("Current directory only", "0");
            cboSubdirectories.Items.Add("1 level", "1");
            for (int i = 2; i <= 32; i++)
            {
                cboSubdirectories.Items.Add($"{i} levels", i.ToString());
            }

            cboSubdirectories.SelectedKey = "-1";
            cboSubdirectories.ReadOnly = true;
            cboSubdirectories.ResumeLayout();
            cboSubdirectories.Invalidate();
        }

        private void InitializeResultExplorer()
        {
            var openCell = new CustomCell
            {
                CreateCell = e => new LinkButton
                {
                    Text = "Open",
                    Command = new Command((_, _) => HandleOpenItem(e.Item))
                }
            };

            var columns = new[]
            {
                new GridColumn { HeaderText = "Select",  DataCell = new CheckBoxCell(0), Editable = true },
                new GridColumn { HeaderText = "Open",    DataCell = openCell },
                new GridColumn { HeaderText = "Matches", DataCell = new TextBoxCell(2) },
                new GridColumn { HeaderText = "Path",    DataCell = new TextBoxCell(3) },
            };

            Array.ForEach(columns, tvwResultExplorer.Columns.Add);
            tvwResultExplorer.AllowMultipleSelection = false;
            tvwResultExplorer.DataStore = _itemCollection;
        }

        private void HandleOpenItem(object item)
        {
            var entry = item as SearchResultEntry;
            OpenInEditor(entry.FilePath);
        }

        private void OpenInEditor(FilePath path)
        {
            string pathToOpen = path.Parent is null ? path.Path : GetTempPath(path);

            try
            {
                if (!string.IsNullOrWhiteSpace(fpOpenWith.FilePath))
                {
                    Process.Start(fpOpenWith.FilePath.Trim(), pathToOpen);
                }
                else
                {
                    FileHandler.Open(pathToOpen);
                }
            }
            catch (FileHandlerException ex)
            {
                MessageBox.Show(ex.Message, "Cannot open", MessageBoxType.Information);
            }
        }

        private static string GetTempPath(FilePath path)
        {
            string tempFileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            tempFileName += Path.GetExtension(path.Path);
            string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            File.WriteAllText(tempPath, path.GetFileContent());
            return tempPath;
        }

        private void HandleSearch(object sender, EventArgs e)
        {
            if (_searchEnded)
            {
                StartSearch();
                return;
            }

            if (MessageBox.Show("Are you sure you want to stop the search?",
                "Question",
                MessageBoxButtons.YesNo,
                MessageBoxType.Question,
                MessageBoxDefaultButton.No) == DialogResult.Yes)
            {
                EndSearch();
            }
        }

        private void StartSearch()
        {
            _searchEnded = false;
            _itemCollection.Clear();
            tvwResultExplorer.ReloadData();

            btnStartSearch.Text = "Stop Search";
            lblStatus.Text = string.Empty;
            txtPath.Text = string.Empty;
            btnOrderByMatches.Enabled = false;
            txtFileNameRegex.Enabled = false;
            txtContentRegex.Enabled = false;

            _cancellationTokenSource = new();
            RegexSearcher searcher = CreateNewSearcher();
            searcher.SearchEnded += EndSearch;
            searcher.CurrentDirectoryChanged += UpdateStatusLabel;

            Task.Factory.StartNew(searcher.StartSearch,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            _updateTimer = new(state => UpdateResultExplorer(),
                state: null,
                dueTime: 0,
                period: (int)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }

        private void EndSearch(bool isUserRequested = true)
        {
            _updateTimer?.Dispose();
            if (isUserRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            UpdateResultExplorer();
            UpdateStatusLabel("Search ended");
            Application.Instance.Invoke(() =>
            {
                btnStartSearch.Text = "Start Search";
                btnOrderByMatches.Enabled = true;
                txtFileNameRegex.Enabled = true;
                txtContentRegex.Enabled = true;
            });

            _searchEnded = true;
        }

        private void UpdateStatusLabel(string dir)
        {
            Application.Instance.Invoke(() => lblStatus.Text = dir);
        }

        private void UpdateResultExplorer()
        {
            Application.Instance.Invoke(() =>
            {
                lock (_itemCollection)
                {
                    tvwResultExplorer.ReloadData();
                    if (_itemCollection.Any())
                    {
                        tvwResultExplorer.ScrollToRow(_itemCollection.Count - 1);
                    }
                }
            });
        }

        private void HandleSelectAll(object sender, EventArgs e)
        {
            SelectAll(true);
        }

        private void HandleSelectNone(object sender, EventArgs e)
        {
            SelectAll(false);
        }

        private void SelectAll(bool value)
        {
            foreach (SearchResultEntry entry in _itemCollection)
            {
                entry.IsSelected = value;
            }

            tvwResultExplorer.ReloadData();
        }

        private void HandleInvertSelection(object sender, EventArgs e)
        {
            foreach (SearchResultEntry entry in _itemCollection)
            {
                entry.IsSelected = !entry.IsSelected;
            }

            tvwResultExplorer.ReloadData();
        }

        private void HandleOpenSelected(object sender, EventArgs e)
        {
            var filesToOpen = new List<FilePath>();
            foreach (SearchResultEntry entry in _itemCollection)
            {
                if (entry.IsSelected)
                {
                    filesToOpen.Add(entry.FilePath);
                }
            }

            const int nrFilesCountWarning = 20;
            if (filesToOpen.Count > nrFilesCountWarning)
            {
                if (MessageBox.Show($"You want to open more than {nrFilesCountWarning} files at once.\r\nAre you sure?",
                    "Warning",
                    MessageBoxButtons.OKCancel,
                    MessageBoxType.Warning,
                    MessageBoxDefaultButton.Cancel) == DialogResult.Cancel)
                {
                    return;
                }
            }

            foreach (FilePath path in filesToOpen)
            {
                OpenInEditor(path);
            }
        }

        private void HandleOrderByMatches(object sender, EventArgs e)
        {
            // Reverses meaning of int.CompareTo
            // depending on the current ordering
            int direction = _matchNumberOrdering ? 1 : -1;
            int CompareItems(ITreeGridItem item, ITreeGridItem otherItem)
            {
                int a = (item as SearchResultEntry)?.Matches ?? 0;
                int b = (otherItem as SearchResultEntry)?.Matches ?? 0;
                return a.CompareTo(b) * direction;
            }

            _itemCollection.Sort(CompareItems);
            _matchNumberOrdering = !_matchNumberOrdering;
            tvwResultExplorer.ReloadData();
            if (_itemCollection.Any())
            {
                tvwResultExplorer.ScrollToRow(0);
            }
        }

        private void HandleResultExplorerSelectedItemChanged(object sender, EventArgs e)
        {
            txtPath.Text = (tvwResultExplorer.SelectedItem as SearchResultEntry)?.FilePath.Path ?? "";
        }
    }
}
