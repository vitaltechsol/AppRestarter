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
            // Clear the FlowLayoutPanel before adding buttons
            AppFlowLayoutPanel.Controls.Clear();

            foreach (var app in selectedApps)
            {
                Button appButton = new Button();
                appButton.Width = 160;
                appButton.Height = 45;
                appButton.Text = app.Name;
                if (app.ClientIP != null)
                {
                    appButton.Click += (sender, e) => HandleRemoteClientAppClick(app); // Attach an event handler
                }
                if (app.ClientIP == null)
                {
                    appButton.Click += (sender, e) => HandleAppButtonClick(app, true); // Attach an event handler
                }

                AppFlowLayoutPanel.Controls.Add(appButton);
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
                    MessageBox.Show("Error stopping the application: " + ex.Message);
                    AddToLog("nError stopping" + app.ProcessName);
                }
            }

            Thread.Sleep(2000);
            try
            {
                Process[] processes2 = Process.GetProcessesByName(app.ProcessName);
                if (processes2.Length == 0 && app.RestartPath != "")
                {
                    System.Diagnostics.Process.Start(app.RestartPath);
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
                    client = new TcpClient(applicationDetails.ClientIP, 2024); // Connect to the server on localhost
                    client.SendTimeout = 3000;
                    // Serialize and send the object using DataContractSerializer
                    var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                    NetworkStream stream = client.GetStream();
                    serializer.WriteObject(stream, applicationDetails);
                    client.Close();
                    AddToLog("Restarted Remote App " + applicationDetails.Name);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the application: " + ex.Message + "\nIP: " + applicationDetails.ClientIP);
            }
        }
        private void AddToLog(string message)
        {
            txtLog.Text = String.Format("{0} {1} \r\n", DateTime.Now, txtLog.Text);

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

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (server != null)
            {
                server.Stop();
                // Close any other resources or clean-up here if needed
            }
        }
    }
}