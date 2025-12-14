using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static AppRestarter.AppStatusManager;

namespace AppRestarter
{
    public partial class Form1
    {

        private static string getSettingsXMLConfigPath()
        {
            string exePath = Application.ExecutablePath;
            string directory = System.IO.Path.GetDirectoryName(exePath);

            return System.IO.Path.Combine(directory, "Settings.xml");
        }

        private void LoadApplicationsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                string settingsPath = getSettingsXMLConfigPath();

                if (!System.IO.File.Exists(configPath))
                {
                    AddToLog("Applications.xml not found. Will create new XML");
                    return;
                }

                var doc = XDocument.Load(configPath);
                var root = doc.Root;
                if (root == null)
                {
                    AddToLog("Applications.xml is empty or invalid.");
                    return;
                }

                _apps.Clear();

                var applicationsElement = root.Element("Applications");
                if (applicationsElement != null)
                {
                    foreach (XElement applicationElement in applicationsElement.Elements("Application"))
                    {
                        ApplicationDetails app = new ApplicationDetails
                        {
                            Name = applicationElement.Element("Name")?.Value ?? "",
                            ProcessName = applicationElement.Element("ProcessName")?.Value ?? "",
                            RestartPath = applicationElement.Element("RestartPath")?.Value,
                            ClientIP = applicationElement.Element("ClientIP")?.Value,
                            AutoStart = bool.TryParse(applicationElement.Element("AutoStart")?.Value, out var autoStart) && autoStart,
                            AutoStartDelayInSeconds = int.TryParse(applicationElement.Element("AutoStartDelayInSeconds")?.Value, out var delay) ? delay : 0,
                            NoWarn = bool.TryParse(applicationElement.Element("NoWarn")?.Value, out var noWarn) && noWarn,
                            StartMinimized = bool.TryParse(applicationElement.Element("StartMinimized")?.Value, out var startMinimized) && startMinimized,
                            GroupName = applicationElement.Element("GroupName")?.Value ?? ""
                        };

                        _apps.Add(app);
                    }
                }

                _groups = LoadGroups(root);

                if (System.IO.File.Exists(settingsPath))
                {
                    var settingsDoc = XDocument.Load(settingsPath);
                    var settingsRoot = settingsDoc.Root;
                    if (settingsRoot != null)
                    {
                        _settings.AppPort = int.TryParse(settingsRoot.Element("AppPort")?.Value, out var appPort) ? appPort : _settings.AppPort;
                        _timeout = int.TryParse(settingsRoot.Element("TimeoutMs")?.Value, out var timeoutMs) ? timeoutMs : _timeout;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading applications from XML: " + ex.Message);
            }
        }

        private static List<string> LoadGroups(XElement root)
        {
            var groups = new List<string>();
            var groupsEl = root.Element("Groups");
            if (groupsEl != null)
            {
                foreach (var g in groupsEl.Elements("Group"))
                {
                    var nameAttr = g.Attribute("Name");
                    if (nameAttr != null && !string.IsNullOrWhiteSpace(nameAttr.Value))
                    {
                        groups.Add(nameAttr.Value.Trim());
                    }
                }
            }
            return groups
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
               .ToList();
        }

        // ---------- CARD STYLE HELPERS (APPS) ----------

        private static readonly Color CardNormalBack = Color.FromArgb(31, 41, 55); // #1f2937
        private static readonly Color CardHoverBack = Color.FromArgb(51, 65, 85); // #334155

        private void StyleGroupPanel(Panel panel)
        {
            panel.BackColor = Color.FromArgb(15, 23, 42);
            panel.ForeColor = Color.FromArgb(229, 231, 235);
            panel.Padding = new Padding(5, 8, 5, 12);
            panel.Margin = new Padding(0, 8, 0, 4);      // no horizontal margin -> no horiz scroll
            panel.BorderStyle = BorderStyle.FixedSingle;

            int fullWidth = Math.Max(100, AppFlowLayoutPanel.ClientSize.Width - 4);
            panel.AutoSize = false;
            panel.AutoSizeMode = AutoSizeMode.GrowOnly;
            panel.MinimumSize = new Size(fullWidth, 0);
            panel.MaximumSize = new Size(fullWidth, int.MaxValue);
            panel.Width = fullWidth;
        }

