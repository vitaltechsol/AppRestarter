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
        private SimpleWebServer _webServer;


        public Form1()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            StartServer();
            LoadApplicationsFromXml("applications.xml");
            UpdateAppList();
            AutoStartApps();
            StartWebServer();
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void LoadApplicationsFromXml(string xmlFilePath)
        {
            try
            {
                XDocument xmlDocument = XDocument.Load(xmlFilePath);
                foreach (XElement applicationElement in xmlDocument.Root.Elements("Application"))
                {
                    ApplicationDetails app = new ApplicationDetails
                    {
                        Name = applicationElement.Element("Name").Value,
                        ProcessName = applicationElement.Element("ProcessName").Value,
                        RestartPath = applicationElement.Element("RestartPath")?.Value,
                        ClientIP = applicationElement.Element("ClientIP")?.Value,
                        AutoStart = bool.TryParse(applicationElement.Element("AutoStart")?.Value, out var autoStart) && autoStart,
                        AutoStartDelayInSeconds = int.TryParse(applicationElement.Element("AutoStartDelayInSeconds")?.Value, out var delay) ? delay : 0
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

                appButton.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(app.ClientIP))
                        HandleRemoteClientAppClick(app, false, false);
                    else
                        _ = HandleAppButtonClickAsync(app, false, false);
                };

                appButton.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.Items.Add("Edit").Click += (ms, me) => EditApp(index);
                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(appButton);
            }
        }

        private void StartWebServer()
        {
            _webServer = new SimpleWebServer(selectedApps, AddToLog, "index.html");
            _webServer.RestartRequested += (s, app) =>
            {
                if (!string.IsNullOrEmpty(app.ClientIP))
                    HandleRemoteClientAppClick(app, true, false);
                else
                    _ = HandleAppButtonClickAsync(app, true, false); // If you want async restart
            };
            _webServer.Start(8080);
            AddToLog($"Web Server started on 8080");
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
                        HandleRemoteClientAppClick(app, true, true);
                    }
                    else
                        await HandleAppButtonClickAsync(app, true, true);
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

                SaveApplicationsToXml("applications.xml");
                UpdateAppList();
            }
        }

        private async Task HandleAppButtonClickAsync(ApplicationDetails app, bool skipConfirm, bool noKill)
        {
            DialogResult? confirmResult = null;

            if (!skipConfirm)
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

            if (skipConfirm || confirmResult == DialogResult.Yes)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(app.ProcessName);

                    if (processes.Length > 0 && !noKill)
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
                        UseShellExecute = false
                    };

                    Process.Start(startInfo);
                    AddToLog($"Started {app.Name}");
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

        private void HandleRemoteClientAppClick(ApplicationDetails applicationDetails, bool skipConfirm, bool noKill)
        {
            try
            {
                DialogResult? confirmResult = null;

                if (!skipConfirm)
                {
                    confirmResult = MessageBox.Show(
                        $"Are you sure you want to stop {applicationDetails.Name}\nand restart from: {applicationDetails.RestartPath}\nfrom IP: {applicationDetails.ClientIP}",
                        "Restart Remote App",
                        MessageBoxButtons.YesNo);
                }

                if (skipConfirm || confirmResult == DialogResult.Yes)
                {
                    Debug.WriteLine("Restarting****");
                    using var client = new TcpClient(applicationDetails.ClientIP, 2024);
                    client.SendTimeout = 3000;

                    using var stream = client.GetStream();

                    applicationDetails.NoKill = noKill;
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
                server = new TcpListener(IPAddress.Any, 2024);
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
                            _ = HandleAppButtonClickAsync(applicationDetails, true, applicationDetails.NoKill);
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

        private void SaveApplicationsToXml(string xmlFilePath)
        {
            var doc = new XDocument(new XElement("Applications",
                selectedApps.Select(app => new XElement("Application",
                    new XElement("Name", app.Name),
                    new XElement("ProcessName", app.ProcessName),
                    new XElement("RestartPath", app.RestartPath),
                    new XElement("ClientIP", app.ClientIP),
                    new XElement("AutoStart", app.AutoStart),
                    new XElement("AutoStartDelayInSeconds", app.AutoStartDelayInSeconds)
                ))
            ));
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
            LoadApplicationsFromXml("applications.xml");
            UpdateAppList();
        }

        private void btnAddApp_Click(object sender, EventArgs e)
        {
            using var addForm = new AddAppForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                selectedApps.Add(addForm.AppData);
                SaveApplicationsToXml("applications.xml");
                UpdateAppList();
            }
        }
    }
}
