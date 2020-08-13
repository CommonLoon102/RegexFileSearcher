using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace RegexFileSearcher
{
    public partial class MainForm : Form
    {
        private Regex _filenameRegex;

        private Regex FilenameRegex =>
            new RegexPattern()
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

        private Regex _contentRegex;

        private Regex ContentRegex =>
            new RegexPattern()
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

        private bool _isSearching;
        private CancellationTokenSource _cancellationTokenSource;
        private static readonly object _locker = new object();
        private bool _matchNumberOrdering;
        private DateTime _lastTreeGridViewRefresh = DateTime.UtcNow;
        private int _searchDepth = -1;
        private string _contentPattern;
        private string _filenamePattern;

        public MainForm() : this(true)
        {
            AddSubdirectoriesItems();
            AddTestResultExplorerColumns();
        }

        private void AddSubdirectoriesItems()
        {
            cboSubdirectories.SuspendLayout();
            cboSubdirectories.Items.Add("all (unlimited depth)", "-1");
            cboSubdirectories.Items.Add("current dir only", "0");
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

        private void AddTestResultExplorerColumns()
        {
            tvwResultExplorer.Columns.Add(new GridColumn()
            {
                HeaderText = "Select",
                DataCell = new CheckBoxCell(0),
                Editable = true
            });

            tvwResultExplorer.Columns.Add(new GridColumn()
            {
                HeaderText = "Open",
                DataCell = new CustomCell()
                {
                    CreateCell = r =>
                    {
                        var item = r.Item as TreeGridItem;

                        void Click(object btnSender, EventArgs btnArgs)
                        {
                            if (CheckEditor())
                                OpenInEditor((string)item.GetValue(3));
                        }

                        var button = new LinkButton
                        {
                            Text = $"Open",
                            Command = new Command(Click)
                        };

                        return button;
                    }
                }
            });

            tvwResultExplorer.Columns.Add(new GridColumn()
            {
                HeaderText = "Matches",
                DataCell = new TextBoxCell(2)
            });

            tvwResultExplorer.Columns.Add(new GridColumn()
            {
                HeaderText = "Path",
                DataCell = new TextBoxCell(3)
            });

            tvwResultExplorer.AllowMultipleSelection = false;
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

        private async void HandleStartSearch(object sender, EventArgs e)
        {
            string searchPath = fpSearchPath.FilePath?.Trim();
            if (string.IsNullOrWhiteSpace(searchPath))
                return;

            if (!Directory.Exists(searchPath))
                return;

            try
            {
                lock (_locker)
                {
                    if (_isSearching)
                    {
                        if (MessageBox.Show("Are you sure you want to stop the search?", "Question",
                            MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.No) == DialogResult.Yes)
                        {
                            _cancellationTokenSource.Cancel();
                        }

                        return;
                    }

                    _isSearching = true;
                }

                _filenamePattern = txtFilenameRegex.Text;
                _contentPattern = txtContentRegex.Text;
                _searchDepth = int.Parse(cboSubdirectories.SelectedKey);

                _filenameRegex = FilenameRegex;
                _contentRegex = ContentRegex;

                var treeGridItemCollection = new TreeGridItemCollection();
                tvwResultExplorer.DataStore = treeGridItemCollection;
                tvwResultExplorer.ReloadData();
                btnStartSearch.Text = "Stop Search";
                lblStatus.Text = string.Empty;
                btnOrderByMatches.Enabled = false;
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = _cancellationTokenSource.Token;
                await Task.Factory.StartNew(() => SearchDirectory(0, searchPath, treeGridItemCollection),
                    token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxType.Error);
            }
            finally
            {
                tvwResultExplorer.ReloadData();
                lblStatus.Text = "Search ended";
                btnOrderByMatches.Enabled = true;
                btnStartSearch.Text = "Start Search";
                _isSearching = false;
            }
        }

        private void SearchDirectory(int level, string path, TreeGridItemCollection treeGridItemCollection)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            if (_searchDepth != -1 && level > _searchDepth)
                return;

            Application.Instance.Invoke(() => lblStatus.Text = path);
            List<string> filePaths = GetMatchingFiles(path);
            foreach (string filePath in filePaths.OrderBy(f => f))
            {
                try
                {
                    int count = 0;
                    bool add = false;
                    if (string.IsNullOrWhiteSpace(_contentPattern))
                    {
                        add = true;
                    }
                    else
                    {
                        MatchCollection matches = _contentRegex.Matches(File.ReadAllText(filePath));
                        count = matches.Count;
                        add = count > 0;
                    }
                    if (add)
                    {
                        treeGridItemCollection.Add(
                            new TreeGridItem()
                            {
                                Values = new object[]
                                {
                                    false, // column 0: Selected checkbox
                                    null, // column 1: Open link
                                    count,
                                    filePath
                                }
                            });
                    }
                }
                catch
                {
                    // Regex timeout, permission issue, whatever else, just skip the file..
                }
            }

            UpdateResultExplorer();

            try
            {
                foreach (string directoryPath in Directory.GetDirectories(path))
                {
                    SearchDirectory(level + 1, directoryPath, treeGridItemCollection);
                }
            }
            catch
            {
                // No permission to list directories, etc.
            }
        }

        private List<string> GetMatchingFiles(string path)
        {
            List<string> filePaths = new List<string>();
            try
            {
                foreach (string filePath in Directory.GetFiles(path))
                {
                    string filename = Path.GetFileName(filePath);
                    try
                    {
                        if (string.IsNullOrWhiteSpace(_filenamePattern) || _filenameRegex.IsMatch(filename))
                        {
                            filePaths.Add(filePath);
                        }
                    }
                    catch
                    {
                        // Regex timeout
                    }
                }
            }
            catch
            {
                // No permission to list files, etc.
            }

            return filePaths;
        }

        private void UpdateResultExplorer()
        {
            if (DateTime.UtcNow - _lastTreeGridViewRefresh > TimeSpan.FromSeconds(1))
            {
                Application.Instance.Invoke(() =>
                {
                    tvwResultExplorer.ReloadData();
                    if (tvwResultExplorer.DataStore.Count > 0)
                    {
                        tvwResultExplorer.ScrollToRow(tvwResultExplorer.DataStore.Count - 1);
                    }
                });

                _lastTreeGridViewRefresh = DateTime.UtcNow;
            }
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
            foreach (var item in tvwResultExplorer.DataStore as TreeGridItemCollection)
            {
                (item as TreeGridItem).SetValue(0, value);
            }

            tvwResultExplorer.ReloadData();
        }

        private void HandleInvertSelection(object sender, EventArgs e)
        {
            foreach (var item in tvwResultExplorer.DataStore as TreeGridItemCollection)
            {
                var row = (item as TreeGridItem);
                row.SetValue(0, !(bool)row.GetValue(0));
            }

            tvwResultExplorer.ReloadData();
        }

        private void HandleOpenSelected(object sender, EventArgs e)
        {
            if (!CheckEditor())
                return;

            List<string> filesToOpen = new List<string>();
            foreach (var item in tvwResultExplorer.DataStore as TreeGridItemCollection)
            {
                var row = (item as TreeGridItem);
                bool isSelected = (bool)row.GetValue(0);
                if (isSelected)
                {
                    filesToOpen.Add((string)row.GetValue(3));
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
            if (_matchNumberOrdering)
            {
                tvwResultExplorer.DataStore = new TreeGridItemCollection((tvwResultExplorer.DataStore as TreeGridItemCollection)
                    .OrderBy(i => (int)(i as TreeGridItem).GetValue(2)));
            }
            else
            {
                tvwResultExplorer.DataStore = new TreeGridItemCollection((tvwResultExplorer.DataStore as TreeGridItemCollection)
                    .OrderByDescending(i => (int)(i as TreeGridItem).GetValue(2)));
            }

            _matchNumberOrdering = !_matchNumberOrdering;

            if (tvwResultExplorer.DataStore.Count > 0)
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
