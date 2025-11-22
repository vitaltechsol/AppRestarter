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
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTitle.Location = new Point(18, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(161, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Add / Edit Remote PC";
            // 
            // lblName
            // 
            lblName.Location = new Point(18, 46);
            lblName.Name = "lblName";
            lblName.Size = new Size(140, 23);
            lblName.TabIndex = 1;
            lblName.Text = "PC Name";
            lblName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblIp
            // 
            lblIp.Location = new Point(18, 108);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(140, 25);
            lblIp.TabIndex = 2;
            lblIp.Text = "PC IP (e.g. 192.168.1.10)";
            lblIp.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtName
            // 
            txtName.Location = new Point(18, 68);
            txtName.Name = "txtName";
            txtName.Size = new Size(346, 23);
            txtName.TabIndex = 0;
            // 
            // txtIp
            // 
            txtIp.Location = new Point(18, 130);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(346, 23);
            txtIp.TabIndex = 1;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(198, 170);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(78, 30);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(286, 170);
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
            ClientSize = new Size(384, 221);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(txtIp);
            Controls.Add(txtName);
            Controls.Add(lblIp);
            Controls.Add(lblName);
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
