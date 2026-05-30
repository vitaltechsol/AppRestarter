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

        // Internal item wrapper for the PC dropdown
        private class PcComboItem
        {
            public string Text { get; set; } = "";
            public string IP { get; set; } = "";

            public override string ToString() => Text;
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

                // select existing client IP in dropdown
                InitializePcDropdown(existing.ClientIP);

                btnDelete.Visible = true;
            }
            else
            {
                AppData = new ApplicationDetails();
                btnDelete.Visible = false;

                // default no groups selected
                SelectGroupsInList(new List<string>());

                // default PC: This PC (empty IP)
                InitializePcDropdown(null);
            }

            // UI tweak: change browse button text to "Select App"
            try { btnBrowse.Text = "Select App"; } catch { }
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
            AppData.Name = txtName.Text.Trim();
            AppData.ProcessName = txtProcess.Text.Trim();
            AppData.RestartPath = txtPath.Text.Trim();
            AppData.AutoStart = chkAutoStart.Checked;
            AppData.AutoStartDelayInSeconds = (int)numDelay.Value;
            AppData.StartMinimized = chkStartMinimized.Checked;
            AppData.NoWarn = chkNoWarn.Checked;

            // Collect selected groups
            var selectedGroups = new List<string>();
            foreach (var item in chkListGroups.CheckedItems)
            {
                selectedGroups.Add(item.ToString());
            }

            AppData.GroupNames = selectedGroups;

            // Keep old GroupName for backward compatibility (use first group if any)
            AppData.GroupName = selectedGroups.FirstOrDefault();

            // From PC dropdown: store only IP ("" for This PC)
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
            // If a remote PC is selected, request its apps via HTTP and let user pick.
            var pcItem = cboClientPc.SelectedItem as PcComboItem;
            var targetIp = pcItem?.IP ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(targetIp))
            {
                // Remote: try to GET /apps from the remote's web server
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var url = $"http://{targetIp}:{_webPort}/apps";
                    var resp = client.GetAsync(url).GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Failed to retrieve apps from {targetIp}:{_webPort} (HTTP {resp.StatusCode}).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<RemoteAppDto>>(json);
                    if (list == null || list.Count == 0)
                    {
                        MessageBox.Show($"No apps returned from {targetIp}.", "No Apps", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    using var sel = new SelectAppDialog(list);
                    if (sel.ShowDialog(this) == DialogResult.OK)
                    {
                        txtName.Text = sel.SelectedApp?.Name ?? txtName.Text;
                        txtPath.Text = sel.SelectedApp?.RestartPath ?? txtPath.Text;
                        txtProcess.Text = sel.SelectedApp?.ProcessName ?? txtProcess.Text;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching apps from {targetIp}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return;
            }

            // Local: open file browser as before
            using var dlg = new OpenFileDialog();
            dlg.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = dlg.FileName;
            }
        }

        // Minimal DTO matching WebServer /apps response
        private class RemoteAppDto
        {
            public string Name { get; set; }
            public string ProcessName { get; set; }
            public string RestartPath { get; set; }
        }

        // Simple selection dialog used to pick one app from remote list
        private class SelectAppDialog : Form
        {
            private readonly ComboBox _combo;
            private readonly Button _ok;
            private readonly Button _cancel;
            public RemoteAppDto SelectedApp { get; private set; }

            public SelectAppDialog(List<RemoteAppDto> apps)
            {
                Text = "Select App";
                Width = 420;
                Height = 140;
                StartPosition = FormStartPosition.CenterParent;
                _combo = new ComboBox { Left = 12, Top = 12, Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
                var ordered = (apps ?? new List<RemoteAppDto>())
                    .OrderBy(a => a?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (var a in ordered) _combo.Items.Add(a);
                _combo.DisplayMember = "Name";
                if (_combo.Items.Count > 0) _combo.SelectedIndex = 0;

                _ok = new Button { Text = "OK", Left = 220, Top = 48, Width = 80, DialogResult = DialogResult.OK };
                _cancel = new Button { Text = "Cancel", Left = 312, Top = 48, Width = 80, DialogResult = DialogResult.Cancel };

                Controls.Add(_combo);
                Controls.Add(_ok);
                Controls.Add(_cancel);

                AcceptButton = _ok;
                CancelButton = _cancel;

                _ok.Click += (s, e) =>
                {
                    SelectedApp = _combo.SelectedItem as RemoteAppDto;
                    DialogResult = DialogResult.OK;
                    Close();
                };
            }
        }

    }
}
