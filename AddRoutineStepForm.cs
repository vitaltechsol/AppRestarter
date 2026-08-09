using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AppRestarter.Models;

namespace AppRestarter
{
    public partial class AddRoutineStepForm : Form
    {
        public RoutineStep StepData { get; private set; }
        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly List<GroupDetails> _groups;

        // Win32 API for global keyboard hook and mouse position
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _hookCallback;
        private bool _isRecording = false;

        public AddRoutineStepForm(
            RoutineStep existing = null,
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

            StepData = existing ?? new RoutineStep { Action = new RoutineAction() };

            LoadWaitConditions();
            LoadAction();
        }

        private void LoadWaitConditions()
        {
            lstWaitConditions.DataSource = null;
            if (StepData.WaitConditions == null || StepData.WaitConditions.Count == 0)
            {
                lstWaitConditions.Items.Clear();
            }
            else
            {
                lstWaitConditions.DataSource = StepData.WaitConditions;
                lstWaitConditions.DisplayMember = "Summary";
            }
            UpdateWaitButtonStates();
        }

        private void LoadAction()
        {
            if (StepData.Action == null)
            {
                StepData.Action = new RoutineAction();
            }

            // Load description
            txtDescription.Text = StepData.Description ?? "";

            // Load action type
            cboActionType.Items.Clear();
            cboActionType.Items.Add("Start");
            cboActionType.Items.Add("Restart");
            cboActionType.Items.Add("Stop");
            cboActionType.Items.Add("Keyboard Shortcut");
            cboActionType.Items.Add("Click Area");
            cboActionType.Items.Add("Minimize");

            switch (StepData.Action.Type)
            {
                case ActionType.Start:
                    cboActionType.SelectedIndex = 0;
                    break;
                case ActionType.Restart:
                    cboActionType.SelectedIndex = 1;
                    break;
                case ActionType.Stop:
                    cboActionType.SelectedIndex = 2;
                    break;
                case ActionType.KeyboardShortcut:
                    cboActionType.SelectedIndex = 3;
                    break;
                case ActionType.ClickArea:
                    cboActionType.SelectedIndex = 4;
                    break;
                case ActionType.Minimize:
                    cboActionType.SelectedIndex = 5;
                    break;
                default:
                    cboActionType.SelectedIndex = 0;
                    break;
            }

            // Load target type
            cboTargetType.Items.Clear();
            cboTargetType.Items.Add("App");
            cboTargetType.Items.Add("PC");
            cboTargetType.Items.Add("Group");

            switch (StepData.Action.TargetType)
            {
                case TargetType.App:
                    cboTargetType.SelectedIndex = 0;
                    break;
                case TargetType.PC:
                    cboTargetType.SelectedIndex = 1;
                    break;
                case TargetType.Group:
                    cboTargetType.SelectedIndex = 2;
                    break;
                default:
                    cboTargetType.SelectedIndex = 0;
                    break;
            }

            LoadTargets();

            txtKeys.Text = StepData.Action.Keys ?? "";
            numClickX.Value = StepData.Action.ClickX;
            numClickY.Value = StepData.Action.ClickY;
            chkDoubleClick.Checked = StepData.Action.DoubleClick;

            UpdateActionControls();
        }

        private void LoadTargets()
        {
            cboTarget.Items.Clear();

            var targetType = cboTargetType.SelectedIndex == 0 ? TargetType.App :
                             cboTargetType.SelectedIndex == 1 ? TargetType.PC : TargetType.Group;

            switch (targetType)
            {
                case TargetType.App:
                    foreach (var app in _apps.OrderBy(a => a.Name))
                        cboTarget.Items.Add(app.Name);
                    break;
                case TargetType.PC:
                    foreach (var pc in _pcs.OrderBy(p => p.Name))
                        cboTarget.Items.Add(pc.Name);
                    break;
                case TargetType.Group:
                    foreach (var group in _groups.OrderBy(g => g.Name))
                        cboTarget.Items.Add(group.Name);
                    break;
            }

            // Try to select existing target
            if (!string.IsNullOrWhiteSpace(StepData.Action.TargetId))
            {
                var index = cboTarget.Items.IndexOf(StepData.Action.TargetId);
                if (index >= 0)
                    cboTarget.SelectedIndex = index;
            }
        }

        private void cboActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionControls();
        }

