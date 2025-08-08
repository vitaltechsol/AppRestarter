using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Linq;

namespace AppRestarter
{
    public partial class Form1 : Form
    {
        private List<ApplicationDetails> selectedApps = new List<ApplicationDetails>();
        private TcpListener server;
        private TcpClient client;


        public Form1()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            StartServer();
            LoadApplicationsFromXml("applications.xml");
            UpdateAppList();
            AutoStartApps();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

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
                    BackColor = Color.FromArgb(90, 143, 240),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };

                appButton.FlatAppearance.BorderSize = 0;

                appButton.Click += (s, e) =>
                {
                    if (app.ClientIP != null)
                        HandleRemoteClientAppClick(app, false, false);
                    else
                        HandleAppButtonClick(app, false, false);
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

        private void AutoStartApps()
        {
            foreach (var app in selectedApps.Where(a => a.AutoStart))
            {
                AddToLog($"Auto starting {app.Name} in {app.AutoStartDelayInSeconds} seconds");

                Task.Run(async () =>
                {

                    if (app.AutoStartDelayInSeconds > 0)
                        await Task.Delay(app.AutoStartDelayInSeconds * 1000);

                    if (app.ClientIP != null)
                    {
                        Debug.WriteLine($"AutoStart ClientIP {app.Name}");
                        HandleRemoteClientAppClick(app, true, true);
                    }
                    else
                        HandleAppButtonClick(app, true, true);

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

        private void HandleAppButtonClick(ApplicationDetails app, bool skipConfirm, bool noKill)
        {
            Process[] processes = Process.GetProcessesByName(app.ProcessName);
            Debug.WriteLine("kill " + app.ProcessName + processes.Length);
            DialogResult? confirmResult = null;

            if (!skipConfirm)
            {
                confirmResult = MessageBox.Show("Are you sure you want to stop " + app.Name + "\nand restart from: " + app.RestartPath,
                    "Restart App",
                    MessageBoxButtons.YesNo);
            }

            if (skipConfirm || confirmResult == DialogResult.Yes)
            {

                try
                {
                    if (processes.Length > 0 && !noKill)
                    {
                        foreach (Process process in processes)
                        {
                            process.Kill(); // Kill the process
                            AddToLog("Stopped " + process.ProcessName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddToLog("Error stopping" + app.ProcessName);
                    Debug.WriteLine("Error stopping the application: " + ex.Message);
                }
            }

            Thread.Sleep(2000);
            try
            {
                Process[] processes2 = Process.GetProcessesByName(app.ProcessName);
                if (processes2.Length == 0 && app.RestartPath != "")
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
                MessageBox.Show($"Error starting {app.ProcessName} {ex.Message}");
            }
        }

        private void HandleRemoteClientAppClick(ApplicationDetails applicationDetails, bool skipConfirm, bool noKill)
        {
           //  AddToLog("Restarting Remote App " + applicationDetails.Name);
            try
            {
                DialogResult? confirmResult = null;

                if (!skipConfirm)
                {
                    confirmResult = MessageBox.Show("Are you sure you want to stop "
                        + applicationDetails.Name + "\nand restart from: " + applicationDetails.RestartPath
                        + "\nfrom IP: " + applicationDetails.ClientIP,
                     "Restart Remote App",
                     MessageBoxButtons.YesNo);
                }

                if (skipConfirm || confirmResult == DialogResult.Yes)
                {
                    Debug.WriteLine("Restarting****");
                    client = new TcpClient(applicationDetails.ClientIP, 2024); // Connect to the server on localhost
                    client.SendTimeout = 3000;
                    // Serialize and send the object using DataContractSerializer
                    var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                    NetworkStream stream = client.GetStream();
                    applicationDetails.NoKill = noKill;
                    serializer.WriteObject(stream, applicationDetails);
                    client.Close();
                    AddToLog($"Started Remote App{applicationDetails.Name} on {applicationDetails.ClientIP}");
                }
            }
            catch (Exception ex)
            {
            //    var msg = $"Error starting {applicationDetails.ProcessName} {ex.Message} \nIP: {applicationDetails.ClientIP}";
                MessageBox.Show(ex.Message);
                // AddToLog(msg);
                Debug.WriteLine(ex);
          
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
                server = new TcpListener(IPAddress.Any, 2024); // Choose a suitable port
                server.Start();

                Thread serverThread = new Thread(ServerThread);
                serverThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the server: " + ex.Message);
            }
        }

        private void ServerThread()
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                // Deserialize the object from the stream using DataContractSerializer
                var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                var applicationDetails = (ApplicationDetails)serializer.ReadObject(stream);

                // Handle the received object (e.g., show in a message box)
                AddToLog($"\nReceived Object:\nName: {applicationDetails.Name}\nProcessName: {applicationDetails.ProcessName}" +
                    $"\nRestartPath: {applicationDetails.RestartPath}\nClientIP: {applicationDetails.ClientIP}");
                client.Close();
                HandleAppButtonClick(applicationDetails, true, applicationDetails.NoKill);
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
            if (server != null)
            {
                server.Stop();
                // Close any other resources or clean-up here if needed
            }
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