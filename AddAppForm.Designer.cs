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
            cmbGroup = new ComboBox();
            btnManageGroups = new Button();
            ((System.ComponentModel.ISupportInitialize)numDelay).BeginInit();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.Location = new Point(15, 15);
            lblName.Name = "lblName";
            lblName.Size = new Size(120, 20);
            lblName.TabIndex = 2;
            lblName.Text = "Application Name:";
            // 
            // txtName
            // 
            txtName.Location = new Point(148, 12);
            txtName.Name = "txtName";
            txtName.Size = new Size(250, 23);
            txtName.TabIndex = 3;
            // 
            // lblProcess
            // 
            lblProcess.Location = new Point(15, 84);
            lblProcess.Name = "lblProcess";
            lblProcess.Size = new Size(120, 20);
            lblProcess.TabIndex = 4;
            lblProcess.Text = "or Process Name:";
            // 
            // txtProcess
            // 
            txtProcess.Location = new Point(148, 81);
            txtProcess.Name = "txtProcess";
            txtProcess.Size = new Size(250, 23);
            txtProcess.TabIndex = 5;
            // 
            // lblPath
            // 
            lblPath.Location = new Point(15, 47);
            lblPath.Name = "lblPath";
            lblPath.Size = new Size(120, 18);
            lblPath.TabIndex = 6;
            lblPath.Text = "Restart Path:";
            // 
            // txtPath
            // 
            txtPath.Location = new Point(148, 44);
            txtPath.Name = "txtPath";
            txtPath.Size = new Size(210, 23);
            txtPath.TabIndex = 7;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(368, 43);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(70, 23);
            btnBrowse.TabIndex = 8;
            btnBrowse.Text = "Browse...";
            btnBrowse.Click += btnBrowse_Click;
            // 
            // lblClientIP
            // 
            lblClientIP.Location = new Point(15, 120);
            lblClientIP.Name = "lblClientIP";
            lblClientIP.Size = new Size(120, 20);
            lblClientIP.TabIndex = 9;
            lblClientIP.Text = "Client IP (optional):";
            // 
            // cboClientPc
            // 
            cboClientPc.DropDownStyle = ComboBoxStyle.DropDownList;
            cboClientPc.Location = new Point(148, 117);
            cboClientPc.Name = "cboClientPc";
            cboClientPc.Size = new Size(250, 23);
            cboClientPc.TabIndex = 10;
            // 
            // chkAutoStart
            // 
            chkAutoStart.Location = new Point(150, 150);
            chkAutoStart.Name = "chkAutoStart";
            chkAutoStart.Size = new Size(181, 24);
            chkAutoStart.TabIndex = 11;
            chkAutoStart.Text = "Auto-start this app after";
            // 
            // numDelay
            // 
            numDelay.Location = new Point(330, 152);
            numDelay.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numDelay.Name = "numDelay";
            numDelay.Size = new Size(50, 23);
            numDelay.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(150, 270);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(80, 30);
            btnSave.TabIndex = 12;
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(240, 270);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 30);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Cancel";
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(330, 270);
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
            label1.Location = new Point(388, 154);
            label1.Name = "label1";
            label1.Size = new Size(50, 15);
            label1.TabIndex = 15;
            label1.Text = "seconds";
            // 
            // chkStartMinimized
            // 
            chkStartMinimized.Location = new Point(150, 180);
            chkStartMinimized.Name = "chkStartMinimized";
            chkStartMinimized.Size = new Size(290, 24);
            chkStartMinimized.TabIndex = 16;
            chkStartMinimized.Text = "Auto-start minimized";
            // 
            // chkNoWarn
            // 
            chkNoWarn.Location = new Point(150, 210);
            chkNoWarn.Name = "chkNoWarn";
            chkNoWarn.Size = new Size(290, 24);
            chkNoWarn.TabIndex = 17;
            chkNoWarn.Text = "Don't warn when restarting";
            // 
            // lblGroup
            // 
            lblGroup.Location = new Point(15, 240);
            lblGroup.Name = "lblGroup";
            lblGroup.Size = new Size(120, 20);
            lblGroup.TabIndex = 0;
            lblGroup.Text = "Group:";
            // 
            // cmbGroup
            // 
            cmbGroup.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGroup.Location = new Point(148, 237);
            cmbGroup.Name = "cmbGroup";
            cmbGroup.Size = new Size(195, 23);
            cmbGroup.TabIndex = 1;
            // 
            // btnManageGroups
            // 
            btnManageGroups.Location = new Point(354, 237);
            btnManageGroups.Name = "btnManageGroups";
            btnManageGroups.Size = new Size(75, 23);
            btnManageGroups.TabIndex = 18;
            btnManageGroups.Text = "Manage";
            btnManageGroups.UseVisualStyleBackColor = true;
            btnManageGroups.Click += btnManageGroups_Click;
            // 
            // AddAppForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(470, 325);
            Controls.Add(btnManageGroups);
            Controls.Add(lblGroup);
            Controls.Add(cmbGroup);
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
        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.ComboBox cmbGroup;

        #endregion

        private Label label1;
        private CheckBox chkStartMinimized;
        private CheckBox chkNoWarn;
        private Button btnManageGroups;
    }
}
