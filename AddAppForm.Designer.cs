namespace AppRestarter
{
    partial class AddAppForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddAppForm));
            lblName = new Label();
            txtName = new TextBox();
            lblProcess = new Label();
            txtProcess = new TextBox();
            lblPath = new Label();
            txtPath = new TextBox();
            btnBrowse = new Button();
            lblClientIP = new Label();
            cboClientPc = new ComboBox();
            chkAutoStart = new CheckBox();
            numDelay = new NumericUpDown();
            btnSave = new Button();
            btnCancel = new Button();
            btnDelete = new Button();
            label1 = new Label();
            chkStartMinimized = new CheckBox();
            chkNoWarn = new CheckBox();
            lblGroup = new Label();
            chkListGroups = new CheckedListBox();
            btnManageGroups = new Button();
            ((System.ComponentModel.ISupportInitialize)numDelay).BeginInit();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.Location = new Point(2, 39);
            lblName.Name = "lblName";
            lblName.Size = new Size(177, 43);
            lblName.TabIndex = 2;
            lblName.Text = "Application Name:";
            lblName.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtName
            // 
            txtName.Location = new Point(185, 50);
            txtName.Name = "txtName";
            txtName.Size = new Size(250, 23);
            txtName.TabIndex = 3;
            // 
            // lblProcess
            // 
            lblProcess.Location = new Point(-12, 114);
            lblProcess.Name = "lblProcess";
            lblProcess.Size = new Size(191, 40);
            lblProcess.TabIndex = 4;
            lblProcess.Text = "or Process Name:";
            lblProcess.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtProcess
            // 
            txtProcess.Location = new Point(185, 124);
            txtProcess.Name = "txtProcess";
            txtProcess.Size = new Size(250, 23);
            txtProcess.TabIndex = 5;
            // 
            // lblPath
            // 
            lblPath.Location = new Point(-12, 79);
            lblPath.Name = "lblPath";
            lblPath.Size = new Size(191, 39);
            lblPath.TabIndex = 6;
            lblPath.Text = "Application Path:";
            lblPath.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtPath
            // 
            txtPath.Location = new Point(185, 87);
            txtPath.Name = "txtPath";
            txtPath.Size = new Size(210, 23);
            txtPath.TabIndex = 7;
            // 
            // btnBrowse
            // 
            btnBrowse.AutoSize = true;
            btnBrowse.Location = new Point(416, 86);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(73, 25);
            btnBrowse.TabIndex = 8;
            btnBrowse.Text = "Select App";
            btnBrowse.Click += btnBrowse_Click;
            // 
            // lblClientIP
            // 
            lblClientIP.Location = new Point(2, 8);
            lblClientIP.Name = "lblClientIP";
            lblClientIP.Size = new Size(177, 30);
            lblClientIP.TabIndex = 9;
            lblClientIP.Text = "Select PC:";
            lblClientIP.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cboClientPc
            // 
            cboClientPc.DropDownStyle = ComboBoxStyle.DropDownList;
            cboClientPc.Location = new Point(185, 13);
            cboClientPc.Name = "cboClientPc";
            cboClientPc.Size = new Size(250, 23);
            cboClientPc.TabIndex = 10;
            // 
            // chkAutoStart
            // 
            chkAutoStart.AutoSize = true;
            chkAutoStart.Location = new Point(200, 154);
            chkAutoStart.Name = "chkAutoStart";
            chkAutoStart.Size = new Size(152, 19);
            chkAutoStart.TabIndex = 11;
            chkAutoStart.Text = "Auto-start this app after";
            // 
            // numDelay
            // 
            numDelay.Location = new Point(385, 153);
            numDelay.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numDelay.Name = "numDelay";
            numDelay.Size = new Size(50, 23);
            numDelay.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.AutoSize = true;
            btnSave.Location = new Point(381, 329);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(80, 30);
            btnSave.TabIndex = 12;
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(272, 329);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 30);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Cancel";
            // 
            // btnDelete
            // 
            btnDelete.AutoSize = true;
            btnDelete.Location = new Point(165, 329);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(80, 30);
            btnDelete.TabIndex = 14;
            btnDelete.Text = "Delete";
            btnDelete.Visible = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(453, 155);
            label1.Name = "label1";
            label1.Size = new Size(50, 15);
            label1.TabIndex = 15;
            label1.Text = "seconds";
            // 
            // chkStartMinimized
            // 
            chkStartMinimized.AutoSize = true;
            chkStartMinimized.Location = new Point(200, 184);
            chkStartMinimized.Name = "chkStartMinimized";
            chkStartMinimized.Size = new Size(139, 19);
            chkStartMinimized.TabIndex = 16;
            chkStartMinimized.Text = "Auto-start minimized";
            // 
            // chkNoWarn
            // 
            chkNoWarn.AutoSize = true;
            chkNoWarn.Location = new Point(200, 214);
            chkNoWarn.Name = "chkNoWarn";
            chkNoWarn.Size = new Size(169, 19);
            chkNoWarn.TabIndex = 17;
            chkNoWarn.Text = "Don't warn when restarting";
            // 
            // lblGroup
            // 
            lblGroup.AutoSize = true;
            lblGroup.Location = new Point(65, 244);
            lblGroup.Name = "lblGroup";
            lblGroup.Size = new Size(48, 15);
            lblGroup.TabIndex = 0;
            lblGroup.Text = "Groups:";
            lblGroup.TextAlign = ContentAlignment.MiddleRight;
            // 
            // chkListGroups
            // 
            chkListGroups.CheckOnClick = true;
            chkListGroups.Location = new Point(185, 241);
            chkListGroups.Name = "chkListGroups";
            chkListGroups.Size = new Size(195, 76);
            chkListGroups.TabIndex = 1;
            // 
            // btnManageGroups
            // 
            btnManageGroups.AutoSize = true;
            btnManageGroups.Location = new Point(396, 239);
            btnManageGroups.Name = "btnManageGroups";
            btnManageGroups.Size = new Size(75, 25);
            btnManageGroups.TabIndex = 18;
            btnManageGroups.Text = "Manage";
            btnManageGroups.UseVisualStyleBackColor = true;
            btnManageGroups.Click += btnManageGroups_Click;
            // 
            // AddAppForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(708, 370);
            Controls.Add(btnManageGroups);
            Controls.Add(lblGroup);
            Controls.Add(chkListGroups);
            Controls.Add(chkNoWarn);
            Controls.Add(chkStartMinimized);
            Controls.Add(label1);
            Controls.Add(numDelay);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblProcess);
            Controls.Add(txtProcess);
            Controls.Add(lblPath);
            Controls.Add(txtPath);
            Controls.Add(btnBrowse);
            Controls.Add(lblClientIP);
            Controls.Add(cboClientPc);
            Controls.Add(chkAutoStart);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            Controls.Add(btnDelete);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "AddAppForm";
            Padding = new Padding(10, 0, 20, 0);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add New Application";
            ((System.ComponentModel.ISupportInitialize)numDelay).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblProcess;
        private System.Windows.Forms.TextBox txtProcess;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblClientIP;
        private System.Windows.Forms.ComboBox cboClientPc;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.NumericUpDown numDelay;

        #endregion

        private Label label1;
        private CheckBox chkStartMinimized;
        private CheckBox chkNoWarn;
        private Label lblGroup;
        private CheckedListBox chkListGroups;
        private Button btnManageGroups;
    }
}
