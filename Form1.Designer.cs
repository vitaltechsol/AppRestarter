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
            SuspendLayout();
            // 
            // AppFlowLayoutPanel
            // 
            AppFlowLayoutPanel.Location = new Point(12, 12);
            AppFlowLayoutPanel.Name = "AppFlowLayoutPanel";
            AppFlowLayoutPanel.Size = new Size(510, 328);
            AppFlowLayoutPanel.TabIndex = 0;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(12, 361);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(504, 61);
            txtLog.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(535, 450);
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
    }
}