namespace AppRestarter
{
    partial class AddRoutineForm
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
            lblName = new Label();
            txtName = new TextBox();
            groupSteps = new GroupBox();
            btnMoveDown = new Button();
            btnMoveUp = new Button();
            btnRemoveStep = new Button();
            btnEditStep = new Button();
            btnAddStep = new Button();
            lstSteps = new ListBox();
            btnSave = new Button();
            btnCancel = new Button();
            btnDelete = new Button();
            grpMode = new GroupBox();
            rdoRemote = new RadioButton();
            rdoLocal = new RadioButton();
            lblRemotePc = new Label();
            cmbRemotePc = new ComboBox();
            btnBrowseRemote = new Button();
            lblRemoteInfo = new Label();
            groupSteps.SuspendLayout();
            grpMode.SuspendLayout();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(12, 85);
            lblName.Name = "lblName";
            lblName.Size = new Size(86, 15);
            lblName.TabIndex = 0;
            lblName.Text = "Routine Name:";
            // 
            // txtName
            // 
            txtName.Location = new Point(120, 82);
            txtName.Name = "txtName";
            txtName.Size = new Size(400, 23);
            txtName.TabIndex = 1;
            // 
            // groupSteps
            // 
            groupSteps.Controls.Add(btnMoveDown);
            groupSteps.Controls.Add(btnMoveUp);
            groupSteps.Controls.Add(btnRemoveStep);
            groupSteps.Controls.Add(btnEditStep);
            groupSteps.Controls.Add(btnAddStep);
            groupSteps.Controls.Add(lstSteps);
            groupSteps.Location = new Point(12, 120);
            groupSteps.Name = "groupSteps";
            groupSteps.Size = new Size(640, 300);
            groupSteps.TabIndex = 2;
            groupSteps.TabStop = false;
            groupSteps.Text = "Steps (executed in order)";
            // 
            // btnMoveDown
            // 
            btnMoveDown.Location = new Point(530, 161);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new Size(100, 30);
            btnMoveDown.TabIndex = 5;
            btnMoveDown.Text = "Move Down";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += btnMoveDown_Click;
            // 
            // btnMoveUp
            // 
            btnMoveUp.Location = new Point(530, 125);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new Size(100, 30);
            btnMoveUp.TabIndex = 4;
            btnMoveUp.Text = "Move Up";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += btnMoveUp_Click;
            // 
            // btnRemoveStep
            // 
            btnRemoveStep.Location = new Point(530, 197);
            btnRemoveStep.Name = "btnRemoveStep";
            btnRemoveStep.Size = new Size(100, 30);
            btnRemoveStep.TabIndex = 3;
            btnRemoveStep.Text = "Remove";
            btnRemoveStep.UseVisualStyleBackColor = true;
            btnRemoveStep.Click += btnRemoveStep_Click;
            // 
            // btnEditStep
            // 
            btnEditStep.Location = new Point(530, 89);
            btnEditStep.Name = "btnEditStep";
            btnEditStep.Size = new Size(100, 30);
            btnEditStep.TabIndex = 2;
            btnEditStep.Text = "Edit";
            btnEditStep.UseVisualStyleBackColor = true;
            btnEditStep.Click += btnEditStep_Click;
            // 
            // btnAddStep
            // 
            btnAddStep.Location = new Point(530, 22);
            btnAddStep.Name = "btnAddStep";
            btnAddStep.Size = new Size(100, 30);
            btnAddStep.TabIndex = 1;
            btnAddStep.Text = "Add Step";
            btnAddStep.UseVisualStyleBackColor = true;
            btnAddStep.Click += btnAddStep_Click;
            // 
            // lstSteps
            // 
            lstSteps.FormattingEnabled = true;
            lstSteps.ItemHeight = 15;
            lstSteps.Location = new Point(6, 22);
            lstSteps.Name = "lstSteps";
            lstSteps.Size = new Size(510, 259);
            lstSteps.TabIndex = 0;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(451, 430);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 32);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(557, 430);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 32);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(12, 430);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(100, 32);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Visible = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // grpMode
            // 
            grpMode.Controls.Add(rdoRemote);
            grpMode.Controls.Add(rdoLocal);
            grpMode.Location = new Point(12, 12);
            grpMode.Name = "grpMode";
            grpMode.Size = new Size(640, 60);
            grpMode.TabIndex = 6;
            grpMode.TabStop = false;
            grpMode.Text = "Routine Type";
            // 
            // rdoRemote
            // 
            rdoRemote.AutoSize = true;
            rdoRemote.Location = new Point(180, 25);
            rdoRemote.Name = "rdoRemote";
            rdoRemote.Size = new Size(177, 19);
            rdoRemote.TabIndex = 1;
            rdoRemote.Text = "Remote Routine Reference";
            rdoRemote.UseVisualStyleBackColor = true;
            rdoRemote.CheckedChanged += rdoRemote_CheckedChanged;
            // 
            // rdoLocal
            // 
            rdoLocal.AutoSize = true;
            rdoLocal.Checked = true;
            rdoLocal.Location = new Point(20, 25);
            rdoLocal.Name = "rdoLocal";
            rdoLocal.Size = new Size(97, 19);
            rdoLocal.TabIndex = 0;
            rdoLocal.TabStop = true;
            rdoLocal.Text = "Local Routine";
            rdoLocal.UseVisualStyleBackColor = true;
            rdoLocal.CheckedChanged += rdoLocal_CheckedChanged;
            // 
            // lblRemotePc
            // 
            lblRemotePc.AutoSize = true;
            lblRemotePc.Location = new Point(12, 85);
            lblRemotePc.Name = "lblRemotePc";
            lblRemotePc.Size = new Size(55, 15);
            lblRemotePc.TabIndex = 7;
            lblRemotePc.Text = "Select PC:";
            lblRemotePc.Visible = false;
            // 
            // cmbRemotePc
            // 
            cmbRemotePc.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRemotePc.FormattingEnabled = true;
            cmbRemotePc.Location = new Point(120, 82);
            cmbRemotePc.Name = "cmbRemotePc";
            cmbRemotePc.Size = new Size(300, 23);
            cmbRemotePc.TabIndex = 8;
            cmbRemotePc.Visible = false;
            // 
            // btnBrowseRemote
            // 
            btnBrowseRemote.Location = new Point(430, 80);
            btnBrowseRemote.Name = "btnBrowseRemote";
            btnBrowseRemote.Size = new Size(120, 27);
            btnBrowseRemote.TabIndex = 9;
            btnBrowseRemote.Text = "Browse Routines";
            btnBrowseRemote.UseVisualStyleBackColor = true;
            btnBrowseRemote.Visible = false;
            btnBrowseRemote.Click += btnBrowseRemote_Click;
            // 
            // lblRemoteInfo
            // 
            lblRemoteInfo.AutoSize = false;
            lblRemoteInfo.Location = new Point(120, 120);
            lblRemoteInfo.Name = "lblRemoteInfo";
            lblRemoteInfo.Size = new Size(500, 80);
            lblRemoteInfo.TabIndex = 10;
            lblRemoteInfo.Text = "No routine selected. Click \"Browse Routines\" to select a routine from the remote PC.";
            lblRemoteInfo.Visible = false;
            // 
            // AddRoutineForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(664, 474);
            Controls.Add(lblRemoteInfo);
            Controls.Add(btnBrowseRemote);
            Controls.Add(cmbRemotePc);
            Controls.Add(lblRemotePc);
            Controls.Add(grpMode);
            Controls.Add(btnDelete);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(groupSteps);
            Controls.Add(txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddRoutineForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add New Routine";
            groupSteps.ResumeLayout(false);
            grpMode.ResumeLayout(false);
            grpMode.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblName;
        private TextBox txtName;
        private GroupBox groupSteps;
        private ListBox lstSteps;
        private Button btnAddStep;
        private Button btnEditStep;
        private Button btnRemoveStep;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private Button btnSave;
        private Button btnCancel;
        private Button btnDelete;
        private GroupBox grpMode;
        private RadioButton rdoLocal;
        private RadioButton rdoRemote;
        private Label lblRemotePc;
        private ComboBox cmbRemotePc;
        private Button btnBrowseRemote;
        private Label lblRemoteInfo;
    }
}
