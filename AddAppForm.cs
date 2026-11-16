using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class AddAppForm : Form
    {
        public ApplicationDetails AppData { get; private set; }
        public bool DeleteRequested { get; private set; } = false;

        private readonly Func<List<string>> _getGroups;
        private readonly Action _manageGroups;
        private readonly List<PcInfo> _pcs;
        private readonly int _webPort;

        // Inline remote-app dropdown (shown instead of text fields when a remote PC is selected)
        private Label _lblSelectApp;
        private ComboBox _cboRemoteApp;

        // Internal item wrapper for the PC dropdown
        private class PcComboItem
        {
            public string Text { get; set; } = "";
            public string IP { get; set; } = "";

            public override string ToString() => Text;
        }

        // Minimal DTO matching WebServer /apps response
        private class RemoteAppDto
        {
            public string Name { get; set; }
            public string ProcessName { get; set; }
            public string RestartPath { get; set; }

            public override string ToString() => Name ?? "";
        }

        public AddAppForm(
            ApplicationDetails existing = null,
            int index = -1,
            Func<List<string>> getGroups = null,
            Action manageGroups = null,
            List<PcInfo> pcs = null,
            int webPort = 8090
        )
        {
            InitializeComponent();

            // Apply current font settings
            FontManager.ConfigureFormForScaling(this);

            _getGroups = getGroups ?? (() => new List<string>());
            _manageGroups = manageGroups ?? (() => { });
            _pcs = pcs ?? new List<PcInfo>();
            _webPort = webPort;

            // Build the inline "Select App" label + dropdown used for remote PCs.
            // Positioned to exactly overlap the Name / Path / Process rows.
            _lblSelectApp = new Label
            {
                Text = "Select App:",
                Location = lblName.Location,
                Size = lblName.Size,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Visible = false
            };
            _cboRemoteApp = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = txtName.Location,
                Size = new System.Drawing.Size(btnBrowse.Right - txtName.Left, txtName.Height),
                Visible = false
            };
            Controls.Add(_lblSelectApp);
            Controls.Add(_cboRemoteApp);

            // Populate groups checklist
            LoadGroupsIntoList();

            if (existing != null)
            {
                AppData = existing;

                txtName.Text = existing.Name;
                txtProcess.Text = existing.ProcessName;
                txtPath.Text = existing.RestartPath;
                chkAutoStart.Checked = existing.AutoStart;
                numDelay.Value = existing.AutoStartDelayInSeconds;
                chkNoWarn.Checked = existing.NoWarn;
                chkStartMinimized.Checked = existing.StartMinimized;

                // select existing groups if present
                SelectGroupsInList(existing.GroupNames ?? (existing.GroupName != null ? new List<string> { existing.GroupName } : new List<string>()));

                // wire change handler AFTER populate so initial set doesn't trigger a fetch
                InitializePcDropdown(existing.ClientIP);
                cboClientPc.SelectedIndexChanged += CboClientPc_SelectedIndexChanged;
                UpdateFormForPcSelection();

                btnDelete.Visible = true;
            }
            else
            {
                AppData = new ApplicationDetails();
                btnDelete.Visible = false;

                SelectGroupsInList(new List<string>());

                InitializePcDropdown(null);
                cboClientPc.SelectedIndexChanged += CboClientPc_SelectedIndexChanged;
                UpdateFormForPcSelection();
            }
        }

        private void LoadGroupsIntoList()
        {
            var groups = _getGroups()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            chkListGroups.BeginUpdate();
            chkListGroups.Items.Clear();
            foreach (var g in groups) 
            {
                chkListGroups.Items.Add(g);
            }
            chkListGroups.EndUpdate();
        }

        private void SelectGroupsInList(List<string> groupNames)
        {
            if (groupNames == null || !groupNames.Any())
            {
                // Clear all selections
                for (int i = 0; i < chkListGroups.Items.Count; i++)
                {
                    chkListGroups.SetItemChecked(i, false);
                }
            }
            else
            {
                for (int i = 0; i < chkListGroups.Items.Count; i++)
                {
                    var item = chkListGroups.Items[i].ToString();
                    bool shouldCheck = groupNames.Any(g => 
                        string.Equals(g, item, StringComparison.OrdinalIgnoreCase));
                    chkListGroups.SetItemChecked(i, shouldCheck);
                }
            }
        }

        private bool IsRemotePcSelected()
        {
            var item = cboClientPc.SelectedItem as PcComboItem;
            return item != null && !string.IsNullOrWhiteSpace(item.IP);
        }

        private void CboClientPc_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFormForPcSelection();
        }

        private void UpdateFormForPcSelection()
        {
            bool isRemote = IsRemotePcSelected();

            // Local fields
            lblName.Visible = !isRemote;
            txtName.Visible = !isRemote;
            lblPath.Visible = !isRemote;
            txtPath.Visible = !isRemote;
            lblProcess.Visible = !isRemote;
            txtProcess.Visible = !isRemote;
            btnBrowse.Visible = !isRemote;

            // Remote inline dropdown
            _lblSelectApp.Visible = isRemote;
            _cboRemoteApp.Visible = isRemote;

            if (isRemote)
                LoadRemoteAppsIntoCombo();
        }

        private void LoadRemoteAppsIntoCombo()
        {
            var pcItem = cboClientPc.SelectedItem as PcComboItem;
            var targetIp = pcItem?.IP ?? string.Empty;

            _cboRemoteApp.Items.Clear();
            _cboRemoteApp.Items.Add("Loading...");
            _cboRemoteApp.SelectedIndex = 0;
            _cboRemoteApp.Enabled = false;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                List<RemoteAppDto> list = null;
                string error = null;
                try
                {
                    using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var json = client.GetStringAsync($"http://{targetIp}:{_webPort}/apps").GetAwaiter().GetResult();
                    list = System.Text.Json.JsonSerializer.Deserialize<List<RemoteAppDto>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex) { error = ex.Message; }

                this.Invoke((Action)(() =>
                {
                    _cboRemoteApp.Items.Clear();
                    if (error != null)
                    {
                        _cboRemoteApp.Items.Add("(Could not connect)");
                        _cboRemoteApp.SelectedIndex = 0;
                        _cboRemoteApp.Enabled = false;
                        return;
                    }
                    if (list == null || list.Count == 0)
                    {
                        _cboRemoteApp.Items.Add("(No apps found)");
                        _cboRemoteApp.SelectedIndex = 0;
                        _cboRemoteApp.Enabled = false;
                        return;
                    }
                    var ordered = list.OrderBy(a => a.Name ?? "", StringComparer.OrdinalIgnoreCase).ToList();
                    foreach (var a in ordered)
                        _cboRemoteApp.Items.Add(a);
                    _cboRemoteApp.SelectedIndex = 0;
                    _cboRemoteApp.Enabled = true;
                }));
            });
        }

        private void InitializePcDropdown(string selectedIp)
        {
            cboClientPc.BeginUpdate();
            cboClientPc.Items.Clear();

            // First option: This PC (empty ClientIP)
            cboClientPc.Items.Add(new PcComboItem
            {
                Text = "This PC",
                IP = ""
            });

            // Remote PCs from configuration
            foreach (var pc in _pcs)
            {
                if (string.IsNullOrWhiteSpace(pc.IP))
                    continue;

                cboClientPc.Items.Add(new PcComboItem
                {
                    Text = string.IsNullOrWhiteSpace(pc.Name)
                        ? pc.IP
                        : $"{pc.Name} ({pc.IP})",
                    IP = pc.IP
                });
            }

            // Preselect based on existing IP
            if (string.IsNullOrWhiteSpace(selectedIp))
            {
                // This PC
                cboClientPc.SelectedIndex = 0;
            }
            else
            {
                var matchIndex = _pcs.FindIndex(p =>
                    string.Equals(p.IP, selectedIp, StringComparison.OrdinalIgnoreCase));

                // +1 because index 0 is "This PC"
                cboClientPc.SelectedIndex = (matchIndex >= 0) ? matchIndex + 1 : 0;
            }

            cboClientPc.EndUpdate();
        }

        // Manage Groups button → open manager via Form1 callback, then refresh list
        private void btnManageGroups_Click(object sender, EventArgs e)
        {
            // Remember current selections
            var currentSelections = new List<string>();
            foreach (var item in chkListGroups.CheckedItems)
            {
                currentSelections.Add(item.ToString());
            }

            // Open the manager in Form1 (centralized save/sync)
            _manageGroups?.Invoke();

            // Reload fresh groups & reselect previous if still present
            LoadGroupsIntoList();
            SelectGroupsInList(currentSelections);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (IsRemotePcSelected())
            {
                var selected = _cboRemoteApp.SelectedItem as RemoteAppDto;
                if (selected == null)
                {
                    MessageBox.Show("Please select an app from the list.", "App Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AppData.Name = selected.Name ?? string.Empty;
                AppData.ProcessName = selected.ProcessName ?? string.Empty;
                AppData.RestartPath = selected.RestartPath ?? string.Empty;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Please enter an application name.", "Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AppData.Name = txtName.Text.Trim();
                AppData.ProcessName = txtProcess.Text.Trim();
                AppData.RestartPath = txtPath.Text.Trim();
            }

            AppData.AutoStart = chkAutoStart.Checked;
            AppData.AutoStartDelayInSeconds = (int)numDelay.Value;
            AppData.StartMinimized = chkStartMinimized.Checked;
            AppData.NoWarn = chkNoWarn.Checked;

            var selectedGroups = new List<string>();
            foreach (var item in chkListGroups.CheckedItems)
                selectedGroups.Add(item.ToString());

            AppData.GroupNames = selectedGroups;
            AppData.GroupName = selectedGroups.FirstOrDefault();

            var pcItem = cboClientPc.SelectedItem as PcComboItem;
            AppData.ClientIP = pcItem?.IP ?? "";

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this application?", "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                DeleteRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
                txtPath.Text = dlg.FileName;
        }

    }
}
