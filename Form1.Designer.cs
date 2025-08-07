namespace AppRestarter
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            AppFlowLayoutPanel = new FlowLayoutPanel();
            txtLog = new TextBox();
            btnReload = new Button();
            btnAddApp = new Button();
            SuspendLayout();
            // 
            // AppFlowLayoutPanel
            // 
            AppFlowLayoutPanel.Location = new Point(12, 40);
            AppFlowLayoutPanel.Name = "AppFlowLayoutPanel";
            AppFlowLayoutPanel.Size = new Size(510, 300);
            AppFlowLayoutPanel.TabIndex = 0;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(12, 361);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(504, 61);
            txtLog.TabIndex = 1;
            // 
            // btnReload
            // 
            btnReload.Location = new Point(410, 12);
            btnReload.Name = "btnReload";
            btnReload.Size = new Size(112, 23);
            btnReload.TabIndex = 2;
            btnReload.Text = "Reload Config";
            btnReload.UseVisualStyleBackColor = true;
            btnReload.Click += btnReload_Click;
            // 
            // btnAddApp
            // 
            btnAddApp.Location = new Point(12, 12);
            btnAddApp.Name = "btnAddApp";
            btnAddApp.Size = new Size(75, 23);
            btnAddApp.TabIndex = 3;
            btnAddApp.Text = "Add New";
            btnAddApp.UseVisualStyleBackColor = true;
            btnAddApp.Click += btnAddApp_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(535, 450);
            Controls.Add(btnAddApp);
            Controls.Add(btnReload);
            Controls.Add(txtLog);
            Controls.Add(AppFlowLayoutPanel);
            Name = "Form1";
            Text = "AppRestarter";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FlowLayoutPanel AppFlowLayoutPanel;
        private TextBox txtLog;
        private Button btnReload;
        private Button btnAddApp;
    }
}