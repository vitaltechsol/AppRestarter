using System;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class SettingsForm : Form
    {
        public AppSettings Updated { get; private set; }

        public SettingsForm(AppSettings current)
        {
            InitializeComponent();

            // Prime UI from current settings
            numAppPort.Value = Math.Max(numAppPort.Minimum, Math.Min(numAppPort.Maximum, current.AppPort));
            numWebPort.Value = Math.Max(numWebPort.Minimum, Math.Min(numWebPort.Maximum, current.WebPort));
            chkAutoStart.Checked = current.AutoStartWithWindows;
            chkStartMin.Checked = current.StartMinimized;
            chkCheckUpdates.Checked = current.CheckForUpdatesOnStart;

            Updated = new AppSettings
            {
                AppPort = current.AppPort,
                WebPort = current.WebPort,
                AutoStartWithWindows = current.AutoStartWithWindows,
                StartMinimized = current.StartMinimized,
                CheckForUpdatesOnStart = current.CheckForUpdatesOnStart,
                Schema = current.Schema
            };
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate ports (extra hardening if needed)
            if (numAppPort.Value == numWebPort.Value)
            {
                MessageBox.Show("AppPort and WebPort must be different.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Updated.AppPort = (int)numAppPort.Value;
            Updated.WebPort = (int)numWebPort.Value;
            Updated.AutoStartWithWindows = chkAutoStart.Checked;
            Updated.StartMinimized = chkStartMin.Checked;
            Updated.CheckForUpdatesOnStart = chkCheckUpdates.Checked;

            this.DialogResult = DialogResult.OK;
            Close();
        }

        private async void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            btnCheckUpdate.Enabled = false;
            try
            {
                await AutoUpdater.CheckForUpdatesAsync(Application.ProductVersion, manualCheck: true);
            }
            finally
            {
                btnCheckUpdate.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
