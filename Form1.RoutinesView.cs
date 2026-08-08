using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppRestarter.Models;

namespace AppRestarter
{
    public partial class Form1
    {
        private void ShowRoutinesView()
        {
            _currentView = ViewMode.Routines;
            btnAddApp.Text = "Add New Routine";
            HighlightNavButton(btnNavRoutines);

            RenderRoutines();
        }

        private void RenderRoutines()
        {
            AppFlowLayoutPanel.SuspendLayout();
            AppFlowLayoutPanel.Controls.Clear();
            AppFlowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
            AppFlowLayoutPanel.WrapContents = true;
            AppFlowLayoutPanel.AutoScroll = true;

            var hasLocal = _routines != null && _routines.Count > 0;
            var hasRemote = _remoteRoutines != null && _remoteRoutines.Count > 0;

            if (!hasLocal && !hasRemote)
            {
                var empty = new Label
                {
                    AutoSize = true,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Text = "No routines configured yet. Click 'Add New Routine' to create local routines or reference routines from other PCs.",
                    Margin = new Padding(6),
                    MaximumSize = new Size(AppFlowLayoutPanel.Width - 30, 0)
                };
                AppFlowLayoutPanel.Controls.Add(empty);
                AppFlowLayoutPanel.ResumeLayout();
                return;
            }

            // Render local routines
            var nameFont = FontManager.GetFont(1.0f, FontStyle.Regular);
            var metaFont = FontManager.GetFont(0.75f, FontStyle.Regular);

            if (hasLocal)
            {
                var localHeader = new Label
                {
                    Text = "Local Routines",
                    AutoSize = false,
                    Width = AppFlowLayoutPanel.Width - 30,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = FontManager.GetFont(0.85f, FontStyle.Bold),
                    Margin = new Padding(6, 12, 6, 6)
                };
                AppFlowLayoutPanel.Controls.Add(localHeader);

                foreach (var routine in _routines)
                {
                    int index = _routines.IndexOf(routine);
                    var card = CreateRoutineCard(routine, index, isRemote: false, nameFont, metaFont);
                    AppFlowLayoutPanel.Controls.Add(card);
                }
            }

            // Render remote routines
            if (hasRemote)
            {
                var remoteHeader = new Label
                {
                    Text = "Remote Routines",
                    AutoSize = false,
                    Width = AppFlowLayoutPanel.Width - 30,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = FontManager.GetFont(0.85f, FontStyle.Bold),
                    Margin = new Padding(6, 12, 6, 6)
                };
                AppFlowLayoutPanel.Controls.Add(remoteHeader);

                foreach (var remoteRef in _remoteRoutines)
                {
                    int index = _remoteRoutines.IndexOf(remoteRef);
                    var card = CreateRemoteRoutineCard(remoteRef, index, nameFont, metaFont);
                    AppFlowLayoutPanel.Controls.Add(card);
                }
            }

            AppFlowLayoutPanel.ResumeLayout();
        }

        private Panel CreateRoutineCard(Routine routine, int index, bool isRemote, Font nameFont, Font metaFont)
        {
            var routineCard = new Panel();
            StyleRoutineCardPanel(routineCard);

            // Routine Name
            var lblName = new Label
            {
                AutoSize = false,
                Text = string.IsNullOrWhiteSpace(routine.Name) ? "(no name)" : routine.Name,
                Font = nameFont,
                ForeColor = Color.FromArgb(243, 244, 246),
                Location = new Point(6, 4),
                Size = new Size(routineCard.Width - 30, nameFont.Height + 2),
                AutoEllipsis = true
            };

            // Step count metadata
            var stepCount = routine.Steps?.Count ?? 0;
            var lblMeta = new Label
            {
                AutoSize = true,
                Text = $"{stepCount} step(s)",
                Font = metaFont,
                ForeColor = Color.FromArgb(148, 163, 184),
                MaximumSize = new Size(routineCard.Width - 30, 0),
                AutoEllipsis = true
            };

            lblMeta.Location = new Point(8, routineCard.Height - lblMeta.PreferredHeight - 8);

            routineCard.Controls.Add(lblMeta);
            routineCard.Controls.Add(lblName);

            // Hover effect
            AttachCardHover(routineCard, lblName, lblMeta);

            // Context menu
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Execute").Click += async (s, e) => await ExecuteRoutineAsync(routine);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add("Edit").Click += (s, e) => EditRoutine(index);
            ctxMenu.Items.Add("Delete").Click += (s, e) => DeleteRoutine(index);

            routineCard.ContextMenuStrip = ctxMenu;
            lblName.ContextMenuStrip = ctxMenu;
            lblMeta.ContextMenuStrip = ctxMenu;

            // Left-click to execute
            void AttachClick(Control c)
            {
                c.Click += async (s, e) => await ExecuteRoutineAsync(routine);
            }

            AttachClick(routineCard);
            AttachClick(lblName);
            AttachClick(lblMeta);

            return routineCard;
        }

