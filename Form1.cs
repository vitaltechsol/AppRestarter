using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AppRestarter
{
    public partial class Form1 : Form
    {
        private enum ViewMode
        {
            Apps,
            Pcs
        }

        private ViewMode _currentView = ViewMode.Apps;

        private List<ApplicationDetails> selectedApps = new List<ApplicationDetails>();

        private List<string> _groups = new List<string>();
        private List<PcInfo> _pcs = new List<PcInfo>();

        private TcpListener server;
        private volatile bool _serverRunning = true;
        private WebServer _webServer;
        private AppSettings _settings = new AppSettings();
        string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        private int _timeout = 8000;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            StartServer();
            LoadSettingsFromXml();
            LoadApplicationsFromXml();
            LoadPcsFromXml();
            ShowAppsView();
            AutoStartApps();
            StartWebServer();

            MakeNavButtonsCircular();
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void MakeNavButtonsCircular()
        {
            void MakeCircular(Button btn)
            {
                try
                {
                    var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddEllipse(0, 0, btn.Width, btn.Height);
                    btn.Region = new Region(path);
                }
                catch { }
            }

            MakeCircular(btnNavApps);
            MakeCircular(btnNavPcs);
        }

        private string getXMLConfigPath()
        {
            return Path.Combine(exeDir, "applications.xml");
        }

        private void LoadApplicationsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                XDocument xmlDocument = XDocument.Load(configPath);
                var root = xmlDocument.Root;

                _groups = LoadGroups(root);

                var applicationsElement = root.Element("Applications");
                selectedApps.Clear();

                foreach (XElement applicationElement in applicationsElement.Elements("Application"))
                {
                    ApplicationDetails app = new ApplicationDetails
                    {
                        Name = applicationElement.Element("Name").Value,
                        ProcessName = applicationElement.Element("ProcessName").Value,
                        RestartPath = applicationElement.Element("RestartPath")?.Value,
                        ClientIP = applicationElement.Element("ClientIP")?.Value,
                        AutoStart = bool.TryParse(applicationElement.Element("AutoStart")?.Value, out var autoStart) && autoStart,
                        AutoStartDelayInSeconds = int.TryParse(applicationElement.Element("AutoStartDelayInSeconds")?.Value, out var delay) ? delay : 0,
                        NoWarn = bool.TryParse(applicationElement.Element("NoWarn")?.Value, out var noWarn) ? noWarn : false,
                        StartMinimized = bool.TryParse(applicationElement.Element("StartMinimized")?.Value, out var startMinimized) ? startMinimized : false,
                        GroupName = applicationElement.Element("GroupName")?.Value
                    };

                    selectedApps.Add(app);
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
                    var name = g.Attribute("Name")?.Value;
                    if (!string.IsNullOrWhiteSpace(name))
                        groups.Add(name);
                }
            }
            return groups
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ---------- VIEW SWITCHING ----------

        private void ShowAppsView()
        {
            _currentView = ViewMode.Apps;
            btnAddApp.Text = "Add New App";
            HighlightNavButton(btnNavApps);
            AppFlowLayoutPanel.Controls.Clear();
            RenderGroupsAndApps();
        }

        private void ShowPcsView()
        {
            _currentView = ViewMode.Pcs;
            btnAddApp.Text = "Add New PC";
            HighlightNavButton(btnNavPcs);
            AppFlowLayoutPanel.Controls.Clear();
            RenderPcButtons();
        }

        private void HighlightNavButton(Button active)
        {
            var activeColor = Color.FromArgb(0, 122, 204);
            var inactiveColor = Color.FromArgb(64, 64, 64);

            btnNavApps.BackColor = (active == btnNavApps) ? activeColor : inactiveColor;
            btnNavPcs.BackColor = (active == btnNavPcs) ? activeColor : inactiveColor;
        }

        private void UpdateAppList()
        {
            if (_currentView != ViewMode.Apps)
                return;

            AppFlowLayoutPanel.Controls.Clear();
            RenderGroupsAndApps();
        }

        private void RenderGroupsAndApps()
        {
            // Group buttons
            foreach (var g in _groups)
            {
                var groupBtn = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = $"[{g}]",
                    BackColor = Color.FromArgb(8, 111, 118),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };
                groupBtn.FlatAppearance.BorderSize = 0;

                groupBtn.Click += async (s, e) =>
                {
                    var apps = selectedApps
                        .Where(a => string.Equals(a.GroupName, g, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var app in apps)
                    {
                        if (!string.IsNullOrEmpty(app.ClientIP))
                            HandleRemoteClientAppClick(app, true, true, false);
                        else
                            await HandleAppButtonClickAsync(app, true, true, false);
                    }
                };

                groupBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.Items.Add("Stop").Click += async (ms, me) =>
                        {
                            var apps = selectedApps
                                .Where(a => string.Equals(a.GroupName, g, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                            foreach (var app in apps)
                            {
                                if (!string.IsNullOrEmpty(app.ClientIP))
                                    HandleRemoteClientAppClick(app, false, true, true);
                                else
                                    await HandleAppButtonClickAsync(app, false, true, true);
                            }
                        };
                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(groupBtn);
            }

            // Per-app buttons
            for (int i = 0; i < selectedApps.Count; i++)
            {
                var app = selectedApps[i];
                int index = i;

                var appButton = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = app.Name,
                    BackColor = !string.IsNullOrEmpty(app.ClientIP)
                        ? Color.FromArgb(90, 143, 240)
                        : Color.FromArgb(94, 103, 240),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };

                appButton.FlatAppearance.BorderSize = 0;

                appButton.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(app.ClientIP))
                        HandleRemoteClientAppClick(app, true, true, false);
                    else
                        _ = HandleAppButtonClickAsync(app, true, true, false);
                };

                appButton.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.Items.Add("Edit").Click += (ms, me) => EditApp(index);
                        menu.Items.Add("Stop").Click += (ms, me) => StopApp(index);
                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(appButton);
            }
        }

        // ---------- PCs VIEW ----------

        private void RenderPcButtons()
        {
            AppFlowLayoutPanel.Controls.Clear();

            // "All PCs" button
            if (_pcs.Count > 0)
            {
                var allBtn = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = "[All PCs]",
                    BackColor = Color.FromArgb(128, 64, 64),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };
                allBtn.FlatAppearance.BorderSize = 0;

                // Left-click: Shutdown all
                allBtn.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        "Are you sure you want to SHUT DOWN all configured PCs?",
                        "Confirm Shutdown All",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes) return;

                    foreach (var pc in _pcs)
                    {
                        await PcPowerController.ShutdownAsync(pc, AddToLog);
                    }
                };

                // Right-click: context menu
                allBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var menu = new ContextMenuStrip();
                        menu.Items.Add("Shutdown All").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                "Are you sure you want to SHUT DOWN all configured PCs?",
                                "Confirm Shutdown All",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (confirm != DialogResult.Yes) return;

                            foreach (var pc in _pcs)
                            {
                                await PcPowerController.ShutdownAsync(pc, AddToLog);
                            }
                        };

                        menu.Items.Add("Restart All").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                "Are you sure you want to RESTART all configured PCs?",
                                "Confirm Restart All",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (confirm != DialogResult.Yes) return;

                            foreach (var pc in _pcs)
                            {
                                await PcPowerController.RestartAsync(pc, AddToLog);
                            }
                        };

                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(allBtn);
            }

            // Individual PCs
            for (int i = 0; i < _pcs.Count; i++)
            {
                var pc = _pcs[i];
                int index = i;

                var pcBtn = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = pc.Name,
                    BackColor = Color.FromArgb(70, 90, 160),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };
                pcBtn.FlatAppearance.BorderSize = 0;

                // Left-click: Shutdown
                pcBtn.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        $"Shut down {pc.Name} ({pc.IP})?",
                        "Confirm Shutdown",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;

                    await PcPowerController.ShutdownAsync(pc, AddToLog);
                };

                // Right-click: context menu
                pcBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var menu = new ContextMenuStrip();

                        menu.Items.Add("Shutdown").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                $"Shut down {pc.Name} ({pc.IP})?",
                                "Confirm Shutdown",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes) return;

                            await PcPowerController.ShutdownAsync(pc, AddToLog);
                        };

                        menu.Items.Add("Restart").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                $"Restart {pc.Name} ({pc.IP})?",
                                "Confirm Restart",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes) return;

                            await PcPowerController.RestartAsync(pc, AddToLog);
                        };

                        menu.Items.Add("Edit").Click += (ms, me) =>
                        {
                            using var dlg = new AddPcForm(pc);
                            if (dlg.ShowDialog(this) == DialogResult.OK)
                            {
                                _pcs[index] = dlg.PcData;
                                SaveApplicationsToXml();
                                RenderPcButtons();
                            }
                        };

                        menu.Items.Add("Delete").Click += (ms, me) =>
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

                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(pcBtn);
            }
        }

        private void StartWebServer()
        {
            var indexPath = Path.Combine(exeDir, "index.html");
            _webServer = new WebServer(selectedApps, AddToLog, indexPath);
            _webServer.RestartRequested += (s, app) =>
            {
                if (!string.IsNullOrEmpty(app.ClientIP))
                    HandleRemoteClientAppClick(app, true, true, true);
                else
                    _ = HandleAppButtonClickAsync(app, true, true, true);
            };
            _webServer.Start(_settings.WebPort);
        }

        private void AutoStartApps()
        {
            foreach (var app in selectedApps.Where(a => a.AutoStart))
            {
                AddToLog($"Auto starting {app.Name} in {app.AutoStartDelayInSeconds} seconds");

                Task.Run(async () =>
                {
                    if (app.AutoStartDelayInSeconds > 0)
                        await Task.Delay(app.AutoStartDelayInSeconds * 1000);

                    if (!string.IsNullOrEmpty(app.ClientIP))
                    {
                        Debug.WriteLine($"AutoStart ClientIP {app.Name}");
                        HandleRemoteClientAppClick(app, true, false, true);
                    }
                    else
                        await HandleAppButtonClickAsync(app, true, false, true);
                });
            }
        }

        private void EditApp(int index)
        {
            var existing = selectedApps[index];
            using var editForm = new AddAppForm(
                existing,
                index,
                getGroups: () => new List<string>(_groups),
                manageGroups: ManageGroups
            );
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                if (editForm.DeleteRequested)
                    selectedApps.RemoveAt(index);
                else
                    selectedApps[index] = editForm.AppData;

                SaveApplicationsToXml();
                UpdateAppList();
            }
        }

        private void StopApp(int index)
        {
            var existing = selectedApps[index];
            if (!string.IsNullOrEmpty(existing.ClientIP))
                HandleRemoteClientAppClick(existing, false, true, false);
            else
                _ = HandleAppButtonClickAsync(existing, false, true, false);
        }

        private void btnAddApp_Click(object sender, EventArgs e)
        {
            if (_currentView == ViewMode.Apps)
            {
                using var addForm = new AddAppForm(
                    existing: null,
                    index: -1,
                    getGroups: () => new List<string>(_groups),
                    manageGroups: ManageGroups
                );
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    selectedApps.Add(addForm.AppData);
                    SaveApplicationsToXml();
                    UpdateAppList();
                }
            }
            else
            {
                using var addPcForm = new AddPcForm();
                if (addPcForm.ShowDialog(this) == DialogResult.OK)
                {
                    _pcs.Add(addPcForm.PcData);
                    SaveApplicationsToXml();
                    RenderPcButtons();
                }
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
                foreach (var app in selectedApps)
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
                foreach (var app in selectedApps)
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

            int stopped = 0;
            if (skipConfirm || confirmResult == DialogResult.Yes || app.NoWarn)
            {
                if (stop)
                {
                    stopped = await ProcessTerminator.StopAsync(app, AddToLog, timeoutMs: _timeout);
                }
            }

            if (start && stopped > 0)
            {
                await Task.Delay(2000);
            }

            try
            {
                bool isAppRunning = start && ProcessTerminator.IsRunning(app);
                if (start && isAppRunning)
                {
                    AddToLog($"Skipped starting {app.Name}: already running.");
                }

                if (start && !string.IsNullOrWhiteSpace(app.RestartPath) && !isAppRunning)
                {
                    AddToLog($"Starting {app.Name}");
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = app.RestartPath,
                        WorkingDirectory = Path.GetDirectoryName(app.RestartPath),
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
                                Thread.Sleep(2000);
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
                        $"Are you sure you want to {actionName} {applicationDetails.Name}\n from: {applicationDetails.RestartPath}\nfrom IP: {applicationDetails.ClientIP}",
                        "Restart Remote App",
                        MessageBoxButtons.YesNo);
                }

                if (skipConfirm || confirmResult == DialogResult.Yes || applicationDetails.NoWarn)
                {
                    AddToLog($"Sending Remote App Request {applicationDetails.Name} on {applicationDetails.ClientIP} to {actionName}");
                    using var client = new TcpClient(applicationDetails.ClientIP, _settings.AppPort);
                    client.SendTimeout = 3000;
                    using var stream = client.GetStream();

                    applicationDetails.StopRequested = stop;
                    applicationDetails.StartRequested = start;
                    var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                    serializer.WriteObject(stream, applicationDetails);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                var msg = $"Error starting {applicationDetails.ProcessName} {ex.Message} \nIP: {applicationDetails.ClientIP}";
                MessageBox.Show(msg);
                AddToLog(msg);
                Debug.WriteLine(msg);
            }
        }

        private void AddToLog(string message)
        {
            string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";

            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => txtLog.Text = logLine + txtLog.Text));
            }
            else
            {
                txtLog.Text = logLine + txtLog.Text;
            }
        }

        private void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, _settings.AppPort);
                server.Start();

                Thread serverThread = new Thread(ServerThread)
                {
                    IsBackground = true
                };
                serverThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the server: " + ex.Message);
            }
        }

        private void ServerThread()
        {
            while (_serverRunning)
            {
                try
                {
                    if (server.Pending())
                    {
                        using var client = server.AcceptTcpClient();
                        using var stream = client.GetStream();

                        client.ReceiveTimeout = _timeout;

                        using var ms = new System.IO.MemoryStream();
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        try
                        {
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
                        }
                        catch (System.IO.IOException ioEx)
                        {
                            AddToLog("IO reading client stream: " + ioEx.Message);
                        }

                        ms.Position = 0;

                        if (ms.Length == 0)
                        {
                            AddToLog("Received empty payload from client.");
                            continue;
                        }

                        try
                        {
                            var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                            var applicationDetails = (ApplicationDetails)serializer.ReadObject(ms);

                            AddToLog($"\nReceived Object:\nName: {applicationDetails.Name}\nProcessName: {applicationDetails.ProcessName}" +
                                $"\nRestartPath: {applicationDetails.RestartPath}\nClientIP: {applicationDetails.ClientIP}");

                            _ = HandleAppButtonClickAsync(
                                    applicationDetails,
                                    applicationDetails.StartRequested,
                                    applicationDetails.StopRequested,
                                    true);
                        }
                        catch (SerializationException serEx)
                        {
                            string raw = Encoding.UTF8.GetString(ms.ToArray());
                            AddToLog("Serialization error: " + serEx.Message);
                            AddToLog("Raw payload (first 1000 chars): " + (raw.Length > 1000 ? raw.Substring(0, 1000) + "..." : raw));
                        }
                        catch (Exception ex)
                        {
                            AddToLog("Unexpected server processing error: " + ex.Message);
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch (SocketException sockEx)
                {
                    if (_serverRunning)
                        AddToLog("Socket error: " + sockEx.Message);
                }
                catch (Exception ex)
                {
                    AddToLog("Server error: " + ex.Message);
                }
            }
        }

        private void SaveApplicationsToXml()
        {
            var xmlFilePath = getXMLConfigPath();

            var doc = new XDocument(
                new XElement("Root",
                    new XElement("Settings",
                        new XElement("AppPort", _settings.AppPort),
                        new XElement("WebPort", _settings.WebPort),
                        new XElement("AutoStartWithWindows", _settings.AutoStartWithWindows),
                        new XElement("StartMinimized", _settings.StartMinimized),
                        new XElement("Schema", _settings.Schema)
                    ),
                    new XElement("Groups",
                        _groups.Select(g =>
                            new XElement("Group",
                                new XAttribute("Name", g)))
                    ),
                    new XElement("Applications",
                        selectedApps.Select(app =>
                        {
                            var x = new XElement("Application",
                                new XElement("Name", app.Name),
                                new XElement("ProcessName", app.ProcessName),
                                new XElement("RestartPath", app.RestartPath),
                                new XElement("ClientIP", app.ClientIP),
                                new XElement("AutoStart", app.AutoStart),
                                new XElement("AutoStartDelayInSeconds", app.AutoStartDelayInSeconds),
                                new XElement("NoWarn", app.NoWarn),
                                new XElement("StartMinimized", app.StartMinimized)
                            );
                            if (!string.IsNullOrWhiteSpace(app.GroupName))
                                x.Add(new XElement("GroupName", app.GroupName));
                            return x;
                        })
                    ),
                    new XElement("System",
                        _pcs.Select(pc =>
                            new XElement("PC",
                                new XElement("Name", pc.Name),
                                new XElement("IP", pc.IP)
                            )
                        )
                    )
                )
            );

            doc.Save(xmlFilePath);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _serverRunning = false;
                server?.Stop();
                _webServer?.Stop();
            }
            catch { }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadApplicationsFromXml();
            LoadPcsFromXml();

            if (_currentView == ViewMode.Apps)
                UpdateAppList();
            else
                RenderPcButtons();
        }

        private void btnOpenWeb_Click(object sender, EventArgs e)
        {
            _webServer.OpenWebInterfaceInBrowser();
        }

        private void LoadSettingsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                var xmlDocument = XDocument.Load(configPath);
                var root = xmlDocument.Root;
                bool autoStartWithWindows = false;

                var settingsElement = root.Element("Settings");
                if (settingsElement != null)
                {
                    _settings.AppPort = int.TryParse(settingsElement.Element("AppPort")?.Value, out var appPort) ? appPort : 2024;
                    _settings.WebPort = int.TryParse(settingsElement.Element("WebPort")?.Value, out var webPort) ? webPort : 8090;
                    _settings.AutoStartWithWindows = bool.TryParse(settingsElement.Element("AutoStartWithWindows")?.Value, out autoStartWithWindows);
                    _settings.StartMinimized = bool.TryParse(settingsElement.Element("StartMinimized")?.Value, out var sm) && sm;
                    _settings.Schema = settingsElement.Element("Schema")?.Value;
                }

                if (autoStartWithWindows)
                    StartupHelper.AddOrUpdateAppStartup(AddToLog);
                else
                    StartupHelper.RemoveAppStartup(AddToLog);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading settings and applications from XML: " + ex.Message);
            }
        }

        private void LoadPcsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                var xml = XDocument.Load(configPath);
                var root = xml.Root;
                var systemNode = root?.Element("System");
                if (systemNode == null) return;

                _pcs.Clear();
                foreach (var pcEl in systemNode.Elements("PC"))
                {
                    var name = pcEl.Element("Name")?.Value ?? "";
                    var ip = pcEl.Element("IP")?.Value ?? "";
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(ip))
                    {
                        _pcs.Add(new PcInfo { Name = name, IP = ip });
                    }
                }
            }
            catch (Exception ex)
            {
                AddToLog("Error loading PCs from XML: " + ex.Message);
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using var dlg = new SettingsForm(_settings);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var oldAppPort = _settings.AppPort;
            var oldWebPort = _settings.WebPort;

            _settings = dlg.Updated;

            SaveApplicationsToXml();

            if (_settings.AutoStartWithWindows)
                StartupHelper.AddOrUpdateAppStartup(AddToLog);
            else
                StartupHelper.RemoveAppStartup(AddToLog);

            if (oldAppPort != _settings.AppPort)
            {
                try { server?.Stop(); } catch { }
                StartServer();
                AddToLog($"App listener restarted on port: {_settings.AppPort}");
            }

            if (oldWebPort != _settings.WebPort)
            {
                try { _webServer?.Stop(); } catch { }
                StartWebServer();
                AddToLog($"Web server restarted on port: {_settings.WebPort}");
            }

            if (_settings.StartMinimized && this.WindowState != FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            bool cliMin = Environment.GetCommandLineArgs()
                .Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            if (_settings.StartMinimized || cliMin)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void btnNavApps_Click(object sender, EventArgs e)
        {
            ShowAppsView();
        }

        private void btnNavPcs_Click(object sender, EventArgs e)
        {
            ShowPcsView();
        }
    }
}
