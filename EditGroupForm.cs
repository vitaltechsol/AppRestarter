using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class EditGroupForm : Form
    {
        private readonly string _originalGroupName;
        private readonly List<GroupDetails> _allGroups;

        public GroupDetails GroupDetails { get; private set; }

        public EditGroupForm(GroupDetails group, List<GroupDetails> allGroups)
        {
            InitializeComponent();

            _originalGroupName = group?.Name ?? "";
            _allGroups = allGroups ?? new List<GroupDetails>();

            GroupDetails = new GroupDetails
            {
                Name = group?.Name ?? "",
                DontWarn = group?.DontWarn ?? false
            };

            txtGroupName.Text = GroupDetails.Name;
            chkDontWarn.Checked = GroupDetails.DontWarn;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var newName = txtGroupName.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Group name cannot be empty.", "Validation", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if name changed and if new name conflicts with existing groups
            if (!string.Equals(newName, _originalGroupName, StringComparison.OrdinalIgnoreCase))
            {
                if (_allGroups.Any(g => g.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("A group with that name already exists.", "Validation", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            GroupDetails.Name = newName;
            GroupDetails.DontWarn = chkDontWarn.Checked;

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
