using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppRestarter.Models;

namespace AppRestarter
{
    public partial class AddRoutineForm : Form
    {
        public enum RoutineMode
        {
            Local,
            Remote
        }

        public Routine RoutineData { get; private set; }
        public RemoteRoutineReference RemoteRoutineData { get; private set; }
        public RoutineMode Mode { get; private set; }
        public bool DeleteRequested { get; private set; } = false;

        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly List<GroupDetails> _groups;
        private readonly int _webPort;
        private bool _isEditMode = false;

        // For creating new routine
        public AddRoutineForm(
            List<ApplicationDetails> apps,
            List<PcInfo> pcs,
            List<GroupDetails> groups,
            int webPort = 8081)
        {
            InitializeComponent();
            FontManager.ConfigureFormForScaling(this);

            _apps = apps ?? new List<ApplicationDetails>();
            _pcs = pcs ?? new List<PcInfo>();
            _groups = groups ?? new List<GroupDetails>();
            _webPort = webPort;

            Mode = RoutineMode.Local; // Default to local
            RoutineData = new Routine();
            RemoteRoutineData = null;

            _isEditMode = false;
            btnDelete.Visible = false;
            Text = "Add New Routine";

            InitializeForm();
        }

        // For editing existing local routine
        public AddRoutineForm(
            Routine existing,
            List<ApplicationDetails> apps,
            List<PcInfo> pcs,
            List<GroupDetails> groups,
            int webPort = 8081)
        {
            InitializeComponent();
            FontManager.ConfigureFormForScaling(this);

            _apps = apps ?? new List<ApplicationDetails>();
            _pcs = pcs ?? new List<PcInfo>();
            _groups = groups ?? new List<GroupDetails>();
            _webPort = webPort;

            Mode = RoutineMode.Local;
            RoutineData = existing ?? new Routine();
            RemoteRoutineData = null;

            _isEditMode = true;
            btnDelete.Visible = true;
            Text = "Edit Local Routine";

            InitializeForm();

            txtName.Text = existing.Name;
            LoadSteps();
        }

        // For editing existing remote routine reference
        public AddRoutineForm(
            RemoteRoutineReference existingRemote,
            List<ApplicationDetails> apps,
            List<PcInfo> pcs,
            List<GroupDetails> groups,
            int webPort = 8081)
        {
            InitializeComponent();
            FontManager.ConfigureFormForScaling(this);

            _apps = apps ?? new List<ApplicationDetails>();
            _pcs = pcs ?? new List<PcInfo>();
            _groups = groups ?? new List<GroupDetails>();
            _webPort = webPort;

            Mode = RoutineMode.Remote;
            RoutineData = null;
            RemoteRoutineData = existingRemote ?? new RemoteRoutineReference();

            _isEditMode = true;
            btnDelete.Visible = true;
            Text = "Edit Remote Routine Reference";

            InitializeForm();

            // Pre-fill remote routine info
            if (existingRemote != null)
            {
                lblRemoteInfo.Text = $"Remote: {existingRemote.CachedName}\nFrom: {existingRemote.RemoteHost}:{existingRemote.RemotePort}";

                // Try to select the PC in the combo
                var pcIndex = _pcs.FindIndex(p => p.IP == existingRemote.RemoteHost);
                if (pcIndex >= 0)
                {
                    cmbRemotePc.SelectedIndex = pcIndex;
                }
            }
        }

        private void InitializeForm()
        {
            // Populate PC combo box
            cmbRemotePc.Items.Clear();
            foreach (var pc in _pcs.Where(p => p.Enabled))
            {
                cmbRemotePc.Items.Add($"{pc.Name} ({pc.IP})");
            }

            if (cmbRemotePc.Items.Count > 0)
                cmbRemotePc.SelectedIndex = 0;

            // Set initial radio button state
            if (Mode == RoutineMode.Local)
            {
                rdoLocal.Checked = true;
            }
            else
            {
                rdoRemote.Checked = true;
            }

            // If editing, lock the mode
            if (_isEditMode)
            {
                rdoLocal.Enabled = false;
                rdoRemote.Enabled = false;
            }

            UpdateUIForMode();
        }