        private Panel CreateRemoteRoutineCard(RemoteRoutineReference remoteRef, int index, Font nameFont, Font metaFont)
        {
            var routineCard = new Panel();
            StyleRoutineCardPanel(routineCard);
            routineCard.BackColor = Color.FromArgb(30, 41, 59); // Slightly different color for remote

            // Routine Name with remote indicator
            var lblName = new Label
            {
                AutoSize = false,
                Text = $"{remoteRef.CachedName ?? "(unnamed)"}",
                Font = nameFont,
                Location = new Point(6, 4),
                Size = new Size(routineCard.Width - 30, nameFont.Height + 2),
                AutoEllipsis = true
            };

            // Host metadata
            var pcName = _pcs.FirstOrDefault(p => p.IP == remoteRef.RemoteHost)?.Name ?? remoteRef.RemoteHost;
            var lblMeta = new Label
            {
                AutoSize = true,
                Text = $"from {pcName}",
                Font = metaFont,
                ForeColor = Color.FromArgb(148, 163, 184),
                MaximumSize = new Size(routineCard.Width - 30, 0),
                AutoEllipsis = true
            };

            lblMeta.Location = new Point(8, routineCard.Height - lblMeta.PreferredHeight - 8);

            routineCard.Controls.Add(lblMeta);
            routineCard.Controls.Add(lblName);

            // Hover effect
            AttachCardHover(routineCard, lblName, lblMeta);

            // Context menu
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Execute").Click += async (s, e) => await ExecuteRemoteRoutineAsync(remoteRef);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add("Edit").Click += (s, e) => EditRemoteRoutine(index);
            ctxMenu.Items.Add("Refresh Info").Click += async (s, e) => await RefreshRemoteRoutineInfo(remoteRef);
            ctxMenu.Items.Add("Delete Reference").Click += (s, e) => DeleteRemoteRoutine(index);

            routineCard.ContextMenuStrip = ctxMenu;
            lblName.ContextMenuStrip = ctxMenu;
            lblMeta.ContextMenuStrip = ctxMenu;

            // Left-click to execute
            void AttachClick(Control c)
            {
                c.Click += async (s, e) => await ExecuteRemoteRoutineAsync(remoteRef);
            }

            AttachClick(routineCard);
            AttachClick(lblName);
            AttachClick(lblMeta);

            return routineCard;
        }

        private void StyleRoutineCardPanel(Panel panel)
        {
            // Scale card size based on font size
            float fontScale = FontManager.BaseFontSize / 9.0f;
            int scaledWidth = (int)(200 * Math.Max(1.0f, fontScale));
            int scaledHeight = (int)(48 * Math.Max(1.0f, fontScale));

            panel.BackColor = CardNormalBack;
            panel.ForeColor = Color.FromArgb(229, 231, 235);
            panel.Padding = new Padding(6, 3, 6, 3);
            panel.Margin = new Padding(6);
            panel.Width = scaledWidth;
            panel.Height = scaledHeight;
            panel.Cursor = Cursors.Hand;
            panel.BorderStyle = BorderStyle.FixedSingle;
        }

        private void EditRoutine(int index)
        {
            if (index < 0 || index >= _routines.Count)
                return;

            var existing = _routines[index];
            using var editForm = new AddRoutineForm(existing, _apps, _pcs, _groups, _settings.WebPort);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                if (editForm.DeleteRequested)
                {
                    _routines.RemoveAt(index);
                }
                else
                {
                    _routines[index] = editForm.RoutineData;
                }

                SaveRoutines();
                RenderRoutines();
            }
        }

        private void DeleteRoutine(int index)
        {
            if (index < 0 || index >= _routines.Count)
                return;

            var routine = _routines[index];
            var result = MessageBox.Show(
                $"Delete routine '{routine.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _routines.RemoveAt(index);
                SaveRoutines();
                RenderRoutines();
            }
        }

