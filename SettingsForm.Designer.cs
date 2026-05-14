namespace AppRestarter
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblAppPort;
        private System.Windows.Forms.Label lblWebPort;
        private System.Windows.Forms.NumericUpDown numAppPort;
        private System.Windows.Forms.NumericUpDown numWebPort;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.CheckBox chkStartMin;
        private System.Windows.Forms.CheckBox chkCheckUpdates;
        private System.Windows.Forms.Button btnCheckUpdate;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblAppPort = new Label();
            lblWebPort = new Label();
            numAppPort = new NumericUpDown();
            numWebPort = new NumericUpDown();
            chkAutoStart = new CheckBox();
            chkStartMin = new CheckBox();
            chkCheckUpdates = new CheckBox();
            btnCheckUpdate = new Button();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numAppPort).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWebPort).BeginInit();
            SuspendLayout();
            // 
            // lblAppPort
            // 
            lblAppPort.AutoSize = true;
            lblAppPort.Location = new Point(20, 20);
            lblAppPort.Name = "lblAppPort";
            lblAppPort.Size = new Size(57, 15);
            lblAppPort.TabIndex = 0;
            lblAppPort.Text = "App Port:";
            // 
            // lblWebPort
            // 
            lblWebPort.AutoSize = true;
            lblWebPort.Location = new Point(20, 60);
            lblWebPort.Name = "lblWebPort";
            lblWebPort.Size = new Size(59, 15);
            lblWebPort.TabIndex = 1;
            lblWebPort.Text = "Web Port:";
            // 
            // numAppPort
            // 
            numAppPort.Location = new Point(120, 18);
            numAppPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numAppPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numAppPort.Name = "numAppPort";
            numAppPort.Size = new Size(120, 23);
            numAppPort.TabIndex = 2;
            numAppPort.Value = new decimal(new int[] { 2024, 0, 0, 0 });
            // 
            // numWebPort
            // 
            numWebPort.Location = new Point(120, 58);
            numWebPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numWebPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numWebPort.Name = "numWebPort";
            numWebPort.Size = new Size(120, 23);
            numWebPort.TabIndex = 3;
            numWebPort.Value = new decimal(new int[] { 8090, 0, 0, 0 });
            // 
            // chkAutoStart
            // 
            chkAutoStart.Location = new Point(23, 95);
            chkAutoStart.Name = "chkAutoStart";
            chkAutoStart.Size = new Size(188, 33);
            chkAutoStart.TabIndex = 4;
            chkAutoStart.Text = "Auto-start with Windows";
            chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // chkStartMin
            // 
            chkStartMin.Location = new Point(23, 124);
            chkStartMin.Name = "chkStartMin";
            chkStartMin.Size = new Size(217, 24);
            chkStartMin.TabIndex = 5;
            chkStartMin.Text = "Start Minimized";
            chkStartMin.UseVisualStyleBackColor = true;
            // 
            // chkCheckUpdates
            // 
            chkCheckUpdates.Location = new Point(23, 150);
            chkCheckUpdates.Name = "chkCheckUpdates";
            chkCheckUpdates.Size = new Size(217, 24);
            chkCheckUpdates.TabIndex = 6;
            chkCheckUpdates.Text = "Check for Updates on Start";
            chkCheckUpdates.UseVisualStyleBackColor = true;
            // 
            // btnCheckUpdate
            // 
            btnCheckUpdate.Location = new Point(23, 180);
            btnCheckUpdate.Name = "btnCheckUpdate";
            btnCheckUpdate.Size = new Size(136, 27);
            btnCheckUpdate.TabIndex = 7;
            btnCheckUpdate.Text = "Check for Updates";
            btnCheckUpdate.UseVisualStyleBackColor = true;
            btnCheckUpdate.Click += btnCheckUpdate_Click;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(84, 215);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 27);
            btnOK.TabIndex = 8;
            btnOK.Text = "Save";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(165, 215);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 27);
            btnCancel.TabIndex = 9;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // SettingsForm
            // 
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            ClientSize = new Size(270, 260);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(btnCheckUpdate);
            Controls.Add(chkCheckUpdates);
            Controls.Add(chkStartMin);
            Controls.Add(chkAutoStart);
            Controls.Add(numWebPort);
            Controls.Add(numAppPort);
            Controls.Add(lblWebPort);
            Controls.Add(lblAppPort);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)numAppPort).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWebPort).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