        private void StyleAppCardPanel(Panel panel)
        {
            // smaller cards to fit more
            panel.BackColor = CardNormalBack;
            panel.ForeColor = Color.FromArgb(229, 231, 235);
            panel.Padding = new Padding(6, 3, 6, 3);
            panel.Margin = new Padding(6);
            panel.Width = 200;  // narrower
            panel.Height = 42;  // slightly shorter
            panel.Cursor = Cursors.Hand;
            panel.BorderStyle = BorderStyle.FixedSingle;
        }

        private void AttachCardHover(Panel card, params Control[] children)
        {
            void HandleEnter(object _, EventArgs __)
            {
                card.BackColor = CardHoverBack;
            }

            void HandleLeave(object _, EventArgs __)
            {
                var pos = card.PointToClient(Cursor.Position);
                if (!card.ClientRectangle.Contains(pos))
                {
                    card.BackColor = CardNormalBack;
                }
            }

            var all = new List<Control> { card };
            all.AddRange(children);
            foreach (var c in all)
            {
                c.MouseEnter += HandleEnter;
                c.MouseLeave += HandleLeave;
            }
        }

        private string GetClientLabel(string clientIP)
        {
            if (string.IsNullOrWhiteSpace(clientIP))
                return "Local";

            var match = _pcs.FirstOrDefault(p => p.IP == clientIP);
            return match != null ? match.Name : clientIP;
        }

        /// <summary>
        /// Called from Form1.AppFlowLayoutPanel_Resize.
        /// We simply rebuild the view to keep widths fluid and avoid horizontal scroll.
        /// </summary>
        private void AdjustGroupPanelWidths()
        {
            if (_currentView == ViewMode.Apps)
            {
                UpdateAppList();
            }
        }

        // ---------- VIEW: APPS ----------

        private void ShowAppsView()
        {
            _currentView = ViewMode.Apps;
            btnAddApp.Text = "Add New App";
            HighlightNavButton(btnNavApps);

            UpdateAppList();

            // Status polling + UI updates are managed by AppStatusManager
            _statusManager.EnsureTimerStarted();
            _statusManager.Refresh();
        }

        private void UpdateAppList()
        {
            if (_currentView != ViewMode.Apps)
                return;

            AppFlowLayoutPanel.SuspendLayout();
            AppFlowLayoutPanel.Controls.Clear();
            AppFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            AppFlowLayoutPanel.WrapContents = false;
            AppFlowLayoutPanel.AutoScroll = true;

            if (_apps.Count == 0)
            {
                var empty = new Label
                {
                    AutoSize = true,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Text = "No applications configured yet."
                };
                AppFlowLayoutPanel.Controls.Add(empty);
            }
            else
            {
                RenderGroupsAndApps();
            }

            AppFlowLayoutPanel.ResumeLayout();

            // Immediately kick off a status refresh whenever the list is rebuilt
            _statusManager.Refresh();
        }




