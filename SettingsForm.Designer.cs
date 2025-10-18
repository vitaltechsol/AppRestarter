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
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.lblAppPort = new System.Windows.Forms.Label();
            this.lblWebPort = new System.Windows.Forms.Label();
            this.numAppPort = new System.Windows.Forms.NumericUpDown();
            this.numWebPort = new System.Windows.Forms.NumericUpDown();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.chkStartMin = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numAppPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWebPort)).BeginInit();
            this.SuspendLayout();
            // 
            // lblAppPort
            // 
            this.lblAppPort.AutoSize = true;
            this.lblAppPort.Location = new System.Drawing.Point(20, 20);
            this.lblAppPort.Name = "lblAppPort";
            this.lblAppPort.Size = new System.Drawing.Size(58, 15);
            this.lblAppPort.TabIndex = 0;
            this.lblAppPort.Text = "App Port:";
            // 
            // lblWebPort
            // 
            this.lblWebPort.AutoSize = true;
            this.lblWebPort.Location = new System.Drawing.Point(20, 60);
            this.lblWebPort.Name = "lblWebPort";
            this.lblWebPort.Size = new System.Drawing.Size(60, 15);
            this.lblWebPort.TabIndex = 1;
            this.lblWebPort.Text = "Web Port:";
            // 
            // numAppPort
            // 
            this.numAppPort.Location = new System.Drawing.Point(120, 18);
            this.numAppPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numAppPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numAppPort.Name = "numAppPort";
            this.numAppPort.Size = new System.Drawing.Size(120, 23);
            this.numAppPort.TabIndex = 2;
            this.numAppPort.Value = new decimal(new int[] { 2024, 0, 0, 0 });
            // 
            // numWebPort
            // 
            this.numWebPort.Location = new System.Drawing.Point(120, 58);
            this.numWebPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numWebPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numWebPort.Name = "numWebPort";
            this.numWebPort.Size = new System.Drawing.Size(120, 23);
            this.numWebPort.TabIndex = 3;
            this.numWebPort.Value = new decimal(new int[] { 8080, 0, 0, 0 });
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Location = new System.Drawing.Point(23, 100);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(162, 19);
            this.chkAutoStart.TabIndex = 4;
            this.chkAutoStart.Text = "Auto-start with Windows";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // chkStartMin
            // 
            this.chkStartMin.AutoSize = true;
            this.chkStartMin.Location = new System.Drawing.Point(23, 130);
            this.chkStartMin.Name = "chkStartMin";
            this.chkStartMin.Size = new System.Drawing.Size(113, 19);
            this.chkStartMin.TabIndex = 5;
            this.chkStartMin.Text = "Start Minimized";
            this.chkStartMin.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(84, 175);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 27);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "Save";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(165, 175);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 27);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(270, 220);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.chkStartMin);
            this.Controls.Add(this.chkAutoStart);
            this.Controls.Add(this.numWebPort);
            this.Controls.Add(this.numAppPort);
            this.Controls.Add(this.lblWebPort);
            this.Controls.Add(this.lblAppPort);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numAppPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWebPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
