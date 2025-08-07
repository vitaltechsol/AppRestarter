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
                        ClientIP = applicationElement.Element("ClientIP")?.Value
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
                    Width = 240,
                    Height = 45,
                    Text = app.Name
                };

                appButton.Click += (s, e) =>
                {
                    if (app.ClientIP != null)
                        HandleRemoteClientAppClick(app);
                    else
                        HandleAppButtonClick(app, false);
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
                HandleAppButtonClick(app, true);
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

        private void HandleAppButtonClick(ApplicationDetails app, bool skipConfirm)
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
                    if (processes.Length > 0)
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
                    AddToLog("nError stopping" + app.ProcessName);
                    MessageBox.Show("Error stopping the application: " + ex.Message);
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

                    System.Diagnostics.Process.Start(startInfo);
                    AddToLog("Started " + app.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the application: " + ex.Message);
            }
        }

        private void HandleRemoteClientAppClick(ApplicationDetails applicationDetails)
        {
            try
            {
                var confirmResult = MessageBox.Show("Are you sure you want to stop "
                    + applicationDetails.Name + "\nand restart from: " + applicationDetails.RestartPath
                    + "\nfrom IP: " + applicationDetails.ClientIP,
                 "Restart Remote App",
                 MessageBoxButtons.YesNo);

                if (confirmResult == DialogResult.Yes)
                {
                    AddToLog("Restarted Remote App " + applicationDetails.Name);
                    client = new TcpClient(applicationDetails.ClientIP, 2024); // Connect to the server on localhost
                    client.SendTimeout = 3000;
                    // Serialize and send the object using DataContractSerializer
                    var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                    NetworkStream stream = client.GetStream();
                    serializer.WriteObject(stream, applicationDetails);
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the application: " + ex.Message + "\nIP: " + applicationDetails.ClientIP);
            }
        }
        private void AddToLog(string message)
        {
            string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
            txtLog.AppendText(logLine);
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
                HandleAppButtonClick(applicationDetails, true);
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
                    new XElement("AutoStart", app.AutoStart)
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