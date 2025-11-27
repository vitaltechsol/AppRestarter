using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class Form1
    {
        private void ShowPcsView()
        {
            _currentView = ViewMode.Pcs;
            btnAddApp.Text = "Add New PC";
            HighlightNavButton(btnNavPcs);

            RenderPcButtons();
        }

        private void StylePcCardPanel(Panel panel, bool isAll = false)
        {
            // Use the same base card colors as app cards so UI matches
            panel.BackColor = CardNormalBack;
            panel.ForeColor = Color.FromArgb(229, 231, 235);
            panel.Padding = new Padding(6, 3, 6, 3);
            panel.Margin = new Padding(8);
            panel.Width = 206;
            panel.Height = 64;
            panel.Cursor = Cursors.Hand;
            panel.BorderStyle = BorderStyle.FixedSingle;
        }

        private void RenderPcButtons()
        {
            AppFlowLayoutPanel.SuspendLayout();
            AppFlowLayoutPanel.Controls.Clear();
            AppFlowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
            AppFlowLayoutPanel.WrapContents = true;
            AppFlowLayoutPanel.AutoScroll = true;

            if (_pcs.Count == 0)
            {
                var empty = new Label
                {
                    AutoSize = true,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Text = "No PCs configured yet."
                };
                AppFlowLayoutPanel.Controls.Add(empty);
                AppFlowLayoutPanel.ResumeLayout();
                return;
            }

            float baseSize = this.Font.Size;
            var nameFont = new Font(this.Font.FontFamily, Math.Max(6, baseSize + 2), FontStyle.Regular);
            var ipFont = new Font(this.Font.FontFamily, Math.Max(6, baseSize - 1), FontStyle.Regular);

            // ---------- "All PCs" card ----------
            {
                var allPanel = new Panel();
                StylePcCardPanel(allPanel, isAll: true);

                var lblTitle = new Label
                {
                    AutoSize = false,
                    Text = "All PCs",
                    Font = nameFont,
                    Location = new Point(6, 8),
                    Size = new Size(allPanel.Width - 12, 18)
                };

                var lblMeta = new Label
                {
                    AutoSize = false,
                    Text = "Shut down every configured PC.",
                    Font = ipFont,
                    ForeColor = Color.FromArgb(191, 219, 254),
                    Location = new Point(6, 32),
                    Size = new Size(allPanel.Width - 12, 18)
                };

                allPanel.Controls.Add(lblMeta);
                allPanel.Controls.Add(lblTitle);

                // Full-card hover (panel + labels)
                AttachCardHover(allPanel, lblTitle, lblMeta);

                // Context menu for All PCs
                var ctxMenu = new ContextMenuStrip();
                ctxMenu.Items.Add("Restart All").Click += async (ms, me) =>
                {
                    var confirm = MessageBox.Show(
                        "Are you sure you want to RESTART all configured PCs?",
                        "Confirm Restart All",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes) return;

                    foreach (var pc in _pcs.ToList())
                    {
                        await PcPowerController.RestartAsync(pc, _settings.AppPort, AddToLog);
                    }
                };

                allPanel.ContextMenuStrip = ctxMenu;
                lblTitle.ContextMenuStrip = ctxMenu;
                lblMeta.ContextMenuStrip = ctxMenu;

                // Left-click: shutdown all
                void AttachAllClick(Control c)
                {
                    c.Click += async (s, e) =>
                    {
                        var confirm = MessageBox.Show(
                            "Are you sure you want to SHUT DOWN all configured PCs?",
                            "Confirm Shutdown All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (confirm != DialogResult.Yes) return;

                        foreach (var pc in _pcs.ToList())
                        {
                            await PcPowerController.ShutdownAsync(pc, _settings.AppPort, AddToLog);
                        }
                    };
                }

                AttachAllClick(allPanel);
                AttachAllClick(lblTitle);
                AttachAllClick(lblMeta);

                AppFlowLayoutPanel.Controls.Add(allPanel);
            }

            // ---------- Individual PC cards ----------
            for (int i = 0; i < _pcs.Count; i++)
            {
                var pc = _pcs[i];
                int index = i;

                var pcPanel = new Panel();
                StylePcCardPanel(pcPanel, isAll: false);

                var lblName = new Label
                {
                    AutoSize = false,
                    Text = pc.Name,
                    Font = nameFont,
                    Location = new Point(6, 8),
                    Size = new Size(pcPanel.Width - 12, 18)
                };

                var lblIp = new Label
                {
                    AutoSize = false,
                    Text = pc.IP,
                    Font = ipFont,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(6, 32),
                    Size = new Size(pcPanel.Width - 12, 18)
                };

                pcPanel.Controls.Add(lblIp);
                pcPanel.Controls.Add(lblName);

                // Full-card hover (panel + labels), same as app cards
                AttachCardHover(pcPanel, lblName, lblIp);

                // Right-click context menu
                var ctxMenuPc = new ContextMenuStrip();
                ctxMenuPc.Items.Add("Restart").Click += async (ms, me) =>
                {
                    var confirm = MessageBox.Show(
                        $"Restart {pc.Name} ({pc.IP})?",
                        "Confirm Restart",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;

                    await PcPowerController.RestartAsync(pc, _settings.AppPort, AddToLog);
                };

                ctxMenuPc.Items.Add(new ToolStripSeparator());
                ctxMenuPc.Items.Add("Edit").Click += (ms, me) =>
                {
                    using var dlg = new AddPcForm(pc);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _pcs[index] = dlg.PcData;
                        SaveApplicationsToXml();
                        RenderPcButtons();
                    }
                };

                ctxMenuPc.Items.Add("Delete").Click += (ms, me) =>
                {
                    var confirm = MessageBox.Show(
                        $"Delete PC '{pc.Name}' from configuration?",
                        "Confirm Delete PC",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;

                    _pcs.RemoveAt(index);
                    SaveApplicationsToXml();
                    RenderPcButtons();
                };

                pcPanel.ContextMenuStrip = ctxMenuPc;
                lblName.ContextMenuStrip = ctxMenuPc;
                lblIp.ContextMenuStrip = ctxMenuPc;

                // Left-click => shutdown this PC
                void AttachPcClick(Control c)
                {
                    c.Click += async (s, e) =>
                    {
                        var confirm = MessageBox.Show(
                            $"Shut down {pc.Name} ({pc.IP})?",
                            "Confirm Shutdown",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        if (confirm != DialogResult.Yes) return;

                        await PcPowerController.ShutdownAsync(pc, _settings.AppPort, AddToLog);
                    };
                }

                AttachPcClick(pcPanel);
                AttachPcClick(lblName);
                AttachPcClick(lblIp);

                AppFlowLayoutPanel.Controls.Add(pcPanel);
            }

            AppFlowLayoutPanel.ResumeLayout();
        }

        private void HandleRemotePcRestart(ApplicationDetails details)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var targetIp = details.ClientIP;
                    AddToLog($"Remote PC RESTART requested for IP {targetIp ?? "(null)"}.");

                    // 1) Select only apps that belong to this PC AND are not AppRestarter itself
                    var targetApps = _apps
                        .Where(app =>
                            !IsAppRestarterSelf(app) &&
                            (
                                string.IsNullOrWhiteSpace(app.ClientIP) ||  // local apps on this machine
                                string.Equals(app.ClientIP, targetIp, StringComparison.OrdinalIgnoreCase)
                            ))
                        .ToList();

                    AddToLog($"Stopping {targetApps.Count} app(s) for this PC before restart.");

                    foreach (var app in targetApps)
                    {
                        await HandleAppButtonClickAsync(
                            app,
                            start: false,
                            stop: true,
                            skipConfirm: true);
                    }

                    // 2) Small pause so processes can exit
                    await Task.Delay(2000);

                    // 3) Graceful restart (no /f)
                    AddToLog("Issuing OS restart (shutdown /r /t 0).");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/r /t 0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                catch (Exception ex)
                {
                    AddToLog("Error during remote PC restart: " + ex.Message);
                }
            });
        }

        private void HandleRemotePcShutdown(ApplicationDetails details)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var targetIp = details.ClientIP;
                    AddToLog($"Remote PC SHUTDOWN requested for IP {targetIp ?? "(null)"}.");

                    // 1) Select only apps that belong to this PC AND are not AppRestarter itself
                    var targetApps = _apps
                        .Where(app =>
                            !IsAppRestarterSelf(app) &&
                            (
                                string.IsNullOrWhiteSpace(app.ClientIP) ||
                                string.Equals(app.ClientIP, targetIp, StringComparison.OrdinalIgnoreCase)
                            ))
                        .ToList();

                    AddToLog($"Stopping {targetApps.Count} app(s) for this PC before shutdown.");

                    foreach (var app in targetApps)
                    {
                        await HandleAppButtonClickAsync(
                            app,
                            start: false,
                            stop: true,
                            skipConfirm: true);
                    }

                    // 2) Small pause so processes can exit
                    await Task.Delay(2000);

                    // 3) Graceful shutdown (no /f)
                    AddToLog("Issuing OS shutdown (shutdown /s /t 0).");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/s /t 0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                catch (Exception ex)
                {
                    AddToLog("Error during remote PC shutdown: " + ex.Message);
                }
            });
        }
    }
}