        private void RenderGroupsAndApps()
        {
            _statusManager.ClearIndicators();

            var ungroupedApps = _apps
                .Where(a => string.IsNullOrWhiteSpace(a.GroupName))
                .ToList();

            var namedGroups = _groups
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var orderedGroupNames = new List<string>();
            if (ungroupedApps.Any())
                orderedGroupNames.Add("(Ungrouped)");
            orderedGroupNames.AddRange(namedGroups);

            foreach (var groupName in orderedGroupNames)
            {
                List<ApplicationDetails> appsInGroup;
                string headerTitle;

                if (groupName == "(Ungrouped)")
                {
                    appsInGroup = ungroupedApps;
                    headerTitle = "Ungrouped";
                    if (appsInGroup.Count == 0)
                        continue;
                }
                else
                {
                    appsInGroup = _apps
                        .Where(a => string.Equals(a.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    headerTitle = groupName;
                    if (!appsInGroup.Any())
                        continue;

                    headerTitle = groupName;
                }

                var groupPanel = new Panel();
                StyleGroupPanel(groupPanel);
                int padLeft = groupPanel.Padding.Left;
                int padRight = groupPanel.Padding.Right;
                int padTop = groupPanel.Padding.Top;

                int innerWidth = groupPanel.ClientSize.Width - padLeft - padRight;
                if (innerWidth < 220) innerWidth = 220;

                // ---------- Header row (TOP) ----------
                var headerPanel = new Panel
                {
                    BackColor = Color.Transparent,
                    Location = new Point(padLeft, padTop),
                    Size = new Size(innerWidth, 30),
                };
                headerPanel.Location = new Point(-5, 0);
                var boldFont = new Font(this.Font, FontStyle.Bold);
                var regularFont = new Font(this.Font, FontStyle.Regular);

                var btnRestartGroup = new Button
                {
                    Text = $"Restart {headerTitle} Apps",
                    AutoSize = true,
                    Font = regularFont,
                    BackColor = Color.FromArgb(15, 89, 117), // Restart button Color
                    ForeColor = Color.FromArgb(250, 250, 250),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(98, 23)
                };
                btnRestartGroup.FlatAppearance.BorderSize = 0;
                headerPanel.Controls.Add(btnRestartGroup);

                btnRestartGroup.Click += async (s, e) =>
                {
                    var confirmMsg = groupName == "(Ungrouped)"
                        ? $"Restart all {appsInGroup.Count} ungrouped app(s)?"
                        : $"Restart all {appsInGroup.Count} app(s) in group \"{groupName}\"?";
                    var dr = MessageBox.Show(confirmMsg, "Restart group",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr != DialogResult.Yes) return;

                    foreach (var app in appsInGroup)
                    {
                        if (!string.IsNullOrEmpty(app.ClientIP))
                            HandleRemoteClientAppClick(app, start: true, stop: true, skipConfirm: true);
                        else
                            await HandleAppButtonClickAsync(app, start: true, stop: true, skipConfirm: true);
                    }
                };
                btnRestartGroup.MouseUp += (s, e) =>
                {
                    if (e.Button != MouseButtons.Right) return;
                    var menu = new ContextMenuStrip();
                    menu.Items.Add("Stop group").Click += async (ms, me) =>
                    {
                        var confirmMsg = groupName == "(Ungrouped)"
                            ? $"Stop all {appsInGroup.Count} ungrouped app(s)?"
                            : $"Stop all {appsInGroup.Count} app(s) in group \"{groupName}\"?";
                        var dr = MessageBox.Show(confirmMsg, "Stop group",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes) return;

                        foreach (var app in appsInGroup)
                        {
                            if (!string.IsNullOrEmpty(app.ClientIP))
                                HandleRemoteClientAppClick(app, start: false, stop: true, skipConfirm: true);
                            else
                                await HandleAppButtonClickAsync(app, start: false, stop: true, skipConfirm: true);
                        }
                    };
                    menu.Show(Cursor.Position);
                };
                groupPanel.Controls.Add(headerPanel);
                // ---------- Inner flow for cards (MULTI-ROW) ----------

                var innerFlow = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    AutoScroll = false,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    Location = new Point(padLeft, headerPanel.Bottom + 6),
                    Size = new Size(innerWidth, 10) // height updated after layout
                };

                // ---------- Apps (cards) row(s) ----------
                innerFlow.Size = new Size(innerWidth, 0);
                innerFlow.Location = new Point(padLeft, headerPanel.Bottom + 4);

                foreach (var app in appsInGroup.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
                {
                    int index = _apps.IndexOf(app);
                    if (index < 0) continue;

                    if (groupName == "(Ungrouped)")
                    {
                        if (!string.IsNullOrWhiteSpace(app.GroupName))
                            continue;
                    }
                    else
                    {
                        if (!string.Equals(app.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    var appCard = new Panel();
                    StyleAppCardPanel(appCard);

                    var clientLabel = GetClientLabel(app.ClientIP);
                    var metaText = !string.IsNullOrWhiteSpace(app.ProcessName)
                        ? $"{app.ProcessName} · {clientLabel}"
                        : clientLabel;

                    float baseSize = this.Font.SizeInPoints;
                    var nameFont = new Font(this.Font.FontFamily, Math.Max(6, baseSize + 2), FontStyle.Regular);
                    var metaFont = new Font(this.Font.FontFamily, Math.Max(6, baseSize - 3), FontStyle.Regular);

                    // App Name Style
                    var lblName = new Label
                    {
                        AutoSize = true,
                        Text = string.IsNullOrWhiteSpace(app.Name) ? "(no name)" : app.Name,
                        Font = nameFont,
                        ForeColor = Color.FromArgb(243, 244, 246),
                        Location = new Point(4, 3),
                        Size = new Size(appCard.Width - 12, 18),
                        AutoEllipsis = true
                    };

                    // PC Name Style
                    var lblMeta2 = new Label
                    {
                        AutoSize = false,
                        Text = metaText,
                        Font = metaFont,
                        ForeColor = Color.FromArgb(148, 163, 184),
                        Location = new Point(6, 22),
                        Size = new Size(appCard.Width - 12, 18)
                    };

                    // Status indicator (small colored dot)
                    var statusLabel = new Label
                    {
                        AutoSize = false,
                        Text = "●",
                        Font = new Font(this.Font.FontFamily, Math.Max(6, baseSize + 2), FontStyle.Bold),
                        ForeColor = AppStatusManager.StatusGray,
                        Size = new Size(18, 18),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(appCard.Width - 20, (appCard.Height / 2) - 2)
                    };

                    // If we already know the last state of this app, keep that color when rebuilding the UI
                    if (_statusManager.TryGetLastState(app, out var lastStateForApp))
                    {
                        Color initialColor = AppStatusManager.StatusGray;
                        switch (lastStateForApp)
                        {
                            case AppRunVisualState.Running:
                                initialColor = AppStatusManager.StatusGreen;
                                break;
                            case AppRunVisualState.UnexpectedlyStopped:
                                initialColor = AppStatusManager.StatusRed;
                                break;
                            case AppRunVisualState.AutoStartPending:
                                initialColor = AppStatusManager.StatusOrange;
                                break;
                            case AppRunVisualState.Stopped:
                                initialColor = AppStatusManager.StatusGray;
                                break;
                        }
                        statusLabel.ForeColor = initialColor;
                    }

                    appCard.Controls.Add(statusLabel);
                    appCard.Controls.Add(lblMeta2);
                    appCard.Controls.Add(lblName);

                    _statusManager.RegisterIndicator(app, statusLabel);

                    // Context menu for stop/edit
                    var ctxMenu = new ContextMenuStrip();
                    ctxMenu.Items.Add("Stop").Click += (ms, me) => StopApp(index);
                    ctxMenu.Items.Add(new ToolStripSeparator());
                    ctxMenu.Items.Add("Edit").Click += (ms, me) => EditApp(index);

                    appCard.ContextMenuStrip = ctxMenu;
                    lblName.ContextMenuStrip = ctxMenu;
                    lblMeta2.ContextMenuStrip = ctxMenu;
                    statusLabel.ContextMenuStrip = ctxMenu;

                    // Hover on full card (panel + labels + status)
                    AttachCardHover(appCard, lblName, lblMeta2, statusLabel);

                    // Left-click anywhere on the card => restart
                    void AttachClick(Control c)
                    {
                        c.Click += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(app.ClientIP))
                                HandleRemoteClientAppClick(app, start: true, stop: true, skipConfirm: false);
                            else
                                _ = HandleAppButtonClickAsync(app, start: true, stop: true, skipConfirm: false);
                        };
                    }

                    AttachClick(appCard);
                    AttachClick(lblName);
                    AttachClick(lblMeta2);
                    AttachClick(statusLabel);

                    innerFlow.Controls.Add(appCard);
                }

                innerFlow.PerformLayout();

                int maxBottom = 0;
                foreach (Control child in innerFlow.Controls)
                {
                    if (child.Bottom > maxBottom)
                        maxBottom = child.Bottom;
                }

                innerFlow.Height = maxBottom + 4;

                groupPanel.Controls.Add(innerFlow);

                int totalHeight =
                    groupPanel.Padding.Top +
                    headerPanel.Height +
                    6 +
                    innerFlow.Height +
                    groupPanel.Padding.Bottom;

                groupPanel.Height = totalHeight;

                AppFlowLayoutPanel.Controls.Add(groupPanel);
            }

            _statusManager.Refresh();
        }

        // ---------- APPS: EDIT / STOP / AUTO-START / GROUPS ----------

        private void EditApp(int index)
        {
            var existing = _apps[index];
            using var editForm = new AddAppForm(
                existing,
                index,
                getGroups: () => new List<string>(_groups),
                manageGroups: ManageGroups,
                pcs: new List<PcInfo>(_pcs)
            );
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                if (editForm.DeleteRequested)
                    _apps.RemoveAt(index);
                else
                    _apps[index] = editForm.AppData;

                SaveApplicationsToXml();
                UpdateAppList();
            }
        }

        private void StopApp(int index)
        {
            var existing = _apps[index];
            if (!string.IsNullOrEmpty(existing.ClientIP))
                HandleRemoteClientAppClick(existing, start: false, stop: true, skipConfirm: false);
            else
                _ = HandleAppButtonClickAsync(existing, start: false, stop: true, skipConfirm: false);
        }

        private void AutoStartApps()
        {
            foreach (var app in _apps.Where(a => a.AutoStart))
            {
                AddToLog($"Auto starting {app.Name} in {app.AutoStartDelayInSeconds} seconds");

                Task.Run(async () =>
                {
                    if (app.AutoStartDelayInSeconds > 0)
                        await Task.Delay(app.AutoStartDelayInSeconds * 1000);

                    if (!string.IsNullOrEmpty(app.ClientIP))
                    {
                        Debug.WriteLine($"AutoStart ClientIP {app.Name}");
                        HandleRemoteClientAppClick(app, start: true, stop: false, skipConfirm: true);
                    }
                    else
                        await HandleAppButtonClickAsync(app, start: true, stop: false, skipConfirm: true);
                });
            }
        }

        private void ManageGroups()
        {
            using var dlg = new GroupsForm(_groups);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var newGroups = dlg.Groups
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var removed = _groups.Except(newGroups, StringComparer.OrdinalIgnoreCase).ToList();
            var added = newGroups.Except(_groups, StringComparer.OrdinalIgnoreCase).ToList();

            if (removed.Count == 1 && added.Count == 1)
            {
                string oldName = removed[0];
                string newName = added[0];
                foreach (var app in _apps)
                {
                    if (!string.IsNullOrEmpty(app.GroupName) &&
                        app.GroupName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                    {
                        app.GroupName = newName;
                    }
                }
            }
            else
            {
                var valid = new HashSet<string>(newGroups, StringComparer.OrdinalIgnoreCase);
                foreach (var app in _apps)
                {
                    if (!string.IsNullOrEmpty(app.GroupName) && !valid.Contains(app.GroupName))
                        app.GroupName = null;
                }
            }

            _groups = newGroups;

            SaveApplicationsToXml();
            if (_currentView == ViewMode.Apps)
                UpdateAppList();
        }

        // ---------- APP (LOCAL) START/STOP LOGIC ----------

        private async Task HandleAppButtonClickAsync(ApplicationDetails app, bool start, bool stop, bool skipConfirm)
        {
            DialogResult? confirmResult = null;
            string actionName = "restart";
            if (stop && !start)
            {
                actionName = "stop";
                _statusManager.MarkStopInitiatedByUs(app);
            }
                
            else if (!stop && start)
            {
                actionName = "start";
                _statusManager.MarkStartInitiatedByUs(app);
            }
            
            if (!skipConfirm && !app.NoWarn)
            {
                if (InvokeRequired)
                {
                    confirmResult = (DialogResult)await Task.Run(() =>
                        this.Invoke(new Func<DialogResult>(() =>
                            MessageBox.Show(
                                $"Are you sure you want to {actionName} {app.Name}\n from: {app.RestartPath}",
                                "Restart App",
                                MessageBoxButtons.YesNo))));
                }
                else
                {
                    confirmResult = MessageBox.Show(
                        $"Are you sure you want to {actionName} {app.Name}\n from: {app.RestartPath}",
                        "Restart App",
                        MessageBoxButtons.YesNo);
                }
            }

            bool proceed = skipConfirm || app.NoWarn || confirmResult == DialogResult.Yes;
            if (!proceed)
            {
                return;
            }

            int stopped = 0;
            if (stop)
            {
                stopped = await ProcessTerminator.StopAsync(app, AddToLog, timeoutMs: _timeout);
                _statusManager.MarkStopInitiatedByUs(app);
            }

            if (start && stopped > 0)
            {
                await Task.Delay(2000);
            }

            try
            {
                var isAppRunning = ProcessTerminator.IsRunning(app);

                if (stop && !start)
                {
                    AddToLog($"Stop requested for {app.Name}; killed {stopped} instances.");
                    return;
                }

                if (!start && !stop)
                {
                    AddToLog($"No start/stop action for {app.Name} requested.");
                    return;
                }

                if (!start && isAppRunning)
                {
                    AddToLog($"{app.Name} is already running.");
                    return;
                }

                if (start && !string.IsNullOrWhiteSpace(app.RestartPath) && !isAppRunning)
                {
                    AddToLog($"Starting {app.Name}");
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = app.RestartPath,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(app.RestartPath),
                        UseShellExecute = false,
                    };

                    if (app.StartMinimized)
                        startInfo.WindowStyle = ProcessWindowStyle.Minimized;

                    var process = Process.Start(startInfo);

                    if (app.StartMinimized)
                    {
                        _ = Task.Run(() =>
                        {
                            if (process != null)
                            {
                                process.WaitForInputIdle();
                                System.Threading.Thread.Sleep(2000);
                                if (process.MainWindowHandle != IntPtr.Zero &&
                                    WinApiHelper.IsWindowVisible(process.MainWindowHandle))
                                {
                                    WinApiHelper.ShowWindow(process.MainWindowHandle, WinApiHelper.SW_MINIMIZE);
                                }
                                AddToLog($"Started {app.Name} minimized.");
                            }
                        });
                    }
                    else
                    {
                        AddToLog($"Started {app.Name}");
                    }

                    Debug.WriteLine($"Started {app.Name}");
                    _statusManager.MarkStartInitiatedByUs(app);

                }
            }
            catch (Exception ex)
            {
                AddToLog($"Error starting {app.ProcessName} {ex.Message}");
                Debug.WriteLine($"Error starting {app.ProcessName} {ex.Message}");

                if (InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                        MessageBox.Show($"Error starting {app.ProcessName} {ex.Message}")));
                }
                else
                {
                    MessageBox.Show($"Error starting {app.ProcessName} {ex.Message}");
                }
            }

            _statusManager.Refresh();
        }

