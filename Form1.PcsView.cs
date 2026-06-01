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
            // Scale card size based on font size. Match app cards!
            float fontScale = FontManager.BaseFontSize / 9.0f; // 9.0 is base size
            int scaledWidth = (int)(200 * Math.Max(1.0f, fontScale));
            int scaledHeight = (int)(48 * Math.Max(1.0f, fontScale));

            // Use the same base card colors as app cards so UI matches
            panel.BackColor = CardNormalBack;
            panel.ForeColor = Color.FromArgb(229, 231, 235);
            panel.Padding = new Padding(6, 3, 6, 3);
            panel.Margin = new Padding(6);
            panel.Width = scaledWidth;
            panel.Height = scaledHeight;
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

            // PC name larger, IP address smaller
            var nameFont = FontManager.GetFont(1.0f, FontStyle.Regular);  // Base size for PC name
            var ipFont = FontManager.GetFont(0.8f, FontStyle.Regular);   // 25% smaller for IP address

            // ---------- "All PCs" card ----------
            {
                var allPanel = new Panel();
                StylePcCardPanel(allPanel, isAll: true);

                var lblTitle = new Label
                {
                    AutoSize = false,
                    Text = "All PCs",
                    Font = nameFont,
                    Location = new Point(6, 4),
                    Size = new Size(allPanel.Width - 16, nameFont.Height + 2),
                    AutoEllipsis = true
                };

                var lblMeta = new Label
                {
                    AutoSize = true,
                    Text = "Shut down every configured PC.",
                    Font = ipFont,
                    ForeColor = Color.FromArgb(191, 219, 254),
                    MaximumSize = new Size(allPanel.Width - 16, 0),
                    AutoEllipsis = true
                };

                // Position meta label at bottom explicitly
                lblMeta.Location = new Point(8, allPanel.Height - lblMeta.PreferredHeight - 12);

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
                        if (!pc.Enabled) continue;
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
                            if (!pc.Enabled) continue;
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

                // PC Name - larger and prominent
                var lblName = new Label
                {
                    AutoSize = false,
                    Text = pc.Name,
                    Font = nameFont,
                    ForeColor = pc.Enabled ? Color.FromArgb(243, 244, 246) : Color.FromArgb(100, 116, 139),
                    Location = new Point(6, 4),
                    Size = new Size(pcPanel.Width - 30, nameFont.Height + 2),
                    AutoEllipsis = true
                };

                // IP Address - smaller and subdued (at bottom)
                var lblIp = new Label
                {
                    AutoSize = true,
                    Text = pc.IP,
                    Font = ipFont,
                    ForeColor = pc.Enabled ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105),
                    MaximumSize = new Size(pcPanel.Width - 30, 0),
                    AutoEllipsis = true
                };

                // Position IP label at bottom explicitly
                lblIp.Location = new Point(6, pcPanel.Height - lblIp.PreferredHeight - 12);

                pcPanel.Controls.Add(lblIp);
                pcPanel.Controls.Add(lblName);

                // Full-card hover (panel + labels), same as app cards
                if (pc.Enabled)
                {
                    AttachCardHover(pcPanel, lblName, lblIp);
                }
                else
                {
                    pcPanel.BackColor = Color.FromArgb(15, 23, 42); // Dimmed background
                    pcPanel.Cursor = Cursors.Default;
                }

                // Right-click context menu
                var ctxMenuPc = new ContextMenuStrip();
                ctxMenuPc.Items.Add("Restart").Click += async (ms, me) =>
                {
                    if (!pc.Enabled) return;
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
                    string oldIp = pc.IP;
                    using var dlg = new AddPcForm(pc);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _pcs[index] = dlg.PcData;
                        // Update all apps with matching ClientIP
                        if (!string.IsNullOrWhiteSpace(oldIp) && oldIp != dlg.PcData.IP)
                        {
                            foreach (var app in _apps)
                            {
                                if (string.Equals(app.ClientIP, oldIp, StringComparison.OrdinalIgnoreCase))
                                {
                                    app.ClientIP = dlg.PcData.IP;
                                }
                            }
                        }
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
                ctxMenuPc.Items.Add(new ToolStripSeparator());
                string enableDisableText = pc.Enabled ? "Disable" : "Enable";
                ctxMenuPc.Items.Add(enableDisableText).Click += (ms, me) =>
                {
                    pc.Enabled = !pc.Enabled;
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
                        if (!pc.Enabled) return;

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

        private void HandleRemoteRoutineAction(RemoteRoutineActionRequest request)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    AddToLog($"Executing routine action '{request.RoutineActionType}' for app '{request.AppName}'");

                    // Create a temporary ApplicationDetails from the request
                    var app = new ApplicationDetails
                    {
                        Name = request.AppName,
                        ProcessName = request.ProcessName,
                        RestartPath = request.RestartPath
                    };

                    // Create a routine executor with logging
                    var executor = new RoutineExecutor(_apps, AddToLog);

                    // Execute the specific action based on type
                    switch (request.RoutineActionType)
                    {
                        case "KeyboardShortcut":
                            await executor.ExecuteKeyboardShortcutAsync(app, request.Keys);
                            break;

                        case "ClickArea":
                            await executor.ExecuteClickAsync(app, request.ClickX, request.ClickY);
                            break;

                        case "Minimize":
                            await executor.ExecuteMinimizeAsync(app);
                            break;

                        default:
                            AddToLog($"Unknown routine action type: {request.RoutineActionType}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AddToLog($"Error executing remote routine action: {ex.Message}");
                }
            });
        }
    }
}
