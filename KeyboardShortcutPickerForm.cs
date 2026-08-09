using System;
using System.Text;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class KeyboardShortcutPickerForm : Form
    {
        public string ShortcutKeys { get; private set; }

        public KeyboardShortcutPickerForm(string existingKeys = "")
        {
            InitializeComponent();
            FontManager.ConfigureFormForScaling(this);

            // Parse existing keys if provided
            if (!string.IsNullOrWhiteSpace(existingKeys))
            {
                ParseExistingKeys(existingKeys);
            }

            UpdatePreview();
        }

        private void ParseExistingKeys(string keys)
        {
            // Try to parse SendKeys format
            chkCtrl.Checked = keys.Contains("^");
            chkAlt.Checked = keys.Contains("%");
            chkShift.Checked = keys.Contains("+");
            chkWin.Checked = keys.Contains("^{ESC}"); // Win key approximation

            // Extract the main key (simplified parsing)
            string mainKey = keys.Replace("^", "").Replace("%", "").Replace("+", "")
                                 .Replace("{", "").Replace("}", "");

            if (!string.IsNullOrWhiteSpace(mainKey) && mainKey.Length > 0)
            {
                // Try to select the key in the combo box
                int index = cboKey.Items.IndexOf(mainKey.ToUpper());
                if (index >= 0)
                    cboKey.SelectedIndex = index;
            }
        }

        private void UpdatePreview()
        {
            var sb = new StringBuilder();

            // Build SendKeys format
            if (chkCtrl.Checked) sb.Append("^");
            if (chkAlt.Checked) sb.Append("%");
            if (chkShift.Checked) sb.Append("+");

            string selectedKey = cboKey.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedKey))
            {
                // Format special keys with braces
                if (selectedKey.Length > 1)
                    sb.Append("{" + selectedKey + "}");
                else
                    sb.Append(selectedKey);
            }

            txtPreview.Text = sb.ToString();
        }

        private void chkModifier_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void cboKey_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cboKey.SelectedItem?.ToString()))
            {
                MessageBox.Show("Please select a key.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShortcutKeys = txtPreview.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
