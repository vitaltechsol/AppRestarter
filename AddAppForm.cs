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
            List<PcInfo> pcs = null
        )
        {
            InitializeComponent();

            _getGroups = getGroups ?? (() => new List<string>());
            _manageGroups = manageGroups ?? (() => { });
            _pcs = pcs ?? new List<PcInfo>();

            // Populate groups combo
            LoadGroupsIntoCombo();

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

                // select existing group if present
                SelectGroupInCombo(existing.GroupName);

                // select existing client IP in dropdown
                InitializePcDropdown(existing.ClientIP);

                btnDelete.Visible = true;
            }
            else
            {
                AppData = new ApplicationDetails();
                btnDelete.Visible = false;

                // default None
                SelectGroupInCombo(null);

                // default PC: This PC (empty IP)
                InitializePcDropdown(null);
            }
        }

        private void LoadGroupsIntoCombo()
        {
            var groups = _getGroups()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            cmbGroup.BeginUpdate();
            cmbGroup.Items.Clear();
            cmbGroup.Items.Add("None");
            foreach (var g in groups) cmbGroup.Items.Add(g);
            cmbGroup.EndUpdate();
        }

        private void SelectGroupInCombo(string groupNameOrNull)
        {
            if (string.IsNullOrWhiteSpace(groupNameOrNull))
            {
                // None
                cmbGroup.SelectedIndex = 0;
            }
            else
            {
                int idx = cmbGroup.FindStringExact(groupNameOrNull);
                cmbGroup.SelectedIndex = (idx >= 0) ? idx : 0; // default to None if missing
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

        // Manage Groups button → open manager via Form1 callback, then refresh combo
        private void btnManageGroups_Click(object sender, EventArgs e)
        {
            // Remember current selection
            var current = (cmbGroup.SelectedIndex <= 0) ? null : cmbGroup.SelectedItem?.ToString();

            // Open the manager in Form1 (centralized save/sync)
            _manageGroups?.Invoke();

            // Reload fresh groups & reselect previous if still present
            LoadGroupsIntoCombo();
            SelectGroupInCombo(current);
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

            var sel = cmbGroup.SelectedItem?.ToString();
            AppData.GroupName = (string.Equals(sel, "None", StringComparison.OrdinalIgnoreCase) ? null : sel);

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
            using var dlg = new OpenFileDialog();
            dlg.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = dlg.FileName;
            }
        }

    }
}
