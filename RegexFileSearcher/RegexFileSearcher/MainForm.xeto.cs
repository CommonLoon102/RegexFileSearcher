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
    public class MainForm : Form
    {
        #region Controls
        private readonly TextBox txtFilenameRegex;
        private readonly CheckBox chkCompiled;
        private readonly CheckBox chkCultureInvariant;
        private readonly CheckBox chkEcmaScript;
        private readonly CheckBox chkExplicitCapture;
        private readonly CheckBox chkIgnoreWhite;
        private readonly CheckBox chkIgnoreCase;
        private readonly CheckBox chkMultiline;
        private readonly CheckBox chkRightToLeft;
        private readonly CheckBox chkSingleLine;
        private readonly NumericStepper nudTimeout;
        private readonly TextBox txtContentRegex;
        private readonly CheckBox chkContentCompiled;
        private readonly CheckBox chkContentCultureInvariant;
        private readonly CheckBox chkContentEcmaScript;
        private readonly CheckBox chkContentExplicitCapture;
        private readonly CheckBox chkContentIgnoreWhite;
        private readonly CheckBox chkContentIgnoreCase;
        private readonly CheckBox chkContentMultiline;
        private readonly CheckBox chkContentRightToLeft;
        private readonly CheckBox chkContentSingleLine;
        private readonly NumericStepper nudContentTimeout;
        private readonly FilePicker fpSearchPath;
        private readonly Button btnStartSearch;
        private readonly FilePicker fpOpenWith;
        private readonly Button btnSelectAll;
        private readonly Button btnSelectNone;
        private readonly Button btnInvertSelection;
        private readonly Button btnOpenSelected;
        private readonly Button btnOrderByMatches;
        private readonly TreeGridView tvwResultExplorer;
        private readonly TextBox txtPath;
        private readonly Label lblStatus;
        #endregion // Controls

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

        public MainForm()
        {
            XamlReader.Load(this);
            #region Initialize Controls
            txtFilenameRegex = FindChild<TextBox>("txtFilenameRegex");
            chkCompiled = FindChild<CheckBox>("chkCompiled");
            chkCultureInvariant = FindChild<CheckBox>("chkCultureInvariant");
            chkEcmaScript = FindChild<CheckBox>("chkEcmaScript");
            chkExplicitCapture = FindChild<CheckBox>("chkExplicitCapture");
            chkIgnoreWhite = FindChild<CheckBox>("chkIgnoreWhite");
            chkIgnoreCase = FindChild<CheckBox>("chkIgnoreCase");
            chkMultiline = FindChild<CheckBox>("chkMultiline");
            chkRightToLeft = FindChild<CheckBox>("chkRightToLeft");
            chkSingleLine = FindChild<CheckBox>("chkSingleLine");
            nudTimeout = FindChild<NumericStepper>("nudTimeout");
            txtContentRegex = FindChild<TextBox>("txtContentRegex");
            chkContentCompiled = FindChild<CheckBox>("chkContentCompiled");
            chkContentCultureInvariant = FindChild<CheckBox>("chkContentCultureInvariant");
            chkContentEcmaScript = FindChild<CheckBox>("chkContentEcmaScript");
            chkContentExplicitCapture = FindChild<CheckBox>("chkContentExplicitCapture");
            chkContentIgnoreWhite = FindChild<CheckBox>("chkContentIgnoreWhite");
            chkContentIgnoreCase = FindChild<CheckBox>("chkContentIgnoreCase");
            chkContentMultiline = FindChild<CheckBox>("chkContentMultiline");
            chkContentRightToLeft = FindChild<CheckBox>("chkContentRightToLeft");
            chkContentSingleLine = FindChild<CheckBox>("chkContentSingleLine");
            nudContentTimeout = FindChild<NumericStepper>("nudContentTimeout");
            fpSearchPath = FindChild<FilePicker>("fpSearchPath");
            btnStartSearch = FindChild<Button>("btnStartSearch");
            fpOpenWith = FindChild<FilePicker>("fpOpenWith");
            btnSelectAll = FindChild<Button>("btnSelectAll");
            btnSelectNone = FindChild<Button>("btnSelectNone");
            btnInvertSelection = FindChild<Button>("btnInvertSelection");
            btnOpenSelected = FindChild<Button>("btnOpenSelected");
            btnOrderByMatches = FindChild<Button>("btnOrderByMatches");
            tvwResultExplorer = FindChild<TreeGridView>("tvwResultExplorer");
            txtPath = FindChild<TextBox>("txtPath");
            lblStatus = FindChild<Label>("lblStatus");
            #endregion // Initialize Controls

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
                            Style = "primary-link-btn",
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
            try
            {
                string searchPath = fpSearchPath.FilePath.Trim();
                if (!Directory.Exists(searchPath))
                    return;

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
                await Task.Factory.StartNew(() => SearchDirectory(searchPath, treeGridItemCollection),
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

        private void SearchDirectory(string path, TreeGridItemCollection treeGridItemCollection)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            Application.Instance.Invoke(() => lblStatus.Text = path);
            List<string> filePaths = GetMatchingFiles(path);
            foreach (string filePath in filePaths.OrderBy(f => f))
            {
                try
                {
                    MatchCollection matches = _contentRegex.Matches(File.ReadAllText(filePath));
                    if (matches.Count > 0)
                    {
                        treeGridItemCollection.Add(
                            new TreeGridItem()
                            {
                                Values = new object[]
                                {
                                    false, // column 0: Selected checkbox
                                    null, // column 1: Open link
                                    matches.Count,
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

            foreach (string directoryPath in Directory.GetDirectories(path))
            {
                SearchDirectory(directoryPath, treeGridItemCollection);
            }
        }

        private List<string> GetMatchingFiles(string path)
        {
            List<string> filePaths = new List<string>();
            foreach (string filePath in Directory.GetFiles(path))
            {
                string filename = Path.GetFileName(filePath);
                try
                {
                    if (_filenameRegex.IsMatch(filename))
                    {
                        filePaths.Add(filePath);
                    }
                }
                catch
                {
                    // Regex timeout
                }
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
