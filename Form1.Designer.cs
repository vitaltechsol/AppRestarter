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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            AppFlowLayoutPanel = new FlowLayoutPanel();
            txtLog = new TextBox();
            btnAddApp = new Button();
            label1 = new Label();
            btnOpenWeb = new Button();
            panelLeftNav = new Panel();
            lblNavSettings = new Label();
            btnNavSettings = new Button();
            lblNavPcs = new Label();
            btnNavPcs = new Button();
            lblNavApps = new Label();
            btnNavApps = new Button();
            panelLeftNav.SuspendLayout();
            SuspendLayout();
            // 
            // AppFlowLayoutPanel
            // 
            AppFlowLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            AppFlowLayoutPanel.AutoScroll = true;
            AppFlowLayoutPanel.BackColor = SystemColors.ControlDarkDark;
            AppFlowLayoutPanel.Location = new Point(96, 40);
            AppFlowLayoutPanel.Name = "AppFlowLayoutPanel";
            AppFlowLayoutPanel.Size = new Size(626, 340);
            AppFlowLayoutPanel.TabIndex = 0;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Location = new Point(12, 386);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(710, 61);
            txtLog.TabIndex = 1;
            // 
            // btnAddApp
            // 
            btnAddApp.BackColor = SystemColors.HotTrack;
            btnAddApp.FlatStyle = FlatStyle.Popup;
            btnAddApp.ForeColor = SystemColors.ButtonFace;
            btnAddApp.Location = new Point(9, 8);
            btnAddApp.Name = "btnAddApp";
            btnAddApp.Size = new Size(98, 23);
            btnAddApp.TabIndex = 0;
            btnAddApp.Text = "Add New App";
            btnAddApp.UseVisualStyleBackColor = false;
            btnAddApp.Click += btnAddApp_Click;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.ForeColor = SystemColors.ButtonShadow;
            label1.Location = new Point(685, 12);
            label1.Name = "label1";
            label1.Size = new Size(37, 15);
            label1.TabIndex = 4;
            label1.Text = "v1.5.0";
            // 
            // btnOpenWeb
            // 
            btnOpenWeb.BackColor = SystemColors.WindowFrame;
            btnOpenWeb.FlatStyle = FlatStyle.Popup;
            btnOpenWeb.ForeColor = SystemColors.ButtonFace;
            btnOpenWeb.Location = new Point(127, 8);
            btnOpenWeb.Name = "btnOpenWeb";
            btnOpenWeb.Size = new Size(100, 23);
            btnOpenWeb.TabIndex = 5;
            btnOpenWeb.Text = "Web Interface";
            btnOpenWeb.UseVisualStyleBackColor = false;
            btnOpenWeb.Click += btnOpenWeb_Click;
            // 
            // panelLeftNav
            // 
            panelLeftNav.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panelLeftNav.BackColor = Color.FromArgb(32, 32, 32);
            panelLeftNav.Controls.Add(lblNavSettings);
            panelLeftNav.Controls.Add(btnNavSettings);
            panelLeftNav.Controls.Add(lblNavPcs);
            panelLeftNav.Controls.Add(btnNavPcs);
            panelLeftNav.Controls.Add(lblNavApps);
            panelLeftNav.Controls.Add(btnNavApps);
            panelLeftNav.Location = new Point(12, 40);
            panelLeftNav.Name = "panelLeftNav";
            panelLeftNav.Size = new Size(78, 340);
            panelLeftNav.TabIndex = 7;
            // 
            // lblNavSettings
            // 
            lblNavSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblNavSettings.AutoSize = true;
            lblNavSettings.Font = new Font("Segoe UI", 7F);
            lblNavSettings.ForeColor = SystemColors.ControlLightLight;
            lblNavSettings.Location = new Point(18, 261);
            lblNavSettings.Name = "lblNavSettings";
            lblNavSettings.Size = new Size(39, 12);
            lblNavSettings.TabIndex = 5;
            lblNavSettings.Text = "Settings";
            lblNavSettings.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnNavSettings
            // 
            btnNavSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNavSettings.CausesValidation = false;
            btnNavSettings.FlatAppearance.BorderColor = Color.Black;
            btnNavSettings.FlatAppearance.BorderSize = 0;
            btnNavSettings.FlatAppearance.MouseDownBackColor = Color.Black;
            btnNavSettings.FlatStyle = FlatStyle.Flat;
            btnNavSettings.Font = new Font("Segoe UI Emoji", 12F);
            btnNavSettings.ForeColor = SystemColors.ControlLightLight;
            btnNavSettings.Location = new Point(13, 276);
            btnNavSettings.Name = "btnNavSettings";
            btnNavSettings.Size = new Size(52, 52);
            btnNavSettings.TabIndex = 4;
            btnNavSettings.Text = "⚙️";
            btnNavSettings.UseVisualStyleBackColor = true;
            btnNavSettings.Click += btnSettings_Click;
            // 
            // lblNavPcs
            // 
            lblNavPcs.AutoSize = true;
            lblNavPcs.Font = new Font("Segoe UI", 7F);
            lblNavPcs.ForeColor = SystemColors.ControlLightLight;
            lblNavPcs.Location = new Point(27, 162);
            lblNavPcs.Name = "lblNavPcs";
            lblNavPcs.Size = new Size(21, 12);
            lblNavPcs.TabIndex = 3;
            lblNavPcs.Text = "PCs";
            lblNavPcs.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnNavPcs
            // 
            btnNavPcs.FlatAppearance.BorderSize = 0;
            btnNavPcs.FlatStyle = FlatStyle.Flat;
            btnNavPcs.Font = new Font("Segoe UI Emoji", 12F);
            btnNavPcs.ForeColor = SystemColors.ControlLightLight;
            btnNavPcs.Location = new Point(13, 107);
            btnNavPcs.Name = "btnNavPcs";
            btnNavPcs.Size = new Size(52, 52);
            btnNavPcs.TabIndex = 2;
            btnNavPcs.Text = "💻";
            btnNavPcs.UseVisualStyleBackColor = true;
            btnNavPcs.Click += btnNavPcs_Click;
            // 
            // lblNavApps
            // 
            lblNavApps.AutoSize = true;
            lblNavApps.Font = new Font("Segoe UI", 7F);
            lblNavApps.ForeColor = SystemColors.ControlLightLight;
            lblNavApps.Location = new Point(24, 76);
            lblNavApps.Name = "lblNavApps";
            lblNavApps.Size = new Size(27, 12);
            lblNavApps.TabIndex = 1;
            lblNavApps.Text = "Apps";
            lblNavApps.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnNavApps
            // 
            btnNavApps.FlatAppearance.BorderSize = 0;
            btnNavApps.FlatStyle = FlatStyle.Flat;
            btnNavApps.Font = new Font("Segoe UI Emoji", 12F);
            btnNavApps.ForeColor = SystemColors.ControlLightLight;
            btnNavApps.Location = new Point(13, 21);
            btnNavApps.Name = "btnNavApps";
            btnNavApps.Size = new Size(52, 52);
            btnNavApps.TabIndex = 1;
            btnNavApps.Text = "🗃️";
            btnNavApps.UseVisualStyleBackColor = true;
            btnNavApps.Click += btnNavApps_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.WindowText;
            ClientSize = new Size(734, 461);
            Controls.Add(panelLeftNav);
            Controls.Add(btnOpenWeb);
            Controls.Add(label1);
            Controls.Add(btnAddApp);
            Controls.Add(txtLog);
            Controls.Add(AppFlowLayoutPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "AppRestarter";
            Load += Form1_Load;
            panelLeftNav.ResumeLayout(false);
            panelLeftNav.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel AppFlowLayoutPanel;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnAddApp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOpenWeb;
        private System.Windows.Forms.Panel panelLeftNav;
        private System.Windows.Forms.Button btnNavApps;
        private System.Windows.Forms.Button btnNavPcs;
        private System.Windows.Forms.Button btnNavSettings;
        private System.Windows.Forms.Label lblNavApps;
        private System.Windows.Forms.Label lblNavPcs;
        private System.Windows.Forms.Label lblNavSettings;
    }
}
