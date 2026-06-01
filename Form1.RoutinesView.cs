using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

            if (_routines == null || _routines.Count == 0)
            {
                var empty = new Label
                {
                    AutoSize = true,
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Text = "No routines configured yet. Click 'Add New Routine' to create one."
                };
                AppFlowLayoutPanel.Controls.Add(empty);
                AppFlowLayoutPanel.ResumeLayout();
                return;
            }

            // Render each routine as a card
            var nameFont = FontManager.GetFont(1.0f, FontStyle.Regular);
            var metaFont = FontManager.GetFont(0.75f, FontStyle.Regular);

            foreach (var routine in _routines)
            {
                int index = _routines.IndexOf(routine);

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

                AppFlowLayoutPanel.Controls.Add(routineCard);
            }

            AppFlowLayoutPanel.ResumeLayout();
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
            using var editForm = new AddRoutineForm(existing, _apps, _pcs, _groups);
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

            var result = MessageBox.Show(
                $"Execute routine '{routine.Name}'?\n\nThis will run all {routine.Steps?.Count ?? 0} step(s) in order.",
                "Execute Routine",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            AddToLog($"Executing routine: {routine.Name}");

            try
            {
                var executor = new RoutineExecutor(_apps);
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
                _routines = RoutineManager.LoadRoutines();
                AddToLog($"Loaded {_routines.Count} routine(s).");
            }
            catch (Exception ex)
            {
                AddToLog($"Error loading routines: {ex.Message}");
                _routines = new List<Routine>();
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
    }
}
