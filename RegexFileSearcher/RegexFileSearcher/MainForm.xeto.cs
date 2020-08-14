using System;
using System.Collections.Generic;
using Eto.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace RegexFileSearcher
{
    public partial class MainForm : Form
    {
        private readonly TreeGridItemCollection _itemCollection = new TreeGridItemCollection();
        private CancellationTokenSource _cancellationTokenSource;
        private Timer _updateTimer;
        private bool _matchNumberOrdering;
        private volatile bool _searchEnded = true;

        public MainForm() : this(initializeControls: true)
        {
            InitializeSubdirectoryPicker();
            InitializeResultExplorer();
        }

        private int SearchDepth => int.Parse(cboSubdirectories.SelectedKey);
        private Regex FilenameRegex =>
            string.IsNullOrEmpty(txtFilenameRegex.Text)
            ? null : new RegexPattern
            {
                Pattern = txtFilenameRegex.Text,
                IsCompiled = chkCompiled.Checked ?? false,
                IsCultureInvariant = chkCultureInvariant.Checked ?? false,
                IsEcmaScript = chkEcmaScript.Checked ?? false,
                IsExplicitCapture = chkExplicitCapture.Checked ?? false,
                IsIgnoreWhite = chkIgnoreWhite.Checked ?? false,
                IsIgnoreCase = chkIgnoreCase.Checked ?? false,
                IsMultiline = chkMultiline.Checked ?? false,
                IsRightToLeft = chkRightToLeft.Checked ?? false,
                IsSingleLine = chkSingleLine.Checked ?? false,
                Timeout = (int)nudTimeout.Value
            }.Regex;
        private Regex ContentRegex =>
            string.IsNullOrEmpty(txtContentRegex.Text)
            ? null : new RegexPattern
            {
                Pattern = txtContentRegex.Text,
                IsCompiled = chkContentCompiled.Checked ?? false,
                IsCultureInvariant = chkContentCultureInvariant.Checked ?? false,
                IsEcmaScript = chkContentEcmaScript.Checked ?? false,
                IsExplicitCapture = chkContentExplicitCapture.Checked ?? false,
                IsIgnoreWhite = chkContentIgnoreWhite.Checked ?? false,
                IsIgnoreCase = chkContentIgnoreCase.Checked ?? false,
                IsMultiline = chkContentMultiline.Checked ?? false,
                IsRightToLeft = chkContentRightToLeft.Checked ?? false,
                IsSingleLine = chkContentSingleLine.Checked ?? false,
                Timeout = (int)nudContentTimeout.Value
            }.Regex;
        private RegexSearcher NewSearcher()
        {
            return new RegexSearcher(fpSearchPath.FilePath, SearchDepth, FilenameRegex, ContentRegex,
                                     _itemCollection,_cancellationTokenSource.Token);
        }

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
                    Command = new Command((_, __) => HandleOpenItem(e.Item))
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
            var tgItem = item as TreeGridItem;
            if (CheckEditor())
                OpenInEditor((string)tgItem.GetValue(3));
        }
        private bool CheckEditor()
        {
            if (string.IsNullOrWhiteSpace(fpOpenWith.FilePath))
            {
                MessageBox.Show("No editor has been specified.", "Cannot open", MessageBoxType.Information);
                return false;
            }

            return true;
        }

        private void OpenInEditor(string path)
        {
            Process.Start(fpOpenWith.FilePath.Trim(), path);
        }
        private void HandleSearch(object sender, EventArgs e)
        {
            if (_searchEnded)
            {
                StartSearch();
                return;
            }

            if (MessageBox.Show("Are you sure you want to stop the search?", "Question",
                MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.No) == DialogResult.Yes)
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

            _cancellationTokenSource = new CancellationTokenSource();
            var searcher = NewSearcher();
            searcher.SearchEnded += EndSearch;
            searcher.CurrentDirectoryChanged += UpdateStatusLabel;

            Task.Factory.StartNew(searcher.StartSearch,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            _updateTimer = new Timer(_ => UpdateResultExplorer(), null, 0, 1000);
        }
        private void EndSearch(bool isUserRequested = true)
        {
            _updateTimer?.Dispose();
            if (isUserRequested)
                _cancellationTokenSource.Cancel();

            Application.Instance.Invoke(() =>
            {
                tvwResultExplorer.ReloadData();
                lblStatus.Text = "Search ended";
                btnStartSearch.Text = "Start Search";
                btnOrderByMatches.Enabled = true;
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
                    if (_itemCollection.Count > 0)
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
            foreach (TreeGridItem item in _itemCollection)
            {
                item.SetValue(0, value);
            }

            tvwResultExplorer.ReloadData();
        }

        private void HandleInvertSelection(object sender, EventArgs e)
        {
            foreach (TreeGridItem item in _itemCollection)
            {
                item.SetValue(0, !(bool)item.GetValue(0));
            }

            tvwResultExplorer.ReloadData();
        }

        private void HandleOpenSelected(object sender, EventArgs e)
        {
            if (!CheckEditor())
                return;

            List<string> filesToOpen = new List<string>();
            foreach (TreeGridItem item in _itemCollection)
            {
                bool isSelected = (bool)item.GetValue(0);
                if (isSelected)
                {
                    filesToOpen.Add((string)item.GetValue(3));
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

            foreach (string path in filesToOpen)
            {
                OpenInEditor(path);
            }
        }

        private void HandleOrderByMatches(object sender, EventArgs e)
        {
            // Reverses meaning of int.CompareTo
            // depending on the current ordering
            int direction = _matchNumberOrdering ? 1 : -1;
            int ComapreItems(ITreeGridItem item, ITreeGridItem otherItem)
            {
                int a = (int)(item as TreeGridItem)?.GetValue(2);
                int b = (int)(otherItem as TreeGridItem)?.GetValue(2);
                return a.CompareTo(b) * direction;
            }
            _itemCollection.Sort(ComapreItems);

            _matchNumberOrdering = !_matchNumberOrdering;

            if (_itemCollection.Count > 0)
            {
                tvwResultExplorer.ScrollToRow(0);
            }
        }

        private void HandleResultExplorerSelectedItemChanged(object sender, EventArgs e)
        {
            txtPath.Text = (string)(tvwResultExplorer.SelectedItem as TreeGridItem)?.GetValue(3) ?? "";
        }
    }
}
