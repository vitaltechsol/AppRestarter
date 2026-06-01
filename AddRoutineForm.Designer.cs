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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddRoutineForm));
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
            groupSteps.SuspendLayout();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(12, 15);
            lblName.Name = "lblName";
            lblName.Size = new Size(88, 15);
            lblName.TabIndex = 0;
            lblName.Text = "Routine Name:";
            // 
            // txtName
            // 
            txtName.Location = new Point(120, 12);
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
            groupSteps.Location = new Point(12, 50);
            groupSteps.Name = "groupSteps";
            groupSteps.Size = new Size(640, 300);
            groupSteps.TabIndex = 2;
            groupSteps.TabStop = false;
            groupSteps.Text = "Steps (executed in order)";
            // 
            // btnMoveDown
            // 
            btnMoveDown.Location = new Point(530, 120);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new Size(100, 30);
            btnMoveDown.TabIndex = 5;
            btnMoveDown.Text = "Move Down";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += btnMoveDown_Click;
            // 
            // btnMoveUp
            // 
            btnMoveUp.Location = new Point(530, 84);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new Size(100, 30);
            btnMoveUp.TabIndex = 4;
            btnMoveUp.Text = "Move Up";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += btnMoveUp_Click;
            // 
            // btnRemoveStep
            // 
            btnRemoveStep.Location = new Point(530, 156);
            btnRemoveStep.Name = "btnRemoveStep";
            btnRemoveStep.Size = new Size(100, 30);
            btnRemoveStep.TabIndex = 3;
            btnRemoveStep.Text = "Remove";
            btnRemoveStep.UseVisualStyleBackColor = true;
            btnRemoveStep.Click += btnRemoveStep_Click;
            // 
            // btnEditStep
            // 
            btnEditStep.Location = new Point(530, 48);
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
            btnSave.Location = new Point(451, 360);
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
            btnCancel.Location = new Point(557, 360);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 32);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(12, 360);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(100, 32);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Visible = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // AddRoutineForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(664, 404);
            Controls.Add(btnDelete);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(groupSteps);
            Controls.Add(txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            //Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddRoutineForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add New Routine";
            groupSteps.ResumeLayout(false);
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
    }
}
