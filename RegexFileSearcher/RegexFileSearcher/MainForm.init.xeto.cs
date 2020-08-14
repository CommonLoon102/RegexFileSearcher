using Eto.Forms;
using Eto.Serialization.Xaml;

namespace RegexFileSearcher
{
    public partial class MainForm : Form
    {
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
        private readonly ComboBox cboSubdirectories;
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
        private MainForm(bool initializeControls)
        {
            XamlReader.Load(this);
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
            cboSubdirectories = FindChild<ComboBox>("cboSubdirectories");
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
        }
    }
}