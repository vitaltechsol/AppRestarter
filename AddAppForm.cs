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

        public AddAppForm(
            ApplicationDetails existing = null,
            int index = -1,
            Func<List<string>> getGroups = null,
            Action manageGroups = null
        )
        {
            InitializeComponent();

            _getGroups = getGroups ?? (() => new List<string>());
            _manageGroups = manageGroups ?? (() => { });



            // Populate groups combo
            LoadGroupsIntoCombo();

            if (existing != null)
            {
                AppData = existing;

                txtName.Text = existing.Name;
                txtProcess.Text = existing.ProcessName;
                txtPath.Text = existing.RestartPath;
                txtClientIP.Text = existing.ClientIP;
                chkAutoStart.Checked = existing.AutoStart;
                numDelay.Value = existing.AutoStartDelayInSeconds;
                chkNoWarn.Checked = existing.NoWarn;
                chkStartMinimized.Checked = existing.StartMinimized;

                // select existing group if present
                var g = existing.GroupName;
                SelectGroupInCombo(g);
                btnDelete.Visible = true;
            }
            else
            {
                AppData = new ApplicationDetails();
                btnDelete.Visible = false;
                // default None
                SelectGroupInCombo(null);
            }
        }

        private void LoadGroupsIntoCombo()
        {
            var groups = _getGroups().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

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

        // NEW: Manage Groups button → open manager via Form1 callback, then refresh combo
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
            AppData.ClientIP = txtClientIP.Text.Trim();
            AppData.AutoStart = chkAutoStart.Checked;
            AppData.AutoStartDelayInSeconds = (int)numDelay.Value;
            AppData.StartMinimized = chkStartMinimized.Checked;
            AppData.NoWarn = chkNoWarn.Checked;

            var sel = cmbGroup.SelectedItem?.ToString();
            AppData.GroupName = (string.Equals(sel, "None", StringComparison.OrdinalIgnoreCase) ? null : sel);

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