        private async Task ExecuteRoutineAsync(Routine routine)
        {
            if (routine == null)
                return;

            //var result = MessageBox.Show(
            //    $"Execute routine '{routine.Name}'?\n\nThis will run all {routine.Steps?.Count ?? 0} step(s) in order.",
            //    "Execute Routine",
            //    MessageBoxButtons.YesNo,
            //    MessageBoxIcon.Question);

            //if (result != DialogResult.Yes)
            //    return;

            AddToLog($"Executing routine: {routine.Name}");

            try
            {
                var executor = new RoutineExecutor(_apps, AddToLog);
                var cts = new CancellationTokenSource();

                // Execute routine in background
                await Task.Run(async () =>
                {
                    await executor.ExecuteRoutineAsync(routine, cts.Token);
                });

                AddToLog($"Routine '{routine.Name}' completed.");
            }
            catch (Exception ex)
            {
                AddToLog($"Error executing routine '{routine.Name}': {ex.Message}");
                MessageBox.Show(
                    $"Error executing routine: {ex.Message}",
                    "Routine Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadRoutines()
        {
            try
            {
                var loadedRoutines = RoutineManager.LoadRoutines();
                _routines.Clear();
                _routines.AddRange(loadedRoutines);
                AddToLog($"Loaded {_routines.Count} routine(s).");
            }
            catch (Exception ex)
            {
                AddToLog($"Error loading routines: {ex.Message}");
                _routines.Clear();
            }
        }

        private void SaveRoutines()
        {
            try
            {
                RoutineManager.SaveRoutines(_routines);
                AddToLog($"Saved {_routines.Count} routine(s).");
            }
            catch (Exception ex)
            {
                AddToLog($"Error saving routines: {ex.Message}");
                MessageBox.Show(
                    $"Error saving routines: {ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadRemoteRoutines()
        {
            try
            {
                var loadedRemoteRoutines = RemoteRoutineManager.LoadRemoteRoutines();
                _remoteRoutines.Clear();
                _remoteRoutines.AddRange(loadedRemoteRoutines);
                AddToLog($"Loaded {_remoteRoutines.Count} remote routine(s).");
            }
            catch (Exception ex)
            {
                AddToLog($"Error loading remote routines: {ex.Message}");
                _remoteRoutines.Clear();
            }
        }

        private void SaveRemoteRoutines()
        {
            try
            {
                RemoteRoutineManager.SaveRemoteRoutines(_remoteRoutines);
                AddToLog($"Saved {_remoteRoutines.Count} remote routine(s).");
            }
            catch (Exception ex)
            {
                AddToLog($"Error saving remote routines: {ex.Message}");
            }
        }

        private async Task ExecuteRemoteRoutineAsync(RemoteRoutineReference remoteRef)
        {
            var pcName = _pcs.FirstOrDefault(p => p.IP == remoteRef.RemoteHost)?.Name ?? remoteRef.RemoteHost;
            AddToLog($"Executing remote routine: {remoteRef.CachedName} on {pcName}");

            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                var executeUrl = $"http://{remoteRef.RemoteHost}:{remoteRef.RemotePort}/routines/execute";
                var requestBody = new { Id = remoteRef.RemoteRoutineId };
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(executeUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to execute remote routine: {response.StatusCode}");
                }

                var result = await response.Content.ReadAsStringAsync();
                AddToLog($"Remote routine '{remoteRef.CachedName}' execution started: {result}");
            }
            catch (Exception ex)
            {
                AddToLog($"Error executing remote routine: {ex.Message}");
                MessageBox.Show(
                    $"Failed to execute remote routine:\n\n{ex.Message}",
                    "Execution Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task RefreshRemoteRoutineInfo(RemoteRoutineReference remoteRef)
        {
            var pcName = _pcs.FirstOrDefault(p => p.IP == remoteRef.RemoteHost)?.Name ?? remoteRef.RemoteHost;

            try
            {
                AddToLog($"Refreshing info for remote routine from {pcName}...");


                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var exportUrl = $"http://{remoteRef.RemoteHost}:{remoteRef.RemotePort}/routines/export/{remoteRef.RemoteRoutineId}";
                var response = await httpClient.GetAsync(exportUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch routine info: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var routine = System.Text.Json.JsonSerializer.Deserialize<Routine>(json);

                if (routine != null)
                {
                    remoteRef.CachedName = routine.Name;
                    remoteRef.LastUpdated = DateTime.Now;
                    SaveRemoteRoutines();
                    RenderRoutines();

                    AddToLog($"Refreshed remote routine info: {routine.Name}");
                    MessageBox.Show(
                        $"Routine info refreshed: {routine.Name}",
                        "Refresh Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                AddToLog($"Error refreshing remote routine info: {ex.Message}");
                MessageBox.Show(
                    $"Failed to refresh routine info:\n\n{ex.Message}",
                    "Refresh Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void EditRemoteRoutine(int index)
        {
            if (index < 0 || index >= _remoteRoutines.Count)
                return;

            var existing = _remoteRoutines[index];
            using var editForm = new AddRoutineForm(existing, _apps, _pcs, _groups, _settings.WebPort);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                if (editForm.DeleteRequested)
                {
                    _remoteRoutines.RemoveAt(index);
                }
                else
                {
                    _remoteRoutines[index] = editForm.RemoteRoutineData;
                }

                SaveRemoteRoutines();
                RenderRoutines();
            }
        }

        private void DeleteRemoteRoutine(int index)
        {
            if (index < 0 || index >= _remoteRoutines.Count)
                return;

            var remoteRef = _remoteRoutines[index];
            var result = MessageBox.Show(
                $"Remove reference to remote routine '{remoteRef.CachedName}' from {remoteRef.RemoteHost}?\n\nThis will not delete the routine on the remote PC.",
                "Delete Remote Reference",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _remoteRoutines.RemoveAt(index);
                SaveRemoteRoutines();
                RenderRoutines();
                AddToLog($"Removed remote routine reference: {remoteRef.CachedName}");
            }
        }
    }
}