        private void rdoLocal_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoLocal.Checked)
            {
                Mode = RoutineMode.Local;
                UpdateUIForMode();
            }
        }

        private void rdoRemote_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoRemote.Checked)
            {
                Mode = RoutineMode.Remote;
                UpdateUIForMode();
            }
        }

        private void UpdateUIForMode()
        {
            if (Mode == RoutineMode.Local)
            {
                // Show local routine controls
                lblName.Visible = true;
                txtName.Visible = true;
                groupSteps.Visible = true;

                // Hide remote routine controls
                lblRemotePc.Visible = false;
                cmbRemotePc.Visible = false;
                btnBrowseRemote.Visible = false;
                lblRemoteInfo.Visible = false;
            }
            else
            {
                // Hide local routine controls
                lblName.Visible = false;
                txtName.Visible = false;
                groupSteps.Visible = false;

                // Show remote routine controls
                lblRemotePc.Visible = true;
                cmbRemotePc.Visible = true;
                btnBrowseRemote.Visible = true;
                lblRemoteInfo.Visible = true;
            }
        }

        private void LoadSteps()
        {
            lstSteps.Items.Clear();

            if (RoutineData.Steps != null && RoutineData.Steps.Any())
            {
                for (int i = 0; i < RoutineData.Steps.Count; i++)
                {
                    var step = RoutineData.Steps[i];
                    lstSteps.Items.Add($"Step {i + 1}: {FormatStep(step)}");
                }
            }
        }

        private string FormatStep(RoutineStep step)
        {
            if (step == null || step.Action == null)
                return "Empty step";

            var waitDesc = step.WaitConditions != null && step.WaitConditions.Any()
                ? $"{step.WaitConditions.Count} wait(s), then "
                : "";

            var actionDesc = step.Action.Type switch
            {
                ActionType.Start => $"Start {step.Action.TargetType} '{step.Action.TargetId}'",
                ActionType.Restart => $"Restart {step.Action.TargetType} '{step.Action.TargetId}'",
                ActionType.Stop => $"Stop {step.Action.TargetType} '{step.Action.TargetId}'",
                ActionType.KeyboardShortcut => $"Send keys '{step.Action.Keys}'",
                ActionType.ClickArea => !string.IsNullOrWhiteSpace(step.Description) 
                    ? step.Description 
                    : $"Click ({step.Action.ClickX}, {step.Action.ClickY})",
                ActionType.Minimize => $"Minimize {step.Action.TargetType} '{step.Action.TargetId}'",
                _ => "Unknown action"
            };

            return waitDesc + actionDesc;
        }

        private void btnAddStep_Click(object sender, EventArgs e)
        {
            using var dlg = new AddRoutineStepForm(null, _apps, _pcs, _groups);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RoutineData.Steps.Add(dlg.StepData);
                LoadSteps();
            }
        }

        private void btnEditStep_Click(object sender, EventArgs e)
        {
            if (lstSteps.SelectedIndex < 0 || lstSteps.SelectedIndex >= RoutineData.Steps.Count)
            {
                MessageBox.Show("Please select a step to edit.", "Edit Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var index = lstSteps.SelectedIndex;
            using var dlg = new AddRoutineStepForm(RoutineData.Steps[index], _apps, _pcs, _groups);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RoutineData.Steps[index] = dlg.StepData;
                LoadSteps();
            }
        }

        private void btnRemoveStep_Click(object sender, EventArgs e)
        {
            if (lstSteps.SelectedIndex < 0 || lstSteps.SelectedIndex >= RoutineData.Steps.Count)
            {
                MessageBox.Show("Please select a step to remove.", "Remove Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var index = lstSteps.SelectedIndex;
            RoutineData.Steps.RemoveAt(index);
            LoadSteps();
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            var index = lstSteps.SelectedIndex;
            if (index <= 0 || index >= RoutineData.Steps.Count)
                return;

            var temp = RoutineData.Steps[index - 1];
            RoutineData.Steps[index - 1] = RoutineData.Steps[index];
            RoutineData.Steps[index] = temp;

            LoadSteps();
            lstSteps.SelectedIndex = index - 1;
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            var index = lstSteps.SelectedIndex;
            if (index < 0 || index >= RoutineData.Steps.Count - 1)
                return;

            var temp = RoutineData.Steps[index + 1];
            RoutineData.Steps[index + 1] = RoutineData.Steps[index];
            RoutineData.Steps[index] = temp;

            LoadSteps();
            lstSteps.SelectedIndex = index + 1;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (Mode == RoutineMode.Local)
            {
                var name = txtName.Text?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Please enter a routine name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtName.Focus();
                    return;
                }

                if (RoutineData.Steps == null || !RoutineData.Steps.Any())
                {
                    MessageBox.Show("Please add at least one step to the routine.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                RoutineData.Name = name;
                DialogResult = DialogResult.OK;
                Close();
            }
            else // Remote mode
            {
                if (RemoteRoutineData == null)
                {
                    MessageBox.Show("Please browse and select a remote routine.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var itemType = Mode == RoutineMode.Local ? "routine" : "remote routine reference";
            var result = MessageBox.Show(
                $"Are you sure you want to delete this {itemType}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                DeleteRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private async void btnBrowseRemote_Click(object sender, EventArgs e)
        {
            if (cmbRemotePc.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a PC first.", "Select PC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedPc = _pcs.Where(p => p.Enabled).ToList()[cmbRemotePc.SelectedIndex];
            await BrowseRemoteRoutines(selectedPc.IP, selectedPc.Name);
        }

        private async Task BrowseRemoteRoutines(string host, string pcName)
        {
            var routinesUrl = $"http://{host}:{_webPort}/routines";

            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var response = await httpClient.GetAsync(routinesUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch routines: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var routines = System.Text.Json.JsonSerializer.Deserialize<List<Routine>>(json);

                if (routines == null || routines.Count == 0)
                {
                    MessageBox.Show("No routines found on the remote PC.", "No Routines", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Show selection dialog
                using var selectDlg = new Form
                {
                    Width = 600,
                    Height = 400,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    Text = $"Select Routine from {pcName}",
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = System.Drawing.Color.FromArgb(15, 23, 42)
                };

                var listBox = new ListBox
                {
                    Left = 20,
                    Top = 20,
                    Width = 540,
                    Height = 280,
                    BackColor = System.Drawing.Color.FromArgb(30, 41, 59),
                    ForeColor = System.Drawing.Color.FromArgb(226, 232, 240),
                    BorderStyle = BorderStyle.FixedSingle
                };

                foreach (var routine in routines)
                {
                    listBox.Items.Add($"{routine.Name} ({routine.Steps?.Count ?? 0} steps)");
                }

                var selectButton = new Button
                {
                    Text = "Select",
                    Left = 380,
                    Width = 100,
                    Top = 320,
                    BackColor = System.Drawing.Color.FromArgb(23, 219, 228),
                    ForeColor = System.Drawing.Color.FromArgb(2, 6, 23),
                    FlatStyle = FlatStyle.Flat
                };
                selectButton.FlatAppearance.BorderSize = 0;
                selectButton.Click += (s, ev) =>
                {
                    if (listBox.SelectedIndex < 0) return;

                    var selectedRoutine = routines[listBox.SelectedIndex];
                    RemoteRoutineData = new RemoteRoutineReference
                    {
                        RemoteHost = host,
                        RemotePort = _webPort,
                        RemoteRoutineId = selectedRoutine.Id,
                        CachedName = selectedRoutine.Name,
                        LastUpdated = DateTime.Now
                    };

                    lblRemoteInfo.Text = $"Selected: {selectedRoutine.Name}\nFrom: {host}:{_webPort}\n({selectedRoutine.Steps?.Count ?? 0} steps)";
                    selectDlg.DialogResult = DialogResult.OK;
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Left = 490,
                    Width = 70,
                    Top = 320,
                    DialogResult = DialogResult.Cancel,
                    BackColor = System.Drawing.Color.FromArgb(51, 65, 85),
                    ForeColor = System.Drawing.Color.FromArgb(226, 232, 240),
                    FlatStyle = FlatStyle.Flat
                };
                cancelButton.FlatAppearance.BorderSize = 0;

                selectDlg.Controls.Add(listBox);
                selectDlg.Controls.Add(selectButton);
                selectDlg.Controls.Add(cancelButton);

                selectDlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to browse routines from {pcName}::{routinesUrl}\n\n{ex.Message}",
                    "Browse Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
