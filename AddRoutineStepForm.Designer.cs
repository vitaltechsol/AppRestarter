namespace AppRestarter
{
    partial class AddRoutineStepForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            groupWait = new GroupBox();
            btnRemoveWait = new Button();
            btnAddWait = new Button();
            btnEditWait = new Button();
            btnMoveUpWait = new Button();
            btnMoveDownWait = new Button();
            lstWaitConditions = new ListBox();
            groupAction = new GroupBox();
            lblClickY = new Label();
            numClickY = new NumericUpDown();
            btnRecordMouse = new Button();
            lblClickX = new Label();
            numClickX = new NumericUpDown();
            lblKeys = new Label();
            txtKeys = new TextBox();
            btnPickShortcut = new Button();
            lblTarget = new Label();
            cboTarget = new ComboBox();
            lblTargetType = new Label();
            cboTargetType = new ComboBox();
            lblActionType = new Label();
            cboActionType = new ComboBox();
            btnSave = new Button();
            btnCancel = new Button();
            groupWait.SuspendLayout();
            groupAction.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numClickY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numClickX).BeginInit();
            SuspendLayout();
            // 
            // groupWait
            // 
            groupWait.Controls.Add(btnRemoveWait);
            groupWait.Controls.Add(btnAddWait);
            groupWait.Controls.Add(btnEditWait);
            groupWait.Controls.Add(btnMoveUpWait);
            groupWait.Controls.Add(btnMoveDownWait);
            groupWait.Controls.Add(lstWaitConditions);
            groupWait.Location = new Point(12, 12);
            groupWait.Name = "groupWait";
            groupWait.Size = new Size(560, 150);
            groupWait.TabIndex = 0;
            groupWait.TabStop = false;
            groupWait.Text = "Optional wait conditions: (executed in order)";
            // 
            // btnRemoveWait
            // 
            btnRemoveWait.Location = new Point(459, 88);
            btnRemoveWait.Name = "btnRemoveWait";
            btnRemoveWait.Size = new Size(90, 27);
            btnRemoveWait.TabIndex = 2;
            btnRemoveWait.Text = "Remove";
            btnRemoveWait.UseVisualStyleBackColor = true;
            btnRemoveWait.Click += btnRemoveWait_Click;
            // 
            // btnAddWait
            // 
            btnAddWait.Location = new Point(459, 22);
            btnAddWait.Name = "btnAddWait";
            btnAddWait.Size = new Size(90, 27);
            btnAddWait.TabIndex = 1;
            btnAddWait.Text = "Add";
            btnAddWait.UseVisualStyleBackColor = true;
            btnAddWait.Click += btnAddWait_Click;
            // 
            // btnEditWait
            // 
            btnEditWait.Location = new Point(459, 55);
            btnEditWait.Name = "btnEditWait";
            btnEditWait.Size = new Size(90, 27);
            btnEditWait.TabIndex = 3;
            btnEditWait.Text = "Edit";
            btnEditWait.UseVisualStyleBackColor = true;
            btnEditWait.Click += btnEditWait_Click;
            // 
            // btnMoveUpWait
            // 
            btnMoveUpWait.Location = new Point(511, 121);
            btnMoveUpWait.Name = "btnMoveUpWait";
            btnMoveUpWait.Size = new Size(38, 23);
            btnMoveUpWait.TabIndex = 4;
            btnMoveUpWait.Text = "↑";
            btnMoveUpWait.UseVisualStyleBackColor = true;
            btnMoveUpWait.Click += btnMoveUpWait_Click;
            // 
            // btnMoveDownWait
            // 
            btnMoveDownWait.Location = new Point(459, 121);
            btnMoveDownWait.Name = "btnMoveDownWait";
            btnMoveDownWait.Size = new Size(37, 23);
            btnMoveDownWait.TabIndex = 5;
            btnMoveDownWait.Text = "↓";
            btnMoveDownWait.UseVisualStyleBackColor = true;
            btnMoveDownWait.Click += btnMoveDownWait_Click;
            // 
            // lstWaitConditions
            // 
            lstWaitConditions.FormattingEnabled = true;
            lstWaitConditions.ItemHeight = 15;
            lstWaitConditions.Location = new Point(6, 22);
            lstWaitConditions.Name = "lstWaitConditions";
            lstWaitConditions.Size = new Size(440, 109);
            lstWaitConditions.TabIndex = 0;
            lstWaitConditions.SelectedIndexChanged += lstWaitConditions_SelectedIndexChanged;
            // 
            // groupAction
            // 
            groupAction.Controls.Add(lblClickY);
            groupAction.Controls.Add(numClickY);
            groupAction.Controls.Add(btnRecordMouse);
            groupAction.Controls.Add(lblClickX);
            groupAction.Controls.Add(numClickX);
            groupAction.Controls.Add(lblKeys);
            groupAction.Controls.Add(txtKeys);
            groupAction.Controls.Add(btnPickShortcut);
            groupAction.Controls.Add(lblTarget);
            groupAction.Controls.Add(cboTarget);
            groupAction.Controls.Add(lblTargetType);
            groupAction.Controls.Add(cboTargetType);
            groupAction.Controls.Add(lblActionType);
            groupAction.Controls.Add(cboActionType);
            groupAction.Location = new Point(12, 168);
            groupAction.Name = "groupAction";
            groupAction.Size = new Size(560, 200);
            groupAction.TabIndex = 1;
            groupAction.TabStop = false;
            groupAction.Text = "Action";
            // 
            // lblClickY
            // 
            lblClickY.AutoSize = true;
            lblClickY.Location = new Point(15, 165);
            lblClickY.Name = "lblClickY";
            lblClickY.Size = new Size(46, 15);
            lblClickY.TabIndex = 11;
            lblClickY.Text = "Click Y:";
            // 
            // numClickY
            // 
            numClickY.Location = new Point(120, 163);
            numClickY.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numClickY.Name = "numClickY";
            numClickY.Size = new Size(120, 23);
            numClickY.TabIndex = 10;
            // 
            // btnRecordMouse
            // 
            btnRecordMouse.Location = new Point(250, 148);
            btnRecordMouse.Name = "btnRecordMouse";
            btnRecordMouse.Size = new Size(246, 35);
            btnRecordMouse.TabIndex = 13;
            btnRecordMouse.Text = "🔴 Record Position (Press R)";
            btnRecordMouse.UseVisualStyleBackColor = true;
            btnRecordMouse.Click += btnRecordMouse_Click;
            // 
            // lblClickX
            // 
            lblClickX.AutoSize = true;
            lblClickX.Location = new Point(15, 136);
            lblClickX.Name = "lblClickX";
            lblClickX.Size = new Size(46, 15);
            lblClickX.TabIndex = 9;
            lblClickX.Text = "Click X:";
            // 
            // numClickX
            // 
            numClickX.Location = new Point(120, 134);
            numClickX.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numClickX.Name = "numClickX";
            numClickX.Size = new Size(120, 23);
            numClickX.TabIndex = 8;
            // 
            // lblKeys
            // 
            lblKeys.AutoSize = true;
            lblKeys.Location = new Point(15, 107);
            lblKeys.Name = "lblKeys";
            lblKeys.Size = new Size(98, 15);
            lblKeys.TabIndex = 7;
            lblKeys.Text = "Keys (e.g., ^{F5}):";
            // 
            // txtKeys
            // 
            txtKeys.Location = new Point(120, 104);
            txtKeys.Name = "txtKeys";
            txtKeys.Size = new Size(200, 23);
            txtKeys.TabIndex = 6;
            // 
            // btnPickShortcut
            // 
            btnPickShortcut.Location = new Point(330, 103);
            btnPickShortcut.Name = "btnPickShortcut";
            btnPickShortcut.Size = new Size(30, 25);
            btnPickShortcut.TabIndex = 12;
            btnPickShortcut.Text = "...";
            btnPickShortcut.UseVisualStyleBackColor = true;
            btnPickShortcut.Click += btnPickShortcut_Click;
            // 
            // lblTarget
            // 
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(295, 52);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(42, 15);
            lblTarget.TabIndex = 5;
            lblTarget.Text = "Target:";
            // 
            // cboTarget
            // 
            cboTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTarget.FormattingEnabled = true;
            cboTarget.Location = new Point(370, 49);
            cboTarget.Name = "cboTarget";
            cboTarget.Size = new Size(180, 23);
            cboTarget.TabIndex = 4;
            // 
            // lblTargetType
            // 
            lblTargetType.AutoSize = true;
            lblTargetType.Location = new Point(15, 52);
            lblTargetType.Name = "lblTargetType";
            lblTargetType.Size = new Size(69, 15);
            lblTargetType.TabIndex = 3;
            lblTargetType.Text = "Target Type:";
            // 
            // cboTargetType
            // 
            cboTargetType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTargetType.FormattingEnabled = true;
            cboTargetType.Location = new Point(120, 49);
            cboTargetType.Name = "cboTargetType";
            cboTargetType.Size = new Size(150, 23);
            cboTargetType.TabIndex = 2;
            cboTargetType.SelectedIndexChanged += cboTargetType_SelectedIndexChanged;
            // 
            // lblActionType
            // 
            lblActionType.AutoSize = true;
            lblActionType.Location = new Point(15, 25);
            lblActionType.Name = "lblActionType";
            lblActionType.Size = new Size(72, 15);
            lblActionType.TabIndex = 1;
            lblActionType.Text = "Action Type:";
            // 
            // cboActionType
            // 
            cboActionType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboActionType.FormattingEnabled = true;
            cboActionType.Location = new Point(120, 22);
            cboActionType.Name = "cboActionType";
            cboActionType.Size = new Size(200, 23);
            cboActionType.TabIndex = 0;
            cboActionType.SelectedIndexChanged += cboActionType_SelectedIndexChanged;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(392, 380);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 30);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(488, 380);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 30);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // AddRoutineStepForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(584, 422);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(groupAction);
            Controls.Add(groupWait);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddRoutineStepForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add/Edit Routine Step";
            groupWait.ResumeLayout(false);
            groupAction.ResumeLayout(false);
            groupAction.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numClickY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numClickX).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupWait;
        private ListBox lstWaitConditions;
        private Button btnAddWait;
        private Button btnRemoveWait;
        private Button btnEditWait;
        private Button btnMoveUpWait;
        private Button btnMoveDownWait;
        private GroupBox groupAction;
        private ComboBox cboActionType;
        private Label lblActionType;
        private Label lblTargetType;
        private ComboBox cboTargetType;
        private Label lblTarget;
        private ComboBox cboTarget;
        private Label lblKeys;
        private TextBox txtKeys;
        private Button btnPickShortcut;
        private Label lblClickX;
        private NumericUpDown numClickX;
        private Label lblClickY;
        private NumericUpDown numClickY;
        private Button btnRecordMouse;
        private Button btnSave;
        private Button btnCancel;
    }
}
