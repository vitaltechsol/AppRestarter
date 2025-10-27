namespace AppRestarter
{
    partial class GroupsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lstGroups = new System.Windows.Forms.ListBox();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lbl = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRename = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lstGroups
            // 
            this.lstGroups.FormattingEnabled = true;
            this.lstGroups.ItemHeight = 15;
            this.lstGroups.Location = new System.Drawing.Point(12, 12);
            this.lstGroups.Name = "lstGroups";
            this.lstGroups.Size = new System.Drawing.Size(240, 184);
            this.lstGroups.TabIndex = 0;
            this.lstGroups.SelectedIndexChanged += new System.EventHandler(this.lstGroups_SelectedIndexChanged);
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(12, 221);
            this.txtName.Name = "txtName";
            this.txtName.PlaceholderText = "Group Name";
            this.txtName.Size = new System.Drawing.Size(240, 23);
            this.txtName.TabIndex = 1;
            // 
            // lbl
            // 
            this.lbl.AutoSize = true;
            this.lbl.Location = new System.Drawing.Point(12, 203);
            this.lbl.Name = "lbl";
            this.lbl.Size = new System.Drawing.Size(77, 15);
            this.lbl.TabIndex = 2;
            this.lbl.Text = "Group Name";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(270, 12);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(110, 30);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add Group";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRename
            // 
            this.btnRename.Location = new System.Drawing.Point(270, 48);
            this.btnRename.Name = "btnRename";
            this.btnRename.Size = new System.Drawing.Size(110, 30);
            this.btnRename.TabIndex = 4;
            this.btnRename.Text = "Rename Group";
            this.btnRename.Click += new System.EventHandler(this.btnRename_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(270, 84);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(110, 30);
            this.btnDelete.TabIndex = 5;
            this.btnDelete.Text = "Delete Group";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(270, 166);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(110, 30);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // GroupsForm
            // 
            this.AcceptButton = this.btnAdd;
            this.ClientSize = new System.Drawing.Size(394, 258);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnRename);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lbl);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lstGroups);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "GroupsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Groups";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lstGroups;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lbl;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClose;
    }
}
