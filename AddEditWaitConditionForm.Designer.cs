namespace AppRestarter
{
    partial class AddEditWaitConditionForm
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
            cboConditionType = new ComboBox();
            lblConditionType = new Label();
            btnSave = new Button();
            btnCancel = new Button();
            lblTarget = new Label();
            cboTarget = new ComboBox();
            lblDuration = new Label();
            numDuration = new NumericUpDown();
            lblProcessName = new Label();
            txtProcessName = new TextBox();
            ((System.ComponentModel.ISupportInitialize)numDuration).BeginInit();
            SuspendLayout();
            // 
            // cboConditionType
            // 
            cboConditionType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboConditionType.FormattingEnabled = true;
            cboConditionType.Location = new Point(12, 29);
            cboConditionType.Name = "cboConditionType";
            cboConditionType.Size = new Size(260, 23);
            cboConditionType.TabIndex = 0;
            cboConditionType.SelectedIndexChanged += cboConditionType_SelectedIndexChanged;
            // 
            // lblConditionType
            // 
            lblConditionType.AutoSize = true;
            lblConditionType.Location = new Point(12, 9);
            lblConditionType.Name = "lblConditionType";
            lblConditionType.Size = new Size(87, 15);
            lblConditionType.TabIndex = 1;
            lblConditionType.Text = "Condition Type";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(116, 226);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(197, 226);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblTarget
            // 
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(12, 60);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(42, 15);
            lblTarget.TabIndex = 4;
            lblTarget.Text = "Target:";
            // 
            // cboTarget
            // 
            cboTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTarget.FormattingEnabled = true;
            cboTarget.Location = new Point(12, 80);
            cboTarget.Name = "cboTarget";
            cboTarget.Size = new Size(260, 23);
            cboTarget.TabIndex = 5;
            // 
            // lblDuration
            // 
            lblDuration.AutoSize = true;
            lblDuration.Location = new Point(12, 60);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new Size(110, 15);
            lblDuration.TabIndex = 6;
            lblDuration.Text = "Duration (seconds):";
            // 
            // numDuration
            // 
            numDuration.Location = new Point(12, 80);
            numDuration.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
            numDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDuration.Name = "numDuration";
            numDuration.Size = new Size(120, 23);
            numDuration.TabIndex = 7;
            numDuration.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblProcessName
            // 
            lblProcessName.AutoSize = true;
            lblProcessName.Location = new Point(12, 60);
            lblProcessName.Name = "lblProcessName";
            lblProcessName.Size = new Size(85, 15);
            lblProcessName.TabIndex = 8;
            lblProcessName.Text = "Process Name:";
            // 
            // txtProcessName
            // 
            txtProcessName.Location = new Point(12, 80);
            txtProcessName.Name = "txtProcessName";
            txtProcessName.Size = new Size(260, 23);
            txtProcessName.TabIndex = 9;
            // 
            // AddEditWaitConditionForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(284, 261);
            Controls.Add(txtProcessName);
            Controls.Add(lblProcessName);
            Controls.Add(numDuration);
            Controls.Add(lblDuration);
            Controls.Add(cboTarget);
            Controls.Add(lblTarget);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(lblConditionType);
            Controls.Add(cboConditionType);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddEditWaitConditionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add/Edit Wait Condition";
            ((System.ComponentModel.ISupportInitialize)numDuration).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cboConditionType;
        private System.Windows.Forms.Label lblConditionType;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.ComboBox cboTarget;
        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.NumericUpDown numDuration;
        private System.Windows.Forms.Label lblProcessName;
        private System.Windows.Forms.TextBox txtProcessName;
    }
}
