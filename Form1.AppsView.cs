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

namespace AppRestarter
{
    public partial class Form1
    {
        private enum AppRunVisualState
        {
            Stopped,
            Running,
            UnexpectedlyStopped,
            AutoStartPending
        }

        private readonly Dictionary<ApplicationDetails, bool> _appStartedByUs = new Dictionary<ApplicationDetails, bool>();
        private readonly Dictionary<ApplicationDetails, Control> _appStatusIndicators = new Dictionary<ApplicationDetails, Control>();
        private readonly Dictionary<ApplicationDetails, AppRunVisualState> _lastAppRunStates = new Dictionary<ApplicationDetails, AppRunVisualState>();
        private readonly HashSet<ApplicationDetails> _appHasRunAtLeastOnce = new HashSet<ApplicationDetails>();

        private System.Windows.Forms.Timer _appStatusTimer;
        private bool _statusRefreshInProgress = false;
        private static readonly Color StatusGray = Color.FromArgb(55, 65, 81);
        private static readonly Color StatusGreen = Color.FromArgb(34, 197, 94);
        private static readonly Color StatusRed = Color.FromArgb(228, 8, 10);
        private static readonly Color StatusOrange = Color.FromArgb(255, 203, 91);

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

            if (_appStatusTimer == null)
            {
                _appStatusTimer = new System.Windows.Forms.Timer();
                _appStatusTimer.Interval = 3_000; // 3 seconds
                _appStatusTimer.Tick += AppStatusTimer_Tick;
                _appStatusTimer.Start();
            }

            RefreshAppStatuses();
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
            RefreshAppStatuses();
        }

        private void AppStatusTimer_Tick(object sender, EventArgs e)
        {
            RefreshAppStatuses();
        }

        // ---------- STATUS & BATCH HELPERS ----------

        /// <summary>
        /// Shared logic to convert a simple "isRunning" flag into the visual state
        /// (green/orange/red/gray) while tracking first-run and started-by-us flags.
        /// </summary>
        private AppRunVisualState ComputeVisualState(ApplicationDetails app, bool isRunning)
        {
            bool startedByUs = _appStartedByUs.TryGetValue(app, out var val) && val;

            // Track if this app has ever been observed running during this session
            if (isRunning)
            {
                _appHasRunAtLeastOnce.Add(app);
            }
            bool hasEverRun = _appHasRunAtLeastOnce.Contains(app);

            // Orange only before the app has ever started (initial delayed auto-start window)
            if (!isRunning &&
                !hasEverRun &&
                app.AutoStart &&
                app.AutoStartDelayInSeconds > 0)
            {
                return AppRunVisualState.AutoStartPending;
            }

            if (isRunning)
                return AppRunVisualState.Running;

            return startedByUs ? AppRunVisualState.UnexpectedlyStopped : AppRunVisualState.Stopped;
        }
            

