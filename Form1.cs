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
        private readonly string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        private int _timeout = 8000;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            StartServer();
            LoadSettingsFromXml();
            LoadApplicationsFromXml();
            LoadPcsFromXml();

            ShowAppsView();      // default view
            AutoStartApps();
            StartWebServer();

            MakeNavButtonsCircular();
        }

        private void Form1_Load(object sender, EventArgs e) { }

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
            return System.IO.Path.Combine(exeDir, "applications.xml");
        }

        // ------------------ NAV / VIEW SWITCH ------------------

        private void HighlightNavButton(Button active)
        {
            var activeColor = Color.FromArgb(0, 122, 204);
            var inactiveColor = Color.FromArgb(64, 64, 64);

            btnNavApps.BackColor = (active == btnNavApps) ? activeColor : inactiveColor;
            btnNavPcs.BackColor = (active == btnNavPcs) ? activeColor : inactiveColor;
        }

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
            var indexPath = System.IO.Path.Combine(exeDir, "index.html");
            _webServer = new WebServer(selectedApps, _pcs, AddToLog, indexPath);
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

        private void btnOpenWeb_Click(object sender, EventArgs e)
        {
            _webServer.OpenWebInterfaceInBrowser();
        }

        // ------------------ TCP SERVER ------------------

        private void StartServer()
        {
            try
            {
                server = new TcpListener(System.Net.IPAddress.Any, _settings.AppPort);
                server.Start();

                Thread serverThread = new Thread(ServerThread)
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
                                    skipConfirm: true);
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

        // ------------------ SETTINGS / XML ------------------

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

        // ------------------ LIFECYCLE ------------------

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
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
