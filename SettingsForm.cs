using System;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class SettingsForm : Form
    {
        public AppSettings Updated { get; private set; }

        private AutoUpdater _autoUpdater;

        public SettingsForm(AppSettings current)
        {
            InitializeComponent();

            _autoUpdater = new AutoUpdater(
                "https://api.github.com/repos/vitaltechsol/AppRestarter/releases/latest",
                "AppRestarter",
                "AppRestarter.exe",
                Application.ProductVersion);

            // Prime UI from current settings
            numAppPort.Value = Math.Max(numAppPort.Minimum, Math.Min(numAppPort.Maximum, current.AppPort));
            numWebPort.Value = Math.Max(numWebPort.Minimum, Math.Min(numWebPort.Maximum, current.WebPort));

            // Set combo box value or default to 10
            string fontStr = ((int)current.BaseFontSize).ToString();
            if (cmbFontSize.Items.Contains(fontStr))
            {
                cmbFontSize.SelectedItem = fontStr;
            }
            else
            {
                cmbFontSize.SelectedItem = "10";
            }

            chkAutoStart.Checked = current.AutoStartWithWindows;
            chkStartMin.Checked = current.StartMinimized;
            chkCheckUpdates.Checked = current.CheckForUpdatesOnStart;

            Updated = new AppSettings
            {
                AppPort = current.AppPort,
                WebPort = current.WebPort,
                BaseFontSize = current.BaseFontSize,
                AutoStartWithWindows = current.AutoStartWithWindows,
                StartMinimized = current.StartMinimized,
                CheckForUpdatesOnStart = current.CheckForUpdatesOnStart,
                Schema = current.Schema
            };

            // Apply current font size to this form
            FontManager.BaseFontSize = current.BaseFontSize;
            FontManager.ConfigureFormForScaling(this);
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

            if (float.TryParse(cmbFontSize.SelectedItem?.ToString(), out float parsedFont))
            {
                Updated.BaseFontSize = parsedFont;
            }
            else
            {
                Updated.BaseFontSize = 10.0f;
            }

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
                await _autoUpdater.CheckForUpdatesAsync(manualCheck: true);
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
