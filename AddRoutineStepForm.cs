using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AppRestarter.Models;

namespace AppRestarter
{
    public partial class AddRoutineStepForm : Form
    {
        public RoutineStep StepData { get; private set; }
        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly List<GroupDetails> _groups;

        public AddRoutineStepForm(
            RoutineStep existing = null,
            List<ApplicationDetails> apps = null,
            List<PcInfo> pcs = null,
            List<GroupDetails> groups = null)
        {
            InitializeComponent();

            // Apply current font settings
            FontManager.ConfigureFormForScaling(this);

            _apps = apps ?? new List<ApplicationDetails>();
            _pcs = pcs ?? new List<PcInfo>();
            _groups = groups ?? new List<GroupDetails>();

            StepData = existing ?? new RoutineStep { Action = new RoutineAction() };

            LoadWaitConditions();
            LoadAction();
        }

        private void LoadWaitConditions()
        {
            // Clear existing wait conditions
            lstWaitConditions.Items.Clear();

            if (StepData.WaitConditions != null && StepData.WaitConditions.Any())
            {
                foreach (var wait in StepData.WaitConditions)
                {
                    lstWaitConditions.Items.Add(FormatWaitCondition(wait));
                }
            }
        }

        private string FormatWaitCondition(WaitCondition wait)
        {
            switch (wait.Type)
            {
                case WaitType.None:
                    return "No wait";
                case WaitType.TimeDelay:
                    return $"Wait {wait.WaitTimeSeconds} seconds";
                case WaitType.AppRunning:
                    var app = _apps.FirstOrDefault(a => a.Name == wait.AppId);
                    return $"Wait for '{wait.AppId}' to be running (timeout: {wait.AppStartTimeoutSeconds}s)";
                default:
                    return "Unknown wait";
            }
        }

        private void LoadAction()
        {
            if (StepData.Action == null)
            {
                StepData.Action = new RoutineAction();
            }

            // Load action type
            cboActionType.Items.Clear();
            cboActionType.Items.Add("Start");
            cboActionType.Items.Add("Restart");
            cboActionType.Items.Add("Stop");
            cboActionType.Items.Add("Keyboard Shortcut");
            cboActionType.Items.Add("Click Area");

            switch (StepData.Action.Type)
            {
                case ActionType.Start:
                    cboActionType.SelectedIndex = 0;
                    break;
                case ActionType.Restart:
                    cboActionType.SelectedIndex = 1;
                    break;
                case ActionType.Stop:
                    cboActionType.SelectedIndex = 2;
                    break;
                case ActionType.KeyboardShortcut:
                    cboActionType.SelectedIndex = 3;
                    break;
                case ActionType.ClickArea:
                    cboActionType.SelectedIndex = 4;
                    break;
                default:
                    cboActionType.SelectedIndex = 0;
                    break;
            }

            // Load target type
            cboTargetType.Items.Clear();
            cboTargetType.Items.Add("App");
            cboTargetType.Items.Add("PC");
            cboTargetType.Items.Add("Group");

            switch (StepData.Action.TargetType)
            {
                case TargetType.App:
                    cboTargetType.SelectedIndex = 0;
                    break;
                case TargetType.PC:
                    cboTargetType.SelectedIndex = 1;
                    break;
                case TargetType.Group:
                    cboTargetType.SelectedIndex = 2;
                    break;
                default:
                    cboTargetType.SelectedIndex = 0;
                    break;
            }

            LoadTargets();

            txtKeys.Text = StepData.Action.Keys ?? "";
            numClickX.Value = StepData.Action.ClickX;
            numClickY.Value = StepData.Action.ClickY;

            UpdateActionControls();
        }

        private void LoadTargets()
        {
            cboTarget.Items.Clear();

            var targetType = cboTargetType.SelectedIndex == 0 ? TargetType.App :
                             cboTargetType.SelectedIndex == 1 ? TargetType.PC : TargetType.Group;

            switch (targetType)
            {
                case TargetType.App:
                    foreach (var app in _apps.OrderBy(a => a.Name))
                        cboTarget.Items.Add(app.Name);
                    break;
                case TargetType.PC:
                    foreach (var pc in _pcs.OrderBy(p => p.Name))
                        cboTarget.Items.Add(pc.Name);
                    break;
                case TargetType.Group:
                    foreach (var group in _groups.OrderBy(g => g.Name))
                        cboTarget.Items.Add(group.Name);
                    break;
            }

            // Try to select existing target
            if (!string.IsNullOrWhiteSpace(StepData.Action.TargetId))
            {
                var index = cboTarget.Items.IndexOf(StepData.Action.TargetId);
                if (index >= 0)
                    cboTarget.SelectedIndex = index;
            }
        }

        private void cboActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionControls();
        }

