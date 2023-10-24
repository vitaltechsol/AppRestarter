using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace AppRestarter
{
    public partial class Form1 : Form
    {
        private List<ApplicationDetails> selectedApps = new List<ApplicationDetails>();

        public Form1()
        {
            InitializeComponent();
            LoadApplicationsFromXml("applications.xml");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateAppList();
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
                        RestartPath = applicationElement.Element("RestartPath").Value
                    };

                    selectedApps.Add(app);
                }
                UpdateAppList();

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
                appButton.Click += (sender, e) => HandleAppButtonClick(app); // Attach an event handler

                AppFlowLayoutPanel.Controls.Add(appButton);
            }
        }

        private void HandleAppButtonClick(ApplicationDetails app)
        {
            Process[] processes = Process.GetProcessesByName(app.ProcessName);
            Debug.WriteLine("kill " + app.ProcessName + processes.Length);

            var confirmResult = MessageBox.Show("Are you sure you want to stop " + app.Name + "\nand restart from: " + app.RestartPath,
                "Restart App",
                MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {

                try
                {
                    if (processes.Length > 0)
                    {
                        foreach (Process process in processes)
                        {
                            process.Kill(); // Kill the process
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error stopping the application: " + ex.Message);
                }
            }

            Thread.Sleep(2000);
            try
            {
                Process[] processes2 = Process.GetProcessesByName(app.ProcessName);
                if (processes2.Length == 0)
                {
                    System.Diagnostics.Process.Start(app.RestartPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting the application: " + ex.Message);
            }
        }
    }
}