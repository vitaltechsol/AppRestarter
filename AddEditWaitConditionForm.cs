using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AppRestarter.Models;

namespace AppRestarter
{
    public partial class AddEditWaitConditionForm : Form
    {
        public WaitCondition Condition { get; private set; }
        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly List<GroupDetails> _groups;

        public AddEditWaitConditionForm(
            WaitCondition existing = null,
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

            Condition = existing ?? new WaitCondition { Type = WaitType.TimeDelay, WaitTimeSeconds = 5 };

            LoadConditionTypes();
            LoadTargets();
            LoadExistingData();
        }

        private void LoadConditionTypes()
        {
            cboConditionType.Items.Clear();
            cboConditionType.Items.Add("Time Delay (seconds)");
            cboConditionType.Items.Add("Wait for App to be running");

            // Set default selection based on condition type
            switch (Condition.Type)
            {
                case WaitType.TimeDelay:
                    cboConditionType.SelectedIndex = 0;
                    break;
                case WaitType.AppRunning:
                    cboConditionType.SelectedIndex = 1;
                    break;
                default:
                    cboConditionType.SelectedIndex = 0;
                    break;
            }
        }

        private void LoadTargets()
        {
            cboTarget.Items.Clear();
            foreach (var app in _apps.OrderBy(a => a.Name))
            {
                cboTarget.Items.Add(app.Name);
            }
        }

        private void LoadExistingData()
        {
            numDuration.Value = Math.Max(1, Math.Min(86400, Condition.WaitTimeSeconds > 0 ? Condition.WaitTimeSeconds : 5));

            if (!string.IsNullOrWhiteSpace(Condition.AppId))
            {
                var index = cboTarget.Items.IndexOf(Condition.AppId);
                if (index >= 0)
                    cboTarget.SelectedIndex = index;
            }

            UpdateControlVisibility();
        }

        private void cboConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlVisibility();
        }

        private void UpdateControlVisibility()
        {
            var selectedIndex = cboConditionType.SelectedIndex;

            bool showDuration = selectedIndex == 0; // Time Delay
            bool showTarget = selectedIndex == 1;   // Wait for App

            lblDuration.Visible = showDuration;
            numDuration.Visible = showDuration;

            lblTarget.Visible = showTarget;
            cboTarget.Visible = showTarget;

            lblProcessName.Visible = false;
            txtProcessName.Visible = false;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var selectedIndex = cboConditionType.SelectedIndex;

            Condition.Type = selectedIndex switch
            {
                0 => WaitType.TimeDelay,
                1 => WaitType.AppRunning,
                _ => WaitType.TimeDelay
            };

            Condition.WaitTimeSeconds = (int)numDuration.Value;
            Condition.AppId = cboTarget.SelectedItem?.ToString() ?? "";

            // Set default timeout for AppRunning if not set
            if (Condition.Type == WaitType.AppRunning && Condition.AppStartTimeoutSeconds == 0)
            {
                Condition.AppStartTimeoutSeconds = 30;
            }

            // Validate based on type
            if (Condition.Type == WaitType.AppRunning && string.IsNullOrWhiteSpace(Condition.AppId))
            {
                MessageBox.Show("Please select an app to wait for.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