        /// <summary>
        /// Main status refresh: locals are checked individually, remotes are checked in batches
        /// per remote PC (one TCP connection per IP per cycle).
        /// </summary>
        private void RefreshAppStatuses()
        {
            if (_currentView != ViewMode.Apps)
                return;

            if (_statusRefreshInProgress)
                return;

            _statusRefreshInProgress = true;

            var appsSnapshot = _apps.ToList();

            Task.Run(() =>
            {
                try
                {
                    var results = new List<(ApplicationDetails app, AppRunVisualState state)>();

                    // Local first (cheap)
                    foreach (var app in appsSnapshot.Where(a => string.IsNullOrWhiteSpace(a.ClientIP)))
                    {
                        bool isRunning = ProcessTerminator.IsRunning(app);
                        results.Add((app, ComputeVisualState(app, isRunning)));
                    }

                    // Remote: one batch per unique IP
                    var groups = appsSnapshot
                        .Where(a => !string.IsNullOrWhiteSpace(a.ClientIP))
                        .GroupBy(a => a.ClientIP);

                    foreach (var g in groups)
                    {
                        results.AddRange(GetRemoteBatchStatesForGroup(g.Key, g.ToList()));
                    }

                    if (!IsDisposed && IsHandleCreated)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            foreach (var r in results)
                                UpdateAppStatusIndicator(r.app, r.state);
                        }));
                    }
                }
                finally
                {
                    _statusRefreshInProgress = false;
                }
            });
        }

        /// <summary>
        /// Batch status request per remote IP: sends one request containing all apps for that
        /// PC and parses a single STATUSBATCH reply.
        /// </summary>

        private List<(ApplicationDetails app, AppRunVisualState state)> GetRemoteBatchStatesForGroup(
            string clientIp,
            List<ApplicationDetails> appsOnThisPc)
        {
            var results = new List<(ApplicationDetails app, AppRunVisualState state)>();

            if (string.IsNullOrWhiteSpace(clientIp) || appsOnThisPc == null || appsOnThisPc.Count == 0)
                return results;

            try
            {
                AddToLog($"Batch status -> {clientIp}: {appsOnThisPc.Count} app(s)");

                // Build request matching server-side AppStatusBatchRequest contract
                var batchRequest = new AppStatusBatchRequest
                {
                    ActionType = RemoteActionType.AppStatusBatch,
                    Apps = appsOnThisPc.Select(a => new ApplicationDetails
                    {
                        Name = a.Name,
                        ProcessName = a.ProcessName,
                        RestartPath = a.RestartPath,
                        ClientIP = null, // remote treats as local
                        AutoStart = a.AutoStart,
                        AutoStartDelayInSeconds = a.AutoStartDelayInSeconds,
                        NoWarn = a.NoWarn,
                        StartMinimized = a.StartMinimized,
                        GroupName = a.GroupName
                    }).ToList()
                };

                using var client = new TcpClient(AddressFamily.InterNetwork);
                client.SendTimeout = 3000;
                client.ReceiveTimeout = 3000;

                client.Connect(clientIp, _settings.AppPort);

                using var stream = client.GetStream();
                var serializer = new DataContractSerializer(typeof(AppStatusBatchRequest));
                serializer.WriteObject(stream, batchRequest);
                stream.Flush();

                // Tell server we’re done sending
                try { client.Client.Shutdown(SocketShutdown.Send); } catch { }

                // Read response
                using var ms = new System.IO.MemoryStream();
                byte[] buffer = new byte[2048];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                    if (ms.Length > 64 * 1024) break; // safety cap
                }

                string reply = Encoding.UTF8.GetString(ms.ToArray());
                if (string.IsNullOrWhiteSpace(reply))
                {
                    AddToLog($"Batch status <- {clientIp}: EMPTY reply");
                    foreach (var app in appsOnThisPc)
                        results.Add((app, ComputeVisualState(app, isRunning: false)));
                    return results;
                }

                // Parse lines
                var lines = reply
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .ToArray();

                AddToLog($"Batch status <- {clientIp}: bytes={ms.Length}, lines={lines.Length}, head='{lines[0]}'");

                if (lines.Length == 0 || !lines[0].Equals("STATUSBATCH", StringComparison.OrdinalIgnoreCase))
                {
                    AddToLog($"Batch status <- {clientIp}: INVALID header");
                    foreach (var app in appsOnThisPc)
                        results.Add((app, ComputeVisualState(app, isRunning: false)));
                    return results;
                }

                // Map reply -> running flag (Name + ProcessName)
                var mapByPath = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                for (int i = 1; i < lines.Length; i++)
                {
                    AddToLog($"Batch line status recieved<- {clientIp}: {lines[i]}");

                    var parts = lines[i].Split('|');
                    if (parts.Length < 5) continue;

                    string path = parts[2] ?? string.Empty;
                    bool isRunning = parts[3].Equals("Running", StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(path))
                        mapByPath[path] = isRunning;
                }

                int matched = 0;
                foreach (var app in appsOnThisPc)
                {
                    bool isRunning = false;

                    if (!string.IsNullOrWhiteSpace(app.RestartPath) &&
                        mapByPath.TryGetValue(app.RestartPath, out var running))
                    {
                        isRunning = running;
                        matched++;
                    }

                    results.Add((app, ComputeVisualState(app, isRunning)));
                }

                AddToLog($"Batch status <- {clientIp}: parsed={mapByPath.Count}, matched={matched}/{appsOnThisPc.Count}");

            }
            catch (Exception ex)
            {
                AddToLog($"Error checking remote batch status for {clientIp}: {ex.Message}");

                // Treat all as stopped for this cycle
                foreach (var app in appsOnThisPc)
                    results.Add((app, ComputeVisualState(app, isRunning: false)));
            }

            return results;
        }

        private AppRunVisualState ComputeAppState(ApplicationDetails app)
        {
            bool isRunning = false;

            if (string.IsNullOrWhiteSpace(app.ClientIP))
            {
                // Local app: just check the process
                isRunning = ProcessTerminator.IsRunning(app);
            }
            else
            {
                // Remote app: single-app status over TCP.
                // Timer-driven status uses batch requests now, but this is still used
                // in other flows and for compatibility.
                try
                {
                    using var client = new TcpClient();
                    client.SendTimeout = 6000;
                    client.ReceiveTimeout = 6000;

                    client.Connect(app.ClientIP, _settings.AppPort);
                    using var stream = client.GetStream();

                    var request = new ApplicationDetails
                    {
                        Name = app.Name,
                        ProcessName = app.ProcessName,
                        RestartPath = app.RestartPath,
                        ClientIP = null, // on the remote, this is a local app
                        AutoStart = app.AutoStart,
                        AutoStartDelayInSeconds = app.AutoStartDelayInSeconds,
                        NoWarn = app.NoWarn,
                        StartMinimized = app.StartMinimized,
                        GroupName = app.GroupName,
                        ActionType = RemoteActionType.AppControl,
                        StartRequested = false,
                        StopRequested = false
                    };

                    var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                    serializer.WriteObject(stream, request);
                    stream.Flush();

                    // IMPORTANT: tell the server we're done sending, so its read loop can finish
                    try
                    {
                        client.Client.Shutdown(SocketShutdown.Send);
                    }
                    catch (Exception ex)
                    {
                        AddToLog($"Warning: could not shutdown send side for status request to {app.ClientIP}: {ex.Message}");
                    }

                    using var ms = new System.IO.MemoryStream();
                    byte[] buffer = new byte[256];
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                        if (ms.Length > 1024) break; // safety cap
                    }

                    var reply = Encoding.UTF8.GetString(ms.ToArray());

                    // Expect: STATUS|Name|ProcessName|Running|Color
                    if (!string.IsNullOrWhiteSpace(reply))
                    {
                        var parts = reply.Split('|');
                        if (parts.Length >= 5 && parts[0].Equals("STATUS", StringComparison.OrdinalIgnoreCase))
                        {
                            isRunning = parts[3].Equals("Running", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If remote is offline or any error, treat as "stopped" but log it
                    AddToLog($"Error checking remote status for {app.Name} on {app.ClientIP}: {ex.Message}");
                    isRunning = false;
                }
            }

            return ComputeVisualState(app, isRunning);
        }

        private void UpdateAppStatusIndicator(ApplicationDetails app, AppRunVisualState state)
        {
            if (!_appStatusIndicators.TryGetValue(app, out var ctrl))
                return;

            // Remember last known state so we can preserve colors during layout rebuilds
            _lastAppRunStates[app] = state;

            Color color = StatusGray;
            switch (state)
            {
                case AppRunVisualState.Running:
                    color = StatusGreen;
                    break;
                case AppRunVisualState.UnexpectedlyStopped:
                    color = StatusRed;
                    break;
                case AppRunVisualState.AutoStartPending:
                    color = StatusOrange;
                    break;
                case AppRunVisualState.Stopped:
                    color = StatusGray;
                    break;
            }

            if (ctrl is Label lbl)
                lbl.ForeColor = color;
            else
                ctrl.BackColor = color;
        }


        private void MarkAppStartedByUs(ApplicationDetails app)
        {
            _appStartedByUs[app] = true;
            _appHasRunAtLeastOnce.Add(app);
        }

        private void MarkAppStoppedByUs(ApplicationDetails app)
        {
            _appStartedByUs[app] = false;
        }

        private void RenderGroupsAndApps()
        {
            _appStatusIndicators.Clear();

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
                        ForeColor = StatusGray,
                        Size = new Size(18, 18),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(appCard.Width - 20, (appCard.Height / 2) - 2)
                    };

                    // If we already know the last state of this app, keep that color when rebuilding the UI
                    if (_lastAppRunStates.TryGetValue(app, out var lastStateForApp))
                    {
                        Color initialColor = StatusGray;
                        switch (lastStateForApp)
                        {
                            case AppRunVisualState.Running:
                                initialColor = StatusGreen;
                                break;
                            case AppRunVisualState.UnexpectedlyStopped:
                                initialColor = StatusRed;
                                break;
                            case AppRunVisualState.AutoStartPending:
                                initialColor = StatusOrange;
                                break;
                            case AppRunVisualState.Stopped:
                                initialColor = StatusGray;
                                break;
                        }
                        statusLabel.ForeColor = initialColor;
                    }

                    appCard.Controls.Add(statusLabel);
                    appCard.Controls.Add(lblMeta2);
                    appCard.Controls.Add(lblName);

                    _appStatusIndicators[app] = statusLabel;

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

            RefreshAppStatuses();
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
                actionName = "stop";
            else if (!stop && start)
                actionName = "start";

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
                MarkAppStoppedByUs(app);
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
                    MarkAppStartedByUs(app);
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

            RefreshAppStatuses();
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
                    MarkAppStartedByUs(applicationDetails);
                else if (stop && !start)
                    MarkAppStoppedByUs(applicationDetails);

                using var client = new TcpClient(applicationDetails.ClientIP, _settings.AppPort);
                client.SendTimeout = 6000;
                using var stream = client.GetStream();

                applicationDetails.StopRequested = stop;
                applicationDetails.StartRequested = start;
                var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                serializer.WriteObject(stream, applicationDetails);
                stream.Flush();

                RefreshAppStatuses();
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
