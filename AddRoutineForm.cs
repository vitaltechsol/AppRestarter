using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AppRestarter.Models;

namespace AppRestarter
{
    public partial class AddRoutineForm : Form
    {
        public Routine RoutineData { get; private set; }
        public bool DeleteRequested { get; private set; } = false;

        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly List<GroupDetails> _groups;

        public AddRoutineForm(
            Routine existing = null,
            List<ApplicationDetails> apps = null,
            List<PcInfo> pcs = null,
            List<GroupDetails> groups = null)
        {
            InitializeComponent();

            // Apply current font settings
            FontManager.ConfigureFormForScaling(this);

            _apps = apps ?? new List<ApplicationDetails>();
            _pcs = pcs ?? new List<PcInfo>();
            _groups = groups ?? new List<GroupDetails>();

            RoutineData = existing ?? new Routine();

            if (existing != null)
            {
                txtName.Text = existing.Name;
                btnDelete.Visible = true;
                Text = "Edit Routine";
            }
            else
            {
                btnDelete.Visible = false;
                Text = "Add New Routine";
            }

            LoadSteps();
        }

        private void LoadSteps()
        {
            lstSteps.Items.Clear();

            if (RoutineData.Steps != null && RoutineData.Steps.Any())
            {
                for (int i = 0; i < RoutineData.Steps.Count; i++)
                {
                    var step = RoutineData.Steps[i];
                    lstSteps.Items.Add($"Step {i + 1}: {FormatStep(step)}");
                }
            }
        }

        private string FormatStep(RoutineStep step)
        {
            if (step == null || step.Action == null)
                return "Empty step";

            var waitDesc = step.WaitConditions != null && step.WaitConditions.Any()
                ? $"{step.WaitConditions.Count} wait(s), then "
                : "";

            var actionDesc = step.Action.Type switch
            {
                ActionType.Start => $"Start {step.Action.TargetType} '{step.Action.TargetId}'",
                ActionType.Restart => $"Restart {step.Action.TargetType} '{step.Action.TargetId}'",
                ActionType.Stop => $"Stop {step.Action.TargetType} '{step.Action.TargetId}'",
                ActionType.KeyboardShortcut => $"Send keys '{step.Action.Keys}'",
                ActionType.ClickArea => $"Click ({step.Action.ClickX}, {step.Action.ClickY})",
                ActionType.Minimize => $"Minimize {step.Action.TargetType} '{step.Action.TargetId}'",
                _ => "Unknown action"
            };

            return waitDesc + actionDesc;
        }

        private void btnAddStep_Click(object sender, EventArgs e)
        {
            using var dlg = new AddRoutineStepForm(null, _apps, _pcs, _groups);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RoutineData.Steps.Add(dlg.StepData);
                LoadSteps();
            }
        }

        private void btnEditStep_Click(object sender, EventArgs e)
        {
            if (lstSteps.SelectedIndex < 0 || lstSteps.SelectedIndex >= RoutineData.Steps.Count)
            {
                MessageBox.Show("Please select a step to edit.", "Edit Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var index = lstSteps.SelectedIndex;
            using var dlg = new AddRoutineStepForm(RoutineData.Steps[index], _apps, _pcs, _groups);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RoutineData.Steps[index] = dlg.StepData;
                LoadSteps();
            }
        }

        private void btnRemoveStep_Click(object sender, EventArgs e)
        {
            if (lstSteps.SelectedIndex < 0 || lstSteps.SelectedIndex >= RoutineData.Steps.Count)
            {
                MessageBox.Show("Please select a step to remove.", "Remove Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var index = lstSteps.SelectedIndex;
            RoutineData.Steps.RemoveAt(index);
            LoadSteps();
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            var index = lstSteps.SelectedIndex;
            if (index <= 0 || index >= RoutineData.Steps.Count)
                return;

            var temp = RoutineData.Steps[index - 1];
            RoutineData.Steps[index - 1] = RoutineData.Steps[index];
            RoutineData.Steps[index] = temp;

            LoadSteps();
            lstSteps.SelectedIndex = index - 1;
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            var index = lstSteps.SelectedIndex;
            if (index < 0 || index >= RoutineData.Steps.Count - 1)
                return;

            var temp = RoutineData.Steps[index + 1];
            RoutineData.Steps[index + 1] = RoutineData.Steps[index];
            RoutineData.Steps[index] = temp;

            LoadSteps();
            lstSteps.SelectedIndex = index + 1;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var name = txtName.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a routine name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtName.Focus();
                return;
            }

            if (RoutineData.Steps == null || !RoutineData.Steps.Any())
            {
                MessageBox.Show("Please add at least one step to the routine.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RoutineData.Name = name;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete this routine?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                DeleteRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