        private void cboTargetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTargets();
        }

        private void UpdateActionControls()
        {
            var selectedIndex = cboActionType.SelectedIndex;

            // Show/hide based on action type
            bool showTarget = selectedIndex >= 0 && selectedIndex <= 5; // All actions need a target
            bool showKeys = selectedIndex == 3; // Keyboard Shortcut
            bool showClick = selectedIndex == 4; // Click Area

            lblTarget.Visible = showTarget;
            lblTargetType.Visible = showTarget;
            cboTarget.Visible = showTarget;
            cboTargetType.Visible = showTarget;

            lblKeys.Visible = showKeys;
            txtKeys.Visible = showKeys;
            btnPickShortcut.Visible = showKeys;

            lblClickX.Visible = showClick;
            numClickX.Visible = showClick;
            lblClickY.Visible = showClick;
            numClickY.Visible = showClick;
            btnRecordMouse.Visible = showClick;
            chkDoubleClick.Visible = showClick;

            // Description is only for Click Area actions (ambiguous actions)
            lblDescription.Visible = showClick;
            txtDescription.Visible = showClick;

            // Stop recording if switching away from click area
            if (!showClick && _isRecording)
            {
                StopRecording();
            }
        }

        private void btnAddWait_Click(object sender, EventArgs e)
        {
            using var dlg = new AddEditWaitConditionForm(null, _apps, _pcs, _groups);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StepData.WaitConditions.Add(dlg.Condition);
                LoadWaitConditions();
            }
        }

        private void btnEditWait_Click(object sender, EventArgs e)
        {
            if (lstWaitConditions.SelectedIndex < 0 || lstWaitConditions.SelectedIndex >= StepData.WaitConditions.Count)
                return;

            var condition = StepData.WaitConditions[lstWaitConditions.SelectedIndex];
            using var dlg = new AddEditWaitConditionForm(condition, _apps, _pcs, _groups);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StepData.WaitConditions[lstWaitConditions.SelectedIndex] = dlg.Condition;
                LoadWaitConditions();
            }
        }

        private void btnRemoveWait_Click(object sender, EventArgs e)
        {
            if (lstWaitConditions.SelectedIndex >= 0 && lstWaitConditions.SelectedIndex < StepData.WaitConditions.Count)
            {
                StepData.WaitConditions.RemoveAt(lstWaitConditions.SelectedIndex);
                LoadWaitConditions();
            }
        }

        private void btnPickShortcut_Click(object sender, EventArgs e)
        {
            using var dlg = new KeyboardShortcutPickerForm(txtKeys.Text);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtKeys.Text = dlg.ShortcutKeys;
            }
        }

        private void btnRecordMouse_Click(object sender, EventArgs e)
        {
            if (_isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            _isRecording = true;
            btnRecordMouse.Text = "⏹️ Recording... (Press R)";
            btnRecordMouse.BackColor = System.Drawing.Color.LightCoral;

            // Set up the keyboard hook
            _hookCallback = HookCallback;
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void StopRecording()
        {
            _isRecording = false;
            btnRecordMouse.Text = "🔴 Record Position (Press R)";
            btnRecordMouse.BackColor = System.Drawing.SystemColors.Control;

            // Unhook the keyboard
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                // 'R' key is virtual key code 0x52
                if (vkCode == 0x52 && _isRecording)
                {
                    // Capture mouse position
                    if (GetCursorPos(out POINT point))
                    {
                        // Use Invoke to update UI from hook thread
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() =>
                            {
                                numClickX.Value = point.X;
                                numClickY.Value = point.Y;
                                StopRecording();
                            }));
                        }
                        else
                        {
                            numClickX.Value = point.X;
                            numClickY.Value = point.Y;
                            StopRecording();
                        }
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up hook on form close
            if (_isRecording)
            {
                StopRecording();
            }
            base.OnFormClosing(e);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Save description
            StepData.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();

            // Save action
            var actionIndex = cboActionType.SelectedIndex;
            StepData.Action.Type = actionIndex switch
            {
                0 => ActionType.Start,
                1 => ActionType.Restart,
                2 => ActionType.Stop,
                3 => ActionType.KeyboardShortcut,
                4 => ActionType.ClickArea,
                5 => ActionType.Minimize,
                _ => ActionType.Start
            };

            var targetTypeIndex = cboTargetType.SelectedIndex;
            StepData.Action.TargetType = targetTypeIndex switch
            {
                0 => TargetType.App,
                1 => TargetType.PC,
                2 => TargetType.Group,
                _ => TargetType.App
            };

            StepData.Action.TargetId = cboTarget.SelectedItem?.ToString() ?? "";
            StepData.Action.Keys = txtKeys.Text;
            StepData.Action.ClickX = (int)numClickX.Value;
            StepData.Action.ClickY = (int)numClickY.Value;
            StepData.Action.DoubleClick = chkDoubleClick.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void lstWaitConditions_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateWaitButtonStates();
        }

        private void UpdateWaitButtonStates()
        {
            var selectedIndex = lstWaitConditions.SelectedIndex;
            var count = lstWaitConditions.Items.Count;

            btnEditWait.Enabled = selectedIndex >= 0;
            btnRemoveWait.Enabled = selectedIndex >= 0;
            btnMoveUpWait.Enabled = selectedIndex > 0;
            btnMoveDownWait.Enabled = selectedIndex >= 0 && selectedIndex < count - 1;
        }

        private void btnMoveUpWait_Click(object sender, EventArgs e)
        {
            var index = lstWaitConditions.SelectedIndex;
            if (index <= 0) return;

            var item = StepData.WaitConditions[index];
            StepData.WaitConditions.RemoveAt(index);
            StepData.WaitConditions.Insert(index - 1, item);
            LoadWaitConditions();
            lstWaitConditions.SelectedIndex = index - 1;
        }

        private void btnMoveDownWait_Click(object sender, EventArgs e)
        {
            var index = lstWaitConditions.SelectedIndex;
            if (index < 0 || index >= StepData.WaitConditions.Count - 1) return;

            var item = StepData.WaitConditions[index];
            StepData.WaitConditions.RemoveAt(index);
            StepData.WaitConditions.Insert(index + 1, item);
            LoadWaitConditions();
            lstWaitConditions.SelectedIndex = index + 1;
        }

        private async void btnTest_Click(object sender, EventArgs e)
        {
            // Validate that we have a valid action configuration
            if (cboActionType.SelectedIndex < 0)
            {
                MessageBox.Show("Please select an action type.", "Test Step", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboTarget.SelectedIndex < 0 && cboActionType.SelectedIndex <= 2) // Start/Restart/Stop/Minimize need target
            {
                MessageBox.Show("Please select a target.", "Test Step", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build a temporary step with current form values
            var testStep = new RoutineStep
            {
                Description = txtDescription.Text,
                WaitConditions = new List<WaitCondition>(StepData.WaitConditions),
                Action = new RoutineAction
                {
                    Type = cboActionType.SelectedIndex switch
                    {
                        0 => ActionType.Start,
                        1 => ActionType.Restart,
                        2 => ActionType.Stop,
                        3 => ActionType.KeyboardShortcut,
                        4 => ActionType.ClickArea,
                        5 => ActionType.Minimize,
                        _ => ActionType.Start
                    },
                    TargetType = cboTargetType.SelectedIndex switch
                    {
                        0 => TargetType.App,
                        1 => TargetType.PC,
                        2 => TargetType.Group,
                        _ => TargetType.App
                    },
                    TargetId = cboTarget.SelectedItem?.ToString() ?? "",
                    Keys = txtKeys.Text,
                    ClickX = (int)numClickX.Value,
                    ClickY = (int)numClickY.Value,
                    DoubleClick = chkDoubleClick.Checked
                }
            };

            // Create a temporary routine with just this step
            var testRoutine = new Routine
            {
                Name = "Test",
                Steps = new List<RoutineStep> { testStep }
            };

            // Collect log messages
            var logMessages = new System.Text.StringBuilder();
            void LogAction(string message)
            {
                logMessages.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }

            // Disable the test button and show it's running
            btnTest.Enabled = false;
            btnTest.Text = "⏳ Testing...";
            Application.DoEvents();

            try
            {
                var executor = new RoutineExecutor(_apps, LogAction);
                await executor.ExecuteRoutineAsync(testRoutine);

                // Show the log
                var result = MessageBox.Show(
                    $"Test completed!\n\nLog:\n{logMessages}",
                    "Test Step Result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Test failed with error:\n{ex.Message}\n\nLog:\n{logMessages}",
                    "Test Step Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "🧪 Test Step";
            }
        }
    }
}