        private void HandleRemoteClientAppClick(ApplicationDetails applicationDetails, bool start, bool stop, bool skipConfirm)
        {
            try
            {
                DialogResult? confirmResult = null;
                string actionName = "restart";
                if (stop && !start)
                    actionName = "stop";
                else if (!stop && start)
                    actionName = "start";

                if (!skipConfirm && !applicationDetails.NoWarn)
                {
                    confirmResult = MessageBox.Show(
                        $"Are you sure you want to {actionName} {applicationDetails.Name}\nfrom: {applicationDetails.RestartPath}\nfrom IP: {applicationDetails.ClientIP}",
                        "Restart Remote App",
                        MessageBoxButtons.YesNo);
                }

                bool proceed = skipConfirm || applicationDetails.NoWarn || confirmResult == DialogResult.Yes;
                if (!proceed)
                {
                    return;
                }

                AddToLog($"Sending Remote App Request {applicationDetails.Name} on {applicationDetails.ClientIP} to {actionName}");

                if (start && !stop)
                    _statusManager.MarkStartInitiatedByUs(applicationDetails);
                else if (stop && !start)
                    _statusManager.MarkStopInitiatedByUs(applicationDetails);

                using var client = new TcpClient(applicationDetails.ClientIP, _settings.AppPort);
                client.SendTimeout = 6000;
                using var stream = client.GetStream();

                applicationDetails.StopRequested = stop;
                applicationDetails.StartRequested = start;
                var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                serializer.WriteObject(stream, applicationDetails);
                stream.Flush();

                _statusManager.Refresh();
            }
            catch (Exception ex)
            {
                var msg = $"Error starting {applicationDetails.ProcessName} {ex.Message} \nIP: {applicationDetails.ClientIP}";
                MessageBox.Show(msg);
                AddToLog(msg);
                Debug.WriteLine(msg);
            }
        }
    }
}
