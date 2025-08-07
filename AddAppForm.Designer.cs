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
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblProcess = new System.Windows.Forms.Label();
            this.txtProcess = new System.Windows.Forms.TextBox();
            this.lblPath = new System.Windows.Forms.Label();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblClientIP = new System.Windows.Forms.Label();
            this.txtClientIP = new System.Windows.Forms.TextBox();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();

            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(15, 15);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(120, 20);
            this.lblName.Text = "Application Name:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(150, 12);
            this.txtName.Size = new System.Drawing.Size(250, 23);
            // 
            // lblProcess
            // 
            this.lblProcess.Location = new System.Drawing.Point(15, 50);
            this.lblProcess.Name = "lblProcess";
            this.lblProcess.Size = new System.Drawing.Size(120, 20);
            this.lblProcess.Text = "Process Name:";
            // 
            // txtProcess
            // 
            this.txtProcess.Location = new System.Drawing.Point(150, 47);
            this.txtProcess.Size = new System.Drawing.Size(250, 23);
            // 
            // lblPath
            // 
            this.lblPath.Location = new System.Drawing.Point(15, 85);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(120, 20);
            this.lblPath.Text = "Restart Path:";
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(150, 82);
            this.txtPath.Size = new System.Drawing.Size(210, 23);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(370, 81);
            this.btnBrowse.Size = new System.Drawing.Size(70, 25);
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // lblClientIP
            // 
            this.lblClientIP.Location = new System.Drawing.Point(15, 120);
            this.lblClientIP.Name = "lblClientIP";
            this.lblClientIP.Size = new System.Drawing.Size(120, 20);
            this.lblClientIP.Text = "Client IP (optional):";
            // 
            // txtClientIP
            // 
            this.txtClientIP.Location = new System.Drawing.Point(150, 117);
            this.txtClientIP.Size = new System.Drawing.Size(250, 23);
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.Location = new System.Drawing.Point(150, 150);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(250, 24);
            this.chkAutoStart.Text = "Auto-start this app on launch";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(150, 190);
            this.btnSave.Size = new System.Drawing.Size(80, 30);
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(240, 190);
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            // btnDelete
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnDelete.Location = new System.Drawing.Point(330, 190);
            this.btnDelete.Size = new System.Drawing.Size(80, 30);
            this.btnDelete.Text = "Delete";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            this.btnDelete.Visible = false;
            // 
            // AddAppForm
            // 
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(470, 240);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblProcess);
            this.Controls.Add(this.txtProcess);
            this.Controls.Add(this.lblPath);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.lblClientIP);
            this.Controls.Add(this.txtClientIP);
            this.Controls.Add(this.chkAutoStart);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnDelete);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AddAppForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add New Application";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblProcess;
        private System.Windows.Forms.TextBox txtProcess;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblClientIP;
        private System.Windows.Forms.TextBox txtClientIP;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDelete;


        #endregion
    }
}
