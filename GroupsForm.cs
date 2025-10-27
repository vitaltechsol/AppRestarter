using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AppRestarter
{
    public partial class GroupsForm : Form
    {
        public List<string> Groups { get; private set; }

        public GroupsForm(IEnumerable<string> existingGroups)
        {
            InitializeComponent();
            Groups = existingGroups?.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList() ?? new List<string>();
            RefreshList();
        }

        private void RefreshList()
        {
            lstGroups.BeginUpdate();
            lstGroups.Items.Clear();
            foreach (var g in Groups) lstGroups.Items.Add(g);
            lstGroups.EndUpdate();
            txtName.Clear();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            if (!Groups.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Groups.Add(name);
                Groups = Groups.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                RefreshList();
            }
            else
            {
                MessageBox.Show("Group already exists.", "Groups", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedItem is string oldName)
            {
                var newName = txtName.Text.Trim();
                if (string.IsNullOrWhiteSpace(newName)) return;
                if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

                if (Groups.Contains(newName, StringComparer.OrdinalIgnoreCase))
                {
                    MessageBox.Show("A group with that name already exists.", "Groups", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Groups.Remove(oldName);
                Groups.Add(newName);
                Groups = Groups.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                RefreshList();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedItem is string toDelete)
            {
                if (MessageBox.Show($"Delete group '{toDelete}'?\nApps referencing it will not be changed by this dialog.",
                        "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Groups.RemoveAll(g => g.Equals(toDelete, StringComparison.OrdinalIgnoreCase));
                    RefreshList();
                }
            }
        }

        private void lstGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstGroups.SelectedItem is string g) txtName.Text = g;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
