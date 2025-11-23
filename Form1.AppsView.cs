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
        // ---------- LOAD APPS / GROUPS ----------

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

        // ---------- VIEW: APPS ----------

        private void ShowAppsView()
        {
            _currentView = ViewMode.Apps;
            btnAddApp.Text = "Add New App";
            HighlightNavButton(btnNavApps);
            AppFlowLayoutPanel.Controls.Clear();
            RenderGroupsAndApps();
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
                            HandleRemoteClientAppClick(app, start: true, stop: true, skipConfirm: false);
                        else
                            await HandleAppButtonClickAsync(app, start: true, stop: true, skipConfirm: false);
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
                                    HandleRemoteClientAppClick(app, start: false, stop: true, skipConfirm: true);
                                else
                                    await HandleAppButtonClickAsync(app, start: false, stop: true, skipConfirm: true);
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
                        HandleRemoteClientAppClick(app, start: true, stop: true, skipConfirm: false);
                    else
                        _ = HandleAppButtonClickAsync(app, start: true, stop: true, skipConfirm: false);
                };

                appButton.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.Items.Add("Stop").Click += (ms, me) => StopApp(index);
                        menu.Items.Add(new ToolStripSeparator());
                        menu.Items.Add("Edit").Click += (ms, me) => EditApp(index);
                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(appButton);
            }
        }

        // ---------- APPS: EDIT / STOP / AUTO-START / GROUPS ----------

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
                HandleRemoteClientAppClick(existing, start: false, stop: true, skipConfirm: false);
            else
                _ = HandleAppButtonClickAsync(existing, start: false, stop: true, skipConfirm: false);
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

        // ---------- APP (REMOTE) START/STOP LOGIC ----------

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
    }
}
