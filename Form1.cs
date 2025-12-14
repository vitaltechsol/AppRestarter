using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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

        private readonly List<ApplicationDetails> _apps = new();
        private List<string> _groups = new();
        private readonly List<PcInfo> _pcs = new();

        private TcpListener server;
        private volatile bool _serverRunning = true;
        private WebServer _webServer;
        private AppSettings _settings = new();
        private readonly string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        private int _timeout = 8000;

        // NEW: all app status logic moved out of Form1/AppView into this manager
        private AppStatusManager _statusManager;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            ApplyDarkTheme();
            LoadSettingsFromXml();
            LoadApplicationsFromXml();
            LoadPcsFromXml();
            MakeNavButtonsCircular();

            // NEW: centralized app status logic (UI polling + TCP STATUS/STATUSBATCH responses)
            _statusManager = new AppStatusManager(
                uiInvoker: this,
                log: AddToLog,
                isAppsViewActive: () => _currentView == ViewMode.Apps,
                getAppsSnapshot: () => _apps.ToList(),
                getAppPort: () => _settings.AppPort,
                getTimeoutMs: () => _timeout
            );

            // Make group “cards” fluid width
            AppFlowLayoutPanel.Resize += AppFlowLayoutPanel_Resize;

            StartServer();
            StartWebServer();
            ShowAppsView();      // default view
            Task.Run(AutoStartApps);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void MakeNavButtonsCircular()
        {
            void MakeCircular(Button btn)
            {
                if (btn == null) return;

                try
                {
                    var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddEllipse(0, 0, btn.Width, btn.Height);
                    btn.Region = new Region(path);
                }
                catch
                {
                }
            }

            MakeCircular(btnNavApps);
            MakeCircular(btnNavPcs);
            MakeCircular(btnNavSettings);
        }

        private string getXMLConfigPath()
        {
            return Path.Combine(exeDir, "applications.xml");
        }

        // When the right pane resizes, stretch group panels to 100% width
        private void AppFlowLayoutPanel_Resize(object sender, EventArgs e)
        {
            AdjustGroupPanelWidths();
        }

        // ------------------ THEME / NAV STYLE ------------------

        private void ApplyDarkTheme()
        {
            BackColor = Color.FromArgb(2, 6, 23);

            AppFlowLayoutPanel.BackColor = Color.FromArgb(15, 23, 42);
            AppFlowLayoutPanel.ForeColor = Color.FromArgb(229, 231, 235);

            txtLog.BackColor = Color.FromArgb(15, 23, 42);
            txtLog.ForeColor = Color.FromArgb(229, 231, 235);
            txtLog.BorderStyle = BorderStyle.FixedSingle;

            panelLeftNav.BackColor = Color.FromArgb(3, 7, 18);

            lblNavApps.ForeColor = Color.FromArgb(226, 232, 240);
            lblNavPcs.ForeColor = Color.FromArgb(226, 232, 240);
            lblNavSettings.ForeColor = Color.FromArgb(226, 232, 240);

            foreach (var btn in new[] { btnNavApps, btnNavPcs, btnNavSettings })
            {
                btn.BackColor = Color.FromArgb(15, 23, 42);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.ForeColor = Color.FromArgb(226, 232, 240);
            }

            btnAddApp.BackColor = Color.FromArgb(2, 205, 215);
            btnAddApp.ForeColor = Color.FromArgb(15, 23, 42);
            btnAddApp.FlatStyle = FlatStyle.Flat;
            btnAddApp.FlatAppearance.BorderSize = 0;

            btnOpenWeb.BackColor = Color.FromArgb(31, 41, 55);
            btnOpenWeb.ForeColor = Color.FromArgb(226, 232, 240);
            btnOpenWeb.FlatStyle = FlatStyle.Flat;
            btnOpenWeb.FlatAppearance.BorderSize = 0;

            label1.ForeColor = Color.FromArgb(148, 163, 184);
        }

        private void HighlightNavButton(Button active)
        {
            var activeBg = Color.FromArgb(0, 172, 182);   // accent
            var inactiveBg = Color.FromArgb(15, 23, 42);  // dark
            var activeFg = Color.FromArgb(15, 23, 42);
            var inactiveFg = Color.FromArgb(226, 232, 240);

            foreach (var btn in new[] { btnNavApps, btnNavPcs })
            {
                bool isActive = (btn == active);
                btn.BackColor = isActive ? activeBg : inactiveBg;
                btn.ForeColor = isActive ? activeFg : inactiveFg;
            }

            btnNavSettings.BackColor = inactiveBg;
            btnNavSettings.ForeColor = inactiveFg;
        }

        // ------------------ NAV / VIEW SWITCH ------------------

        private void btnNavApps_Click(object sender, EventArgs e)
        {
            ShowAppsView();
        }

        private void btnNavPcs_Click(object sender, EventArgs e)
        {
            ShowPcsView();
        }

        // ------------------ LOGGING ------------------

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

        // ------------------ WEB SERVER ------------------

        private void StartWebServer()
        {
            try
            {
                var indexPath = Path.Combine(exeDir, "index.html");
                _webServer = new WebServer(_apps, _pcs, AddToLog, indexPath, _settings);

                _webServer.RestartRequested += (s, app) =>
                {
                    if (!string.IsNullOrEmpty(app.ClientIP))
                        HandleRemoteClientAppClick(app, start: true, stop: true, skipConfirm: true);
                    else
                        _ = HandleAppButtonClickAsync(app, start: true, stop: true, skipConfirm: true);
                };

                _webServer.StopRequested += (s, app) =>
                {
                    if (!string.IsNullOrEmpty(app.ClientIP))
                        HandleRemoteClientAppClick(app, start: false, stop: true, skipConfirm: true);
                    else
                        _ = HandleAppButtonClickAsync(app, start: false, stop: true, skipConfirm: true);
                };

                _webServer.Start(_settings.WebPort);
                AddToLog($"Web server started on port: {_settings.WebPort}");
            }
            catch (Exception ex)
            {
                AddToLog($"Failed to start Web Server: {ex.Message}");
            }
        }

        private void btnOpenWeb_Click(object sender, EventArgs e)
        {
            _webServer?.OpenWebInterfaceInBrowser();
        }

        // ------------------ TCP SERVER ------------------

        private void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, _settings.AppPort);
                server.Start();

                var serverThread = new System.Threading.Thread(ServerThread)
                {
                    IsBackground = true
                };
                serverThread.Start();

                AddToLog($"App listener started on port: {_settings.AppPort}");
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

                        using var ms = new MemoryStream();
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        try
                        {
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
                        }
                        catch (IOException ioEx)
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
                            ApplicationDetails applicationDetails = null;
                            AppStatusBatchRequest batchRequest = null;

                            // Try ApplicationDetails first
                            try
                            {
                                var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                                applicationDetails = (ApplicationDetails)serializer.ReadObject(ms);
                            }
                            catch (SerializationException serEx)
                            {
                                // If that fails, try AppStatusBatchRequest (STATUSBATCH)
                                ms.Position = 0;
                                try
                                {
                                    var batchSerializer = new DataContractSerializer(typeof(AppStatusBatchRequest));
                                    batchRequest = (AppStatusBatchRequest)batchSerializer.ReadObject(ms);
                                }
                                catch (SerializationException)
                                {
                                    string raw = Encoding.UTF8.GetString(ms.ToArray());
                                    AddToLog("Serialization error: " + serEx.Message);
                                    AddToLog("Raw payload (first 1000 chars): " +
                                             (raw.Length > 1000 ? raw.Substring(0, 1000) + "..." : raw));
                                    continue;
                                }
                            }

                            // Handle batch status request
                            if (batchRequest != null)
                            {
                                AddToLog($"Received TCP Batch Status Request for {batchRequest.Apps?.Count ?? 0} app(s).");
                                _statusManager?.HandleAppStatusBatchReceived(stream, batchRequest);
                                continue;
                            }

                            if (applicationDetails == null)
                                continue;

                            AddToLog($"\nReceived Object:\nName: {applicationDetails.Name}\nProcessName: {applicationDetails.ProcessName}" +
                                     $"\nRestartPath: {applicationDetails.RestartPath}\nClientIP: {applicationDetails.ClientIP}");

                            AddToLog($"Received TCP Message: Action={applicationDetails.ActionType}, Name={applicationDetails.Name}");

                            switch (applicationDetails.ActionType)
                            {
                                case RemoteActionType.AppControl:
                                    // If this is a real control request, do it.
                                    if (applicationDetails.StartRequested || applicationDetails.StopRequested)
                                    {
                                        _ = HandleAppButtonClickAsync(
                                            applicationDetails,
                                            applicationDetails.StartRequested,
                                            applicationDetails.StopRequested,
                                            skipConfirm: true);
                                    }
                                    else
                                    {
                                        AddToLog($"Status probe received for {applicationDetails.Name} (no start/stop).");
                                    }
                                    break;

                                case RemoteActionType.PcRestart:
                                    HandleRemotePcRestart(applicationDetails);
                                    break;

                                case RemoteActionType.PcShutdown:
                                    HandleRemotePcShutdown(applicationDetails);
                                    break;

                                case RemoteActionType.AppStatusBatch:
                                    // In case a client ever sends AppStatusBatch in an ApplicationDetails payload
                                    // (older/alternate protocol), try to respond if StatusBatchApps is present.
                                    if (applicationDetails.StatusBatchApps != null && applicationDetails.StatusBatchApps.Count > 0)
                                    {
                                        var br = new AppStatusBatchRequest
                                        {
                                            ActionType = RemoteActionType.AppStatusBatch,
                                            Apps = applicationDetails.StatusBatchApps
                                        };
                                        _statusManager?.HandleAppStatusBatchReceived(stream, br);
                                    }
                                    break;
                            }
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

        // ------------------ SETTINGS / XML ------------------

        private void LoadSettingsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();

                if (!System.IO.File.Exists(configPath))
                {
                    AddToLog("Applications.xml not found. New xml will be created");
                    return;
                }

                var xmlDocument = XDocument.Load(configPath);
                var root = xmlDocument.Root;
                bool autoStartWithWindows = false;

                var settingsElement = root.Element("Settings");
                if (settingsElement != null)
                {
                    _settings.AppPort = int.TryParse(settingsElement.Element("AppPort")?.Value, out var appPort)
                        ? appPort : 2024;
                    _settings.WebPort = int.TryParse(settingsElement.Element("WebPort")?.Value, out var webPort)
                        ? webPort : 8090;
                    _settings.AutoStartWithWindows = bool.TryParse(settingsElement.Element("AutoStartWithWindows")?.Value, out autoStartWithWindows);
                    _settings.StartMinimized = bool.TryParse(settingsElement.Element("StartMinimized")?.Value, out var sm) && sm;
                    _settings.Schema = settingsElement.Element("Schema")?.Value;

                    // restore main form size if present
                    if (int.TryParse(settingsElement.Element("MainFormWidth")?.Value, out var w) && w > 0 &&
                        int.TryParse(settingsElement.Element("MainFormHeight")?.Value, out var h) && h > 0)
                    {
                        this.ClientSize = new Size(w, h);
                    }
                }

                if (autoStartWithWindows)
                    StartupHelper.AddOrUpdateAppStartup(AddToLog);
                else
                    StartupHelper.RemoveAppStartup(AddToLog);

                if (_settings.StartMinimized)
                {
                    this.WindowState = FormWindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                AddToLog("Error loading settings and applications from XML: " + ex.Message);
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
                        new XElement("Schema", _settings.Schema),
                        new XElement("MainFormWidth", this.ClientSize.Width),
                        new XElement("MainFormHeight", this.ClientSize.Height)
                    ),
                    new XElement("Groups",
                        _groups.Select(g =>
                            new XElement("Group",
                                new XAttribute("Name", g)))
                    ),
                    new XElement("Applications",
                        _apps.Select(app =>
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
                    new XElement("Systems",
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

        private void LoadPcsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                var xml = XDocument.Load(configPath);
                var root = xml.Root;
                var systemNode = root?.Element("Systems");
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

        private bool IsAppRestarterSelf(ApplicationDetails app)
        {
            if (app == null)
                return false;

            bool nameHas =
                !string.IsNullOrWhiteSpace(app.Name) &&
                app.Name.IndexOf("AppRestarter", StringComparison.OrdinalIgnoreCase) >= 0;

            bool procHas =
                !string.IsNullOrWhiteSpace(app.ProcessName) &&
                app.ProcessName.IndexOf("AppRestarter", StringComparison.OrdinalIgnoreCase) >= 0;

            return nameHas || procHas;
        }

        // ------------------ TOP BUTTONS ------------------

        private void btnAddApp_Click(object sender, EventArgs e)
        {
            if (_currentView == ViewMode.Apps)
            {
                using var addForm = new AddAppForm(
                    existing: null,
                    index: -1,
                    getGroups: () => new List<string>(_groups),
                    manageGroups: ManageGroups,
                    pcs: new List<PcInfo>(_pcs)
                );
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    _apps.Add(addForm.AppData);
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

            bool cliMin = Environment.GetCommandLineArgs()
                .Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));
        }

        // ------------------ LIFECYCLE ------------------

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SaveApplicationsToXml();

                // NEW: stop centralized status polling timer
                _statusManager?.Dispose();

                _serverRunning = false;
                server?.Stop();
                _webServer?.Stop();
            }
            catch
            {
            }
        }
    }
}
