using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Linq;

namespace AppRestarter
{
    public partial class AddAppForm : Form
    {
        public ApplicationDetails AppData { get; private set; }

        private bool isEditMode;
        private int editIndex;

        public bool DeleteRequested { get; private set; } = false;

        public AddAppForm(ApplicationDetails existing = null, int index = -1)
        {
            InitializeComponent();

            if (existing != null)
            {
                isEditMode = true;
                editIndex = index;
                AppData = existing;

                // Populate fields
                txtName.Text = existing.Name;
                txtProcess.Text = existing.ProcessName;
                txtPath.Text = existing.RestartPath;
                txtClientIP.Text = existing.ClientIP;
                chkAutoStart.Checked = existing.AutoStart;
                numDelay.Value = existing.AutoStartDelayInSeconds;
                chkNoWarn.Checked = existing.NoWarn;
                chkStartMinimized.Checked = existing.StartMinimized;

                btnDelete.Visible = true;
            }
            else
            {
                AppData = new ApplicationDetails();
                btnDelete.Visible = false;
            }
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