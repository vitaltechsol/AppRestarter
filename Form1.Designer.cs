using System.Drawing;
using System.Windows.Forms;

namespace AppRestarter
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private FlowLayoutPanel AppFlowLayoutPanel;
        private TextBox txtLog;
        private Button btnAddApp;
        private Label label1;
        private Button btnOpenWeb;
        private Panel panelLeftNav;
        private Label lblNavSettings;
        private Button btnNavSettings;
        private Label lblNavPcs;
        private Button btnNavPcs;
        private Label lblNavApps;
        private Button btnNavApps;

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
            AppFlowLayoutPanel.BackColor = Color.FromArgb(15, 23, 42);
            AppFlowLayoutPanel.Location = new Point(84, 40);
            AppFlowLayoutPanel.Name = "AppFlowLayoutPanel";
            AppFlowLayoutPanel.Size = new Size(668, 340);
            AppFlowLayoutPanel.TabIndex = 0;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = Color.FromArgb(15, 23, 42);
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.ForeColor = Color.FromArgb(229, 231, 235);
            txtLog.Location = new Point(12, 386);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(740, 61);
            txtLog.TabIndex = 1;
            // 
            // btnAddApp
            // 
            btnAddApp.BackColor = Color.FromArgb(0, 192, 192);
            btnAddApp.FlatStyle = FlatStyle.Flat;
            btnAddApp.ForeColor = Color.FromArgb(15, 23, 42);
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
            label1.Location = new Point(715, 12);
            label1.Name = "label1";
            label1.Size = new Size(37, 15);
            label1.TabIndex = 4;
            label1.Text = "v1.7.0";
            // 
            // btnOpenWeb
            // 
            btnOpenWeb.BackColor = Color.FromArgb(31, 41, 55);
            btnOpenWeb.FlatStyle = FlatStyle.Flat;
            btnOpenWeb.ForeColor = Color.FromArgb(226, 232, 240);
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
            panelLeftNav.BackColor = Color.FromArgb(3, 7, 18);
            panelLeftNav.Controls.Add(lblNavSettings);
            panelLeftNav.Controls.Add(btnNavSettings);
            panelLeftNav.Controls.Add(lblNavPcs);
            panelLeftNav.Controls.Add(btnNavPcs);
            panelLeftNav.Controls.Add(lblNavApps);
            panelLeftNav.Controls.Add(btnNavApps);
            panelLeftNav.Location = new Point(12, 40);
            panelLeftNav.Name = "panelLeftNav";
            panelLeftNav.Size = new Size(66, 340);
            panelLeftNav.TabIndex = 7;
            // 
            // lblNavSettings
            // 
            lblNavSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblNavSettings.AutoSize = true;
            lblNavSettings.Font = new Font("Segoe UI", 7F);
            lblNavSettings.ForeColor = Color.FromArgb(226, 232, 240);
            lblNavSettings.Location = new Point(13, 261);
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
            btnNavSettings.ForeColor = Color.FromArgb(226, 232, 240);
            btnNavSettings.Location = new Point(6, 276);
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
            lblNavPcs.ForeColor = Color.FromArgb(226, 232, 240);
            lblNavPcs.Location = new Point(22, 162);
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
            btnNavPcs.ForeColor = Color.FromArgb(226, 232, 240);
            btnNavPcs.Location = new Point(6, 107);
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
            lblNavApps.ForeColor = Color.FromArgb(226, 232, 240);
            lblNavApps.Location = new Point(19, 76);
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
            btnNavApps.ForeColor = Color.FromArgb(226, 232, 240);
            btnNavApps.Location = new Point(6, 21);
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
            BackColor = Color.FromArgb(2, 6, 23);
            ClientSize = new Size(764, 461);
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
    }
}
