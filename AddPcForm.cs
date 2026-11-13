using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRestarter
{
    public partial class AddPcForm : Form
    {
        public PcInfo PcData { get; private set; }
        private readonly bool _isEdit;

        // Simple container for items shown in the discovery listbox.
        private sealed class DiscoveredPcItem
        {
            public string Name { get; }
            public string IP   { get; }
            public DiscoveredPcItem(string name, string ip) { Name = name; IP = ip; }
            public override string ToString() => $"{Name}  ({IP})";
        }

        public AddPcForm()
        {
            InitializeComponent();
            _isEdit = false;
            PcData = new PcInfo();
            _ = StartDiscoveryAsync();
        }

        public AddPcForm(PcInfo existing)
        {
            InitializeComponent();
            _isEdit = true;
            PcData = new PcInfo
            {
                Name = existing?.Name ?? "",
                IP   = existing?.IP   ?? ""
            };

            txtName.Text  = PcData.Name;
            txtIp.Text    = PcData.IP;
            lblTitle.Text = "Edit Remote PC";

            // Hide discovery section
            lblDiscoveryStatus.Visible    = false;
            btnRescan.Visible             = false;
            listDiscovered.Visible        = false;
            pnlDiscoverySeparator.Visible = false;
            lblManualHint.Visible         = false;

            // Restore compact positions matching original form layout
            lblName.Location   = new System.Drawing.Point(18, 46);
            txtName.Location   = new System.Drawing.Point(18, 68);
            lblIp.Location     = new System.Drawing.Point(18, 108);
            txtIp.Location     = new System.Drawing.Point(18, 130);
            btnSave.Location   = new System.Drawing.Point(198, 170);
            btnCancel.Location = new System.Drawing.Point(286, 170);
            ClientSize = new System.Drawing.Size(384, 221);
        }

        private async Task StartDiscoveryAsync()
        {
            try
            {
                lblDiscoveryStatus.Text    = "Scanning for PCs on network...";
                listDiscovered.Enabled     = false;

                var peers = await PcDiscoveryService.DiscoverPeersAsync(timeoutMs: 2500);

                if (IsDisposed) return;

                listDiscovered.Items.Clear();

                if (peers.Count == 0)
                {
                    lblDiscoveryStatus.Text = "No PCs found on the network.";
                    listDiscovered.Enabled = false;
                }
                else
                {
                    lblDiscoveryStatus.Text = $"{peers.Count} PC(s) found — click one to select:";
                    foreach (var (name, ip) in peers)
                        listDiscovered.Items.Add(new DiscoveredPcItem(name, ip));
                    listDiscovered.Enabled = true;
                }
            }
            catch (ObjectDisposedException) { }
            catch
            {
                if (!IsDisposed)
                    lblDiscoveryStatus.Text = "Discovery unavailable.";
            }
        }

        private void listDiscovered_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listDiscovered.SelectedItem is DiscoveredPcItem item)
            {
                txtName.Text = item.Name;
                txtIp.Text   = item.IP;
            }
        }

        private void btnRescan_Click(object sender, EventArgs e)
        {
            listDiscovered.Items.Clear();
            listDiscovered.Enabled = false;
            _ = StartDiscoveryAsync();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var name = txtName.Text?.Trim() ?? "";
            var ip   = txtIp.Text?.Trim()   ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a PC Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ip) || !IsValidIPv4(ip))
            {
                MessageBox.Show("Please enter a valid IPv4 address (e.g., 192.168.1.10).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtIp.Focus();
                return;
            }

            PcData.Name = name;
            PcData.IP   = ip;

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool IsValidIPv4(string ip)
        {
            return IPAddress.TryParse(ip, out var addr) && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}

