using System;
using System.Diagnostics;
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
        private List<ApplicationDetails> selectedApps = new List<ApplicationDetails>();
        private TcpListener server;
        private volatile bool _serverRunning = true;
        private WebServer _webServer;
        private AppSettings _settings = new AppSettings();
        string exeDir = Path.GetDirectoryName(Application.ExecutablePath);

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            StartServer();
            LoadSettingsFromXml();
            LoadApplicationsFromXml();
            UpdateAppList();
            AutoStartApps();
            StartWebServer();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void LoadApplicationsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                XDocument xmlDocument = XDocument.Load(configPath);
                var root = xmlDocument.Root;
                var applicationsElement = root.Element("Applications");

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
                    };

                    selectedApps.Add(app);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading applications from XML: " + ex.Message);
            }
        }

        private void UpdateAppList()
        {
            AppFlowLayoutPanel.Controls.Clear();

            for (int i = 0; i < selectedApps.Count; i++)
            {
                var app = selectedApps[i];
                int index = i;

                var appButton = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = app.Name,
                    BackColor = !string.IsNullOrEmpty(app.ClientIP) ? Color.FromArgb(90, 143, 240) : Color.FromArgb(94, 103, 240),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };

                appButton.FlatAppearance.BorderSize = 0;

                // When clicking a button
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

        private void StartWebServer()
        {
            // exeDir is already defined in your Form1: Path.GetDirectoryName(Application.ExecutablePath)
            var indexPath = Path.Combine(exeDir, "index.html"); // <-- make it absolute
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
                        // When auto starting
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
            using var editForm = new AddAppForm(existing, index);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                if (editForm.DeleteRequested)
                {
                    selectedApps.RemoveAt(index);
                }
                else
                {
                    selectedApps[index] = editForm.AppData;
                }

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

        private async Task HandleAppButtonClickAsync(ApplicationDetails app, bool start, bool stop, bool skipConfirm)
        {
            DialogResult? confirmResult = null;

            if (!skipConfirm)
            {
                if (!app.NoWarn)
                {
                    if (InvokeRequired)
                    {
                        confirmResult = (DialogResult)await Task.Run(() =>
                            this.Invoke(new Func<DialogResult>(() =>
                                MessageBox.Show(
                                    $"Are you sure you want to stop {app.Name}\nand restart from: {app.RestartPath}",
                                    "Restart App",
                                    MessageBoxButtons.YesNo))));
                    }
                    else
                    {
                        confirmResult = MessageBox.Show(
                            $"Are you sure you want to stop {app.Name}\nand restart from: {app.RestartPath}",
                            "Restart App",
                            MessageBoxButtons.YesNo);
                    }
                }
            }

            if (skipConfirm || confirmResult == DialogResult.Yes || app.NoWarn)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(app.ProcessName);

                    if (processes.Length > 0 && stop)
                    {
                        foreach (Process process in processes)
                        {
                            process.Kill();
                            AddToLog($"Stopped {process.ProcessName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddToLog($"Error stopping {app.ProcessName}");
                    Debug.WriteLine("Error stopping the application: " + ex.Message);
                }
            }

            await Task.Delay(2000);

            try
            {
                Process[] processes2 = Process.GetProcessesByName(app.ProcessName);
                if (processes2.Length == 0 && !string.IsNullOrWhiteSpace(app.RestartPath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = app.RestartPath,
                        WorkingDirectory = Path.GetDirectoryName(app.RestartPath),
                        UseShellExecute = false,
                    };

                    if (app.StartMinimized)
                    {
                        startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    }

                    var process = Process.Start(startInfo);

                    if (app.StartMinimized)
                    {
                        _ = Task.Run(() =>
                        {
                            if (process != null)
                            {
                                // Wait for the main window to appear
                                process.WaitForInputIdle();
                                Thread.Sleep(2000); // Give the UI time to render

                                if (process.MainWindowHandle != IntPtr.Zero && WinApiHelper.IsWindowVisible(process.MainWindowHandle))
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

                if (!skipConfirm)
                {
                    if (!applicationDetails.NoWarn)
                    {
                        confirmResult = MessageBox.Show(
                            $"Are you sure you want to stop {applicationDetails.Name}\nand restart from: {applicationDetails.RestartPath}\nfrom IP: {applicationDetails.ClientIP}",
                            "Restart Remote App",
                            MessageBoxButtons.YesNo);
                    }
                }

                if (skipConfirm || confirmResult == DialogResult.Yes || applicationDetails.NoWarn)
                {
                    Debug.WriteLine("Restarting****");
                    using var client = new TcpClient(applicationDetails.ClientIP, _settings.AppPort);
                    client.SendTimeout = 3000;

                    using var stream = client.GetStream();

                    applicationDetails.KillProcess = stop;
                    var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                    serializer.WriteObject(stream, applicationDetails);
                    stream.Flush();
                    AddToLog($"Started Remote App {applicationDetails.Name} on {applicationDetails.ClientIP}");
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

                        client.ReceiveTimeout = 5000;

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

                            // Fire and forget the async handler:
                            _ = HandleAppButtonClickAsync(applicationDetails, true, applicationDetails.KillProcess, true);
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
                // Save settings
                new XElement("Settings",
                    new XElement("AppPort", _settings.AppPort),
                    new XElement("WebPort", _settings.WebPort),
                    new XElement("AutoStartWithWindows", _settings.AutoStartWithWindows),
                    new XElement("StartMinimized", _settings.StartMinimized),
                    new XElement("Schema", _settings.Schema)
                ),
                    // Save applications
                    new XElement("Applications",
                        selectedApps.Select(app =>
                            new XElement("Application",
                                new XElement("Name", app.Name),
                                new XElement("ProcessName", app.ProcessName),
                                new XElement("RestartPath", app.RestartPath),
                                new XElement("ClientIP", app.ClientIP),
                                new XElement("AutoStart", app.AutoStart),
                                new XElement("AutoStartDelayInSeconds", app.AutoStartDelayInSeconds),
                                new XElement("NoWarn", app.NoWarn),
                                new XElement("StartMinimized", app.StartMinimized)
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
            selectedApps.Clear();
            LoadApplicationsFromXml();
            UpdateAppList();
        }

        private void btnAddApp_Click(object sender, EventArgs e)
        {
            using var addForm = new AddAppForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                selectedApps.Add(addForm.AppData);
                SaveApplicationsToXml();
                UpdateAppList();
            }
        }

        private void LoadSettingsFromXml()
        {
            try
            {
                string configPath = getXMLConfigPath();
                var xmlDocument = XDocument.Load(configPath);
                var root = xmlDocument.Root;
                bool autoStartWithWindows = false;

                // Load settings
                var settingsElement = root.Element("Settings");
                if (settingsElement != null)
                {
                    _settings.AppPort = int.TryParse(settingsElement.Element("AppPort")?.Value, out var appPort) ? appPort : 2024;
                    _settings.WebPort = int.TryParse(settingsElement.Element("WebPort")?.Value, out var webPort) ? webPort : 8090;
                    _settings.AutoStartWithWindows = bool.TryParse(settingsElement.Element("AutoStartWithWindows")?.Value, out autoStartWithWindows);
                    _settings.StartMinimized = bool.TryParse(settingsElement.Element("StartMinimized")?.Value, out var sm) && sm;
                    _settings.Schema = settingsElement.Element("Schema")?.Value;
                }

                // Apply the startup setting
                if (autoStartWithWindows)
                {
                    StartupHelper.AddOrUpdateAppStartup(AddToLog);
                }
                else
                {
                    StartupHelper.RemoveAppStartup(AddToLog);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading settings and applications from XML: " + ex.Message);
            }
        }

        private void ShowSettingsDialog()
        {
            using var dlg = new SettingsForm(_settings);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var oldAppPort = _settings.AppPort;
            var oldWebPort = _settings.WebPort;

            // Update settings in memory
            _settings = dlg.Updated;

            // Persist settings & apps
            SaveApplicationsToXml(); // ensure this writes Settings incl. new flags

            // Apply Auto-start (Scheduled Task)
            if (_settings.AutoStartWithWindows)
                StartupHelper.AddOrUpdateAppStartup(AddToLog);
            else
                StartupHelper.RemoveAppStartup(AddToLog);

            // Restart TCP listener if AppPort changed
            if (oldAppPort != _settings.AppPort)
            {
                try
                {
                    server?.Stop();
                }
                catch { }
                StartServer(); // use _settings.AppPort inside StartServer
                AddToLog($"App listener restarted on port: {_settings.AppPort}");
            }

            // Restart web server if WebPort changed
            if (oldWebPort != _settings.WebPort)
            {
                try { _webServer?.Stop(); } catch { }
                StartWebServer(); // uses _settings.WebPort internally
                AddToLog($"Web server restarted on port: {_settings.WebPort}");
            }

            // If StartMinimized changed and currently visible, apply now (optional UX)
            if (_settings.StartMinimized && this.WindowState != FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Minimized;
                // if you do minimize-to-tray, also set ShowInTaskbar = false here
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // CLI override: pass --minimized to force minimized start (e.g., from Scheduled Task)
            bool cliMin = Environment.GetCommandLineArgs().Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            if (_settings.StartMinimized || cliMin)
            {
                // plain minimized to taskbar
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void btnOpenWeb_Click(object sender, EventArgs e)
        {
            _webServer.OpenWebInterfaceInBrowser();
        }

        private string getXMLConfigPath()
        {
            string configPath = Path.Combine(exeDir, "applications.xml");
            return configPath;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowSettingsDialog();
        }
    }
}