        private void cboTargetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTargets();
        }

        private void UpdateActionControls()
        {
            var selectedIndex = cboActionType.SelectedIndex;

            // Show/hide based on action type
            bool showTarget = selectedIndex >= 0 && selectedIndex <= 2; // Start/Restart/Stop
            bool showKeys = selectedIndex == 3; // Keyboard Shortcut
            bool showClick = selectedIndex == 4; // Click Area

            lblTarget.Visible = showTarget;
            lblTargetType.Visible = showTarget;
            cboTarget.Visible = showTarget;
            cboTargetType.Visible = showTarget;

            lblKeys.Visible = showKeys;
            txtKeys.Visible = showKeys;

            lblClickX.Visible = showClick;
            numClickX.Visible = showClick;
            lblClickY.Visible = showClick;
            numClickY.Visible = showClick;
        }

        private void btnAddWait_Click(object sender, EventArgs e)
        {
            using var dlg = new AddWaitConditionForm(_apps);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StepData.WaitConditions.Add(dlg.WaitData);
                LoadWaitConditions();
            }
        }

        private void btnRemoveWait_Click(object sender, EventArgs e)
        {
            if (lstWaitConditions.SelectedIndex >= 0 && lstWaitConditions.SelectedIndex < StepData.WaitConditions.Count)
            {
                StepData.WaitConditions.RemoveAt(lstWaitConditions.SelectedIndex);
                LoadWaitConditions();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Save action
            var actionIndex = cboActionType.SelectedIndex;
            StepData.Action.Type = actionIndex switch
            {
                0 => ActionType.Start,
                1 => ActionType.Restart,
                2 => ActionType.Stop,
                3 => ActionType.KeyboardShortcut,
                4 => ActionType.ClickArea,
                _ => ActionType.Start
            };

            var targetTypeIndex = cboTargetType.SelectedIndex;
            StepData.Action.TargetType = targetTypeIndex switch
            {
                0 => TargetType.App,
                1 => TargetType.PC,
                2 => TargetType.Group,
                _ => TargetType.App
            };

            StepData.Action.TargetId = cboTarget.SelectedItem?.ToString() ?? "";
            StepData.Action.Keys = txtKeys.Text;
            StepData.Action.ClickX = (int)numClickX.Value;
            StepData.Action.ClickY = (int)numClickY.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // Simple dialog to add a wait condition
    public class AddWaitConditionForm : Form
    {
        private ComboBox cboWaitType;
        private NumericUpDown numSeconds;
        private ComboBox cboApp;
        private NumericUpDown numTimeout;
        private Button btnOk;
        private Button btnCancel;
        private Label lblWaitType;
        private Label lblSeconds;
        private Label lblApp;
        private Label lblTimeout;

        public WaitCondition WaitData { get; private set; } = new WaitCondition();
        private readonly List<ApplicationDetails> _apps;

        public AddWaitConditionForm(List<ApplicationDetails> apps)
        {
            _apps = apps ?? new List<ApplicationDetails>();
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Add Wait Condition";
            Width = 400;
            Height = 280;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            lblWaitType = new Label { Text = "Wait Type:", Left = 12, Top = 12, Width = 100 };
            cboWaitType = new ComboBox { Left = 120, Top = 12, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cboWaitType.Items.Add("None");
            cboWaitType.Items.Add("Time Delay (seconds)");
            cboWaitType.Items.Add("Wait for App to be running");
            cboWaitType.SelectedIndex = 0;
            cboWaitType.SelectedIndexChanged += CboWaitType_SelectedIndexChanged;

            lblSeconds = new Label { Text = "Seconds:", Left = 12, Top = 42, Width = 100 };
            numSeconds = new NumericUpDown { Left = 120, Top = 42, Width = 100, Maximum = 3600, Minimum = 1, Value = 5 };

            lblApp = new Label { Text = "App:", Left = 12, Top = 72, Width = 100 };
            cboApp = new ComboBox { Left = 120, Top = 72, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };

            lblTimeout = new Label { Text = "Timeout (s):", Left = 12, Top = 102, Width = 100 };
            numTimeout = new NumericUpDown { Left = 120, Top = 102, Width = 100, Maximum = 3600, Minimum = 0, Value = 30 };

            btnOk = new Button { Text = "OK", Left = 200, Top = 200, Width = 80, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button { Text = "Cancel", Left = 290, Top = 200, Width = 80, DialogResult = DialogResult.Cancel };

            Controls.Add(lblWaitType);
            Controls.Add(cboWaitType);
            Controls.Add(lblSeconds);
            Controls.Add(numSeconds);
            Controls.Add(lblApp);
            Controls.Add(cboApp);
            Controls.Add(lblTimeout);
            Controls.Add(numTimeout);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            UpdateVisibility();
        }

        private void LoadData()
        {
            foreach (var app in _apps.OrderBy(a => a.Name))
                cboApp.Items.Add(app.Name);
        }

        private void CboWaitType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var selectedIndex = cboWaitType.SelectedIndex;

            bool showSeconds = selectedIndex == 1;
            bool showApp = selectedIndex == 2;

            lblSeconds.Visible = showSeconds;
            numSeconds.Visible = showSeconds;

            lblApp.Visible = showApp;
            cboApp.Visible = showApp;
            lblTimeout.Visible = showApp;
            numTimeout.Visible = showApp;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var selectedIndex = cboWaitType.SelectedIndex;

            WaitData.Type = selectedIndex switch
            {
                0 => WaitType.None,
                1 => WaitType.TimeDelay,
                2 => WaitType.AppRunning,
                _ => WaitType.None
            };

            WaitData.WaitTimeSeconds = (int)numSeconds.Value;
            WaitData.AppId = cboApp.SelectedItem?.ToString() ?? "";
            WaitData.AppStartTimeoutSeconds = (int)numTimeout.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
