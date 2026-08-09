namespace AppRestarter
{
    partial class KeyboardShortcutPickerForm
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
            groupModifiers = new GroupBox();
            chkWin = new CheckBox();
            chkShift = new CheckBox();
            chkAlt = new CheckBox();
            chkCtrl = new CheckBox();
            lblKey = new Label();
            cboKey = new ComboBox();
            lblPreview = new Label();
            txtPreview = new TextBox();
            btnOK = new Button();
            btnCancel = new Button();
            lblHelp = new Label();
            groupModifiers.SuspendLayout();
            SuspendLayout();
            // 
            // groupModifiers
            // 
            groupModifiers.Controls.Add(chkWin);
            groupModifiers.Controls.Add(chkShift);
            groupModifiers.Controls.Add(chkAlt);
            groupModifiers.Controls.Add(chkCtrl);
            groupModifiers.Location = new Point(12, 12);
            groupModifiers.Name = "groupModifiers";
            groupModifiers.Size = new Size(460, 60);
            groupModifiers.TabIndex = 0;
            groupModifiers.TabStop = false;
            groupModifiers.Text = "Modifiers";
            // 
            // chkWin
            // 
            chkWin.AutoSize = true;
            chkWin.Location = new Point(250, 25);
            chkWin.Name = "chkWin";
            chkWin.Size = new Size(80, 19);
            chkWin.TabIndex = 3;
            chkWin.Text = "Win (⊞)";
            chkWin.UseVisualStyleBackColor = true;
            chkWin.CheckedChanged += chkModifier_CheckedChanged;
            // 
            // chkShift
            // 
            chkShift.AutoSize = true;
            chkShift.Location = new Point(170, 25);
            chkShift.Name = "chkShift";
            chkShift.Size = new Size(50, 19);
            chkShift.TabIndex = 2;
            chkShift.Text = "Shift";
            chkShift.UseVisualStyleBackColor = true;
            chkShift.CheckedChanged += chkModifier_CheckedChanged;
            // 
            // chkAlt
            // 
            chkAlt.AutoSize = true;
            chkAlt.Location = new Point(100, 25);
            chkAlt.Name = "chkAlt";
            chkAlt.Size = new Size(41, 19);
            chkAlt.TabIndex = 1;
            chkAlt.Text = "Alt";
            chkAlt.UseVisualStyleBackColor = true;
            chkAlt.CheckedChanged += chkModifier_CheckedChanged;
            // 
            // chkCtrl
            // 
            chkCtrl.AutoSize = true;
            chkCtrl.Location = new Point(20, 25);
            chkCtrl.Name = "chkCtrl";
            chkCtrl.Size = new Size(45, 19);
            chkCtrl.TabIndex = 0;
            chkCtrl.Text = "Ctrl";
            chkCtrl.UseVisualStyleBackColor = true;
            chkCtrl.CheckedChanged += chkModifier_CheckedChanged;
            // 
            // lblKey
            // 
            lblKey.AutoSize = true;
            lblKey.Location = new Point(12, 85);
            lblKey.Name = "lblKey";
            lblKey.Size = new Size(29, 15);
            lblKey.TabIndex = 1;
            lblKey.Text = "Key:";
            // 
            // cboKey
            // 
            cboKey.DropDownStyle = ComboBoxStyle.DropDownList;
            cboKey.FormattingEnabled = true;
            cboKey.Items.AddRange(new object[] {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
            "ENTER", "TAB", "ESC", "SPACE", "BACKSPACE", "DELETE",
            "UP", "DOWN", "LEFT", "RIGHT",
            "HOME", "END", "PGUP", "PGDN",
            "INSERT"
            });
            cboKey.Location = new Point(70, 82);
            cboKey.Name = "cboKey";
            cboKey.Size = new Size(150, 23);
            cboKey.TabIndex = 2;
            cboKey.SelectedIndexChanged += cboKey_SelectedIndexChanged;
            // 
            // lblPreview
            // 
            lblPreview.AutoSize = true;
            lblPreview.Location = new Point(12, 120);
            lblPreview.Name = "lblPreview";
            lblPreview.Size = new Size(102, 15);
            lblPreview.TabIndex = 3;
            lblPreview.Text = "SendKeys Syntax:";
            // 
            // txtPreview
            // 
            txtPreview.Location = new Point(120, 117);
            txtPreview.Name = "txtPreview";
            txtPreview.ReadOnly = true;
            txtPreview.Size = new Size(352, 23);
            txtPreview.TabIndex = 4;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(292, 195);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 30);
            btnOK.TabIndex = 5;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(388, 195);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 30);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblHelp
            // 
            lblHelp.ForeColor = SystemColors.GrayText;
            lblHelp.Location = new Point(12, 150);
            lblHelp.Name = "lblHelp";
            lblHelp.Size = new Size(460, 35);
            lblHelp.TabIndex = 7;
            lblHelp.Text = "Select modifier keys and a main key to create a keyboard shortcut. The shortcut will be sent to the target application.";
            // 
            // KeyboardShortcutPickerForm
            // 
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            ClientSize = new Size(484, 237);
            Controls.Add(lblHelp);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(txtPreview);
            Controls.Add(lblPreview);
            Controls.Add(cboKey);
            Controls.Add(lblKey);
            Controls.Add(groupModifiers);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeyboardShortcutPickerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Keyboard Shortcut Picker";
            groupModifiers.ResumeLayout(false);
            groupModifiers.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupModifiers;
        private CheckBox chkWin;
        private CheckBox chkShift;
        private CheckBox chkAlt;
        private CheckBox chkCtrl;
        private Label lblKey;
        private ComboBox cboKey;
        private Label lblPreview;
        private TextBox txtPreview;
        private Button btnOK;
        private Button btnCancel;
        private Label lblHelp;
    }
}
