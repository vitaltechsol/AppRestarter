using System;
using System.Net;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AppRestarter
{
    public partial class AddPcForm : Form
    {
        public PcInfo PcData { get; private set; }
        private readonly bool _isEdit;

        public AddPcForm()
        {
            InitializeComponent();
            _isEdit = false;
            PcData = new PcInfo();
        }

        public AddPcForm(PcInfo existing)
        {
            InitializeComponent();
            _isEdit = true;
            PcData = new PcInfo
            {
                Name = existing?.Name ?? "",
                IP = existing?.IP ?? ""
            };

            txtName.Text = PcData.Name;
            txtIp.Text = PcData.IP;
            lblTitle.Text = "Edit Remote PC";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var name = txtName.Text?.Trim() ?? "";
            var ip = txtIp.Text?.Trim() ?? "";

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
            PcData.IP = ip;

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
