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
            AppFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            txtLog = new System.Windows.Forms.TextBox();
            btnReload = new System.Windows.Forms.Button();
            btnAddApp = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            btnOpenWeb = new System.Windows.Forms.Button();
            btnSettings = new System.Windows.Forms.Button();
            panelLeftNav = new System.Windows.Forms.Panel();
            btnNavPcs = new System.Windows.Forms.Button();
            btnNavApps = new System.Windows.Forms.Button();
            panelLeftNav.SuspendLayout();
            SuspendLayout();
            // 
            // AppFlowLayoutPanel
            // 
            AppFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                    | System.Windows.Forms.AnchorStyles.Left)
                                    | System.Windows.Forms.AnchorStyles.Right)));
            AppFlowLayoutPanel.AutoScroll = true;
            AppFlowLayoutPanel.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            AppFlowLayoutPanel.Location = new System.Drawing.Point(96, 40);
            AppFlowLayoutPanel.Name = "AppFlowLayoutPanel";
            AppFlowLayoutPanel.Size = new System.Drawing.Size(626, 340);
            AppFlowLayoutPanel.TabIndex = 0;
            // 
            // txtLog
            // 
            txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                                    | System.Windows.Forms.AnchorStyles.Right)));
            txtLog.Location = new System.Drawing.Point(12, 386);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(710, 61);
            txtLog.TabIndex = 1;
            // 
            // btnReload
            // 
            btnReload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnReload.BackColor = System.Drawing.SystemColors.WindowFrame;
            btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            btnReload.ForeColor = System.Drawing.SystemColors.ButtonFace;
            btnReload.Location = new System.Drawing.Point(289, 8);
            btnReload.Name = "btnReload";
            btnReload.Size = new System.Drawing.Size(99, 23);
            btnReload.TabIndex = 2;
            btnReload.Text = "Reload Config";
            btnReload.UseVisualStyleBackColor = false;
            btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnAddApp
            // 
            btnAddApp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnAddApp.BackColor = System.Drawing.SystemColors.HotTrack;
            btnAddApp.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            btnAddApp.ForeColor = System.Drawing.SystemColors.ButtonFace;
            btnAddApp.Location = new System.Drawing.Point(12, 9);
            btnAddApp.Name = "btnAddApp";
            btnAddApp.Size = new System.Drawing.Size(98, 23);
            btnAddApp.TabIndex = 3;
            btnAddApp.Text = "Add New App";
            btnAddApp.UseVisualStyleBackColor = false;
            btnAddApp.Click += new System.EventHandler(this.btnAddApp_Click);
            // 
            // label1
            // 
            label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            label1.AutoSize = true;
            label1.ForeColor = System.Drawing.SystemColors.ButtonShadow;
            label1.Location = new System.Drawing.Point(685, 12);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(37, 15);
            label1.TabIndex = 4;
            label1.Text = "v1.5.0";
            // 
            // btnOpenWeb
            // 
            btnOpenWeb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnOpenWeb.BackColor = System.Drawing.SystemColors.WindowFrame;
            btnOpenWeb.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            btnOpenWeb.ForeColor = System.Drawing.SystemColors.ButtonFace;
            btnOpenWeb.Location = new System.Drawing.Point(183, 8);
            btnOpenWeb.Name = "btnOpenWeb";
            btnOpenWeb.Size = new System.Drawing.Size(100, 23);
            btnOpenWeb.TabIndex = 5;
            btnOpenWeb.Text = "Web Interface";
            btnOpenWeb.UseVisualStyleBackColor = false;
            btnOpenWeb.Click += new System.EventHandler(this.btnOpenWeb_Click);
            // 
            // btnSettings
            // 
            btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            btnSettings.BackColor = System.Drawing.SystemColors.WindowFrame;
            btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            btnSettings.ForeColor = System.Drawing.SystemColors.ButtonFace;
            btnSettings.Location = new System.Drawing.Point(394, 8);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new System.Drawing.Size(68, 23);
            btnSettings.TabIndex = 6;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = false;
            btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // panelLeftNav
            // 
            panelLeftNav.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                    | System.Windows.Forms.AnchorStyles.Left)));
            panelLeftNav.BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
            panelLeftNav.Controls.Add(btnNavPcs);
            panelLeftNav.Controls.Add(btnNavApps);
            panelLeftNav.Location = new System.Drawing.Point(12, 40);
            panelLeftNav.Name = "panelLeftNav";
            panelLeftNav.Size = new System.Drawing.Size(78, 340);
            panelLeftNav.TabIndex = 7;
            // 
            // btnNavPcs
            // 
            btnNavPcs.FlatAppearance.BorderSize = 0;
            btnNavPcs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnNavPcs.Font = new System.Drawing.Font("Segoe UI Emoji", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnNavPcs.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            btnNavPcs.Location = new System.Drawing.Point(13, 91);
            btnNavPcs.Name = "btnNavPcs";
            btnNavPcs.Size = new System.Drawing.Size(52, 52);
            btnNavPcs.TabIndex = 1;
            btnNavPcs.Text = "💻";
            btnNavPcs.UseVisualStyleBackColor = true;
            btnNavPcs.Click += new System.EventHandler(this.btnNavPcs_Click);
            // 
            // btnNavApps
            // 
            btnNavApps.FlatAppearance.BorderSize = 0;
            btnNavApps.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnNavApps.Font = new System.Drawing.Font("Segoe UI Emoji", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnNavApps.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            btnNavApps.Location = new System.Drawing.Point(13, 21);
            btnNavApps.Name = "btnNavApps";
            btnNavApps.Size = new System.Drawing.Size(52, 52);
            btnNavApps.TabIndex = 0;
            btnNavApps.Text = "🧩";
            btnNavApps.UseVisualStyleBackColor = true;
            btnNavApps.Click += new System.EventHandler(this.btnNavApps_Click);
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.WindowText;
            ClientSize = new System.Drawing.Size(734, 461);
            Controls.Add(panelLeftNav);
            Controls.Add(btnSettings);
            Controls.Add(btnOpenWeb);
            Controls.Add(label1);
            Controls.Add(btnAddApp);
            Controls.Add(btnReload);
            Controls.Add(txtLog);
            Controls.Add(AppFlowLayoutPanel);
            Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            Name = "Form1";
            Text = "AppRestarter";
            Load += new System.EventHandler(this.Form1_Load);
            panelLeftNav.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel AppFlowLayoutPanel;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnAddApp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOpenWeb;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Panel panelLeftNav;
        private System.Windows.Forms.Button btnNavApps;
        private System.Windows.Forms.Button btnNavPcs;
    }
}
