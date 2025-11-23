using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class Form1
    {
        private void ShowPcsView()
        {
            _currentView = ViewMode.Pcs;
            btnAddApp.Text = "Add New PC";
            HighlightNavButton(btnNavPcs);
            AppFlowLayoutPanel.Controls.Clear();
            RenderPcButtons();
        }

        private void RenderPcButtons()
        {
            AppFlowLayoutPanel.Controls.Clear();

            // "All PCs" button
            if (_pcs.Count > 0)
            {
                var allBtn = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = "[All PCs]",
                    BackColor = Color.FromArgb(128, 64, 64),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };
                allBtn.FlatAppearance.BorderSize = 0;

                // Left-click: Shutdown all
                allBtn.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        "Are you sure you want to SHUT DOWN all configured PCs?",
                        "Confirm Shutdown All",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes) return;

                    foreach (var pc in _pcs)
                    {
                        await PcPowerController.ShutdownAsync(pc, AddToLog);
                    }
                };

                // Right-click: context menu
                allBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var menu = new ContextMenuStrip();
                        menu.Items.Add("Shutdown All").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                "Are you sure you want to SHUT DOWN all configured PCs?",
                                "Confirm Shutdown All",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (confirm != DialogResult.Yes) return;

                            foreach (var pc in _pcs)
                            {
                                await PcPowerController.ShutdownAsync(pc, AddToLog);
                            }
                        };

                        menu.Items.Add("Restart All").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                "Are you sure you want to RESTART all configured PCs?",
                                "Confirm Restart All",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (confirm != DialogResult.Yes) return;

                            foreach (var pc in _pcs)
                            {
                                await PcPowerController.RestartAsync(pc, AddToLog);
                            }
                        };

                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(allBtn);
            }

            // Individual PCs
            for (int i = 0; i < _pcs.Count; i++)
            {
                var pc = _pcs[i];
                int index = i;

                var pcBtn = new Button
                {
                    Width = 163,
                    Height = 45,
                    Text = pc.Name,
                    BackColor = Color.FromArgb(70, 90, 160),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = SystemColors.ButtonFace
                };
                pcBtn.FlatAppearance.BorderSize = 0;

                // Left-click: Shutdown
                pcBtn.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        $"Shut down {pc.Name} ({pc.IP})?",
                        "Confirm Shutdown",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;

                    await PcPowerController.ShutdownAsync(pc, AddToLog);
                };

                // Right-click: context menu
                pcBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var menu = new ContextMenuStrip();

                        menu.Items.Add("Shutdown").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                $"Shut down {pc.Name} ({pc.IP})?",
                                "Confirm Shutdown",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes) return;

                            await PcPowerController.ShutdownAsync(pc, AddToLog);
                        };

                        menu.Items.Add("Restart").Click += async (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                $"Restart {pc.Name} ({pc.IP})?",
                                "Confirm Restart",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes) return;

                            await PcPowerController.RestartAsync(pc, AddToLog);
                        };

                        menu.Items.Add("Edit").Click += (ms, me) =>
                        {
                            using var dlg = new AddPcForm(pc);
                            if (dlg.ShowDialog(this) == DialogResult.OK)
                            {
                                _pcs[index] = dlg.PcData;
                                SaveApplicationsToXml();
                                RenderPcButtons();
                            }
                        };

                        menu.Items.Add("Delete").Click += (ms, me) =>
                        {
                            var confirm = MessageBox.Show(
                                $"Delete PC '{pc.Name}' from configuration?",
                                "Confirm Delete PC",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes) return;

                            _pcs.RemoveAt(index);
                            SaveApplicationsToXml();
                            RenderPcButtons();
                        };

                        menu.Show(Cursor.Position);
                    }
                };

                AppFlowLayoutPanel.Controls.Add(pcBtn);
            }
        }
    }
}
