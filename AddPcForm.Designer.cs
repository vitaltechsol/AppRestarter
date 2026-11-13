namespace AppRestarter
{
    partial class AddPcForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblDiscoveryStatus;
        private System.Windows.Forms.ListBox listDiscovered;
        private System.Windows.Forms.Button btnRescan;
        private System.Windows.Forms.Panel pnlDiscoverySeparator;
        private System.Windows.Forms.Label lblManualHint;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblName = new Label();
            lblIp = new Label();
            txtName = new TextBox();
            txtIp = new TextBox();
            btnSave = new Button();
            btnCancel = new Button();
            pnlDiscoverySeparator = new Panel();
            lblDiscoveryStatus = new Label();
            btnRescan = new Button();
            listDiscovered = new ListBox();
            lblManualHint = new Label();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTitle.Location = new Point(18, 14);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(161, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Add Remote PC";
            // 
            // lblDiscoveryStatus
            // 
            lblDiscoveryStatus.AutoSize = true;
            lblDiscoveryStatus.Location = new Point(18, 46);
            lblDiscoveryStatus.Name = "lblDiscoveryStatus";
            lblDiscoveryStatus.Size = new Size(200, 15);
            lblDiscoveryStatus.TabIndex = 10;
            lblDiscoveryStatus.Text = "Scanning for PCs on network...";
            // 
            // btnRescan
            // 
            btnRescan.Location = new Point(286, 42);
            btnRescan.Name = "btnRescan";
            btnRescan.Size = new Size(78, 25);
            btnRescan.TabIndex = 11;
            btnRescan.Text = "Rescan";
            btnRescan.UseVisualStyleBackColor = true;
            btnRescan.Click += btnRescan_Click;
            // 
            // listDiscovered
            // 
            listDiscovered.FormattingEnabled = true;
            listDiscovered.Location = new Point(18, 68);
            listDiscovered.Name = "listDiscovered";
            listDiscovered.Size = new Size(346, 95);
            listDiscovered.TabIndex = 12;
            listDiscovered.SelectedIndexChanged += listDiscovered_SelectedIndexChanged;
            // 
            // pnlDiscoverySeparator
            // 
            pnlDiscoverySeparator.BackColor = System.Drawing.Color.FromArgb(55, 65, 81);
            pnlDiscoverySeparator.Location = new Point(18, 173);
            pnlDiscoverySeparator.Name = "pnlDiscoverySeparator";
            pnlDiscoverySeparator.Size = new Size(346, 1);
            pnlDiscoverySeparator.TabIndex = 13;
            // 
            // lblManualHint
            // 
            lblManualHint.AutoSize = false;
            lblManualHint.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            lblManualHint.Location = new Point(18, 181);
            lblManualHint.Name = "lblManualHint";
            lblManualHint.Size = new Size(346, 18);
            lblManualHint.TabIndex = 14;
            lblManualHint.Text = "\u2014 or enter manually \u2014";
            lblManualHint.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblName
            // 
            lblName.Location = new Point(18, 206);
            lblName.Name = "lblName";
            lblName.Size = new Size(140, 23);
            lblName.TabIndex = 1;
            lblName.Text = "PC Name";
            lblName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtName
            // 
            txtName.Location = new Point(18, 227);
            txtName.Name = "txtName";
            txtName.Size = new Size(346, 23);
            txtName.TabIndex = 0;
            // 
            // lblIp
            // 
            lblIp.Location = new Point(18, 259);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(200, 23);
            lblIp.TabIndex = 2;
            lblIp.Text = "PC IP (e.g. 192.168.1.10)";
            lblIp.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtIp
            // 
            txtIp.Location = new Point(18, 280);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(346, 23);
            txtIp.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(198, 316);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(78, 30);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(286, 316);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(78, 30);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // AddPcForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(384, 358);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(txtIp);
            Controls.Add(lblIp);
            Controls.Add(txtName);
            Controls.Add(lblName);
            Controls.Add(lblManualHint);
            Controls.Add(pnlDiscoverySeparator);
            Controls.Add(listDiscovered);
            Controls.Add(btnRescan);
            Controls.Add(lblDiscoveryStatus);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddPcForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add / Edit PC";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
