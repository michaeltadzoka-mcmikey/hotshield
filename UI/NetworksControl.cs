using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Hotshield.Data;
using Hotshield.Models;

namespace Hotshield.UI
{
    public class NetworksControl : UserControl
    {
        private DataGridView _grid;
        private MeteredNetworkRepo _repo = new MeteredNetworkRepo();
        private Label _emptyLabel;

        public NetworksControl()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            var header = new Label
            {
                Text = "🌐 Which networks do you want to protect?",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(12, 10),
                Size = new Size(600, 20)
            };
            Controls.Add(header);

            var subHeader = new Label
            {
                Text = "Add your phone hotspot or any limited data WiFi. When you connect to these networks,\n" +
                       "Hotshield will automatically block the apps you don't want using your data.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Location = new Point(14, 32),
                Size = new Size(700, 40)
            };
            Controls.Add(subHeader);

            _grid = new DataGridView
            {
                Location = new Point(12, 75),
                Size = new Size(ClientSize.Width - 24, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.Fixed3D,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            _grid.Columns.Add("Ssid", "Network Name");
            _grid.Columns.Add("Preset", "Protection Mode");
            _grid.Columns.Add("Status", "Status");
            _grid.Columns[0].Width = 250;
            _grid.Columns[1].Width = 180;

            _emptyLabel = new Label
            {
                Text = "No networks added yet.\n\n" +
                       "👉 Click \"Add Current Network\" to instantly protect the WiFi you're on now.\n" +
                       "   This will block data-wasting apps like Windows Update and OneDrive.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                Location = new Point(12, 85),
                AutoSize = true,
                Visible = false
            };
            Controls.Add(_emptyLabel);

            var btnAddCurrent = new Button
            {
                Text = "📱  Add Current Network",
                Location = new Point(12, 370),
                Size = new Size(200, 36),
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddCurrent.FlatAppearance.BorderSize = 0;
            btnAddCurrent.Click += BtnAddCurrent_Click;
            Controls.Add(btnAddCurrent);

            var tipLabel = new Label
            {
                Text = "This will add the WiFi you are currently connected to.",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Location = new Point(12, 410),
                AutoSize = true
            };
            Controls.Add(tipLabel);

            var btnAddManual = new Button
            {
                Text = "➕  Add Manually",
                Location = new Point(230, 370),
                Size = new Size(140, 36),
                FlatStyle = FlatStyle.System
            };
            btnAddManual.Click += BtnAddManual_Click;
            Controls.Add(btnAddManual);

            var btnAssignPreset = new Button
            {
                Text = "🔄  Change Mode",
                Location = new Point(385, 370),
                Size = new Size(130, 36),
                FlatStyle = FlatStyle.System
            };
            btnAssignPreset.Click += BtnAssignPreset_Click;
            Controls.Add(btnAssignPreset);

            var btnDelete = new Button
            {
                Text = "🗑️  Remove",
                Location = new Point(530, 370),
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.System,
                ForeColor = Color.Red
            };
            btnDelete.Click += BtnDelete_Click;
            Controls.Add(btnDelete);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _grid.Size = new Size(ClientSize.Width - 24, 280);
        }

        private void LoadData()
        {
            _grid.Rows.Clear();
            var networks = _repo.GetAll();
            if (networks.Count == 0)
            {
                _emptyLabel.Visible = true;
                _grid.Visible = false;
                return;
            }
            _emptyLabel.Visible = false;
            _grid.Visible = true;
            var presets = PresetRepo.GetAll();
            foreach (var net in networks)
            {
                string presetName = net.PresetId != null
                    ? presets.FirstOrDefault(p => p.Id == net.PresetId)?.Name ?? "Unknown"
                    : "(none)";
                string status = net.PresetId != null ? "✅ Active" : "⚠️ No protection mode";
                _grid.Rows.Add(net.Ssid, presetName, status);
            }
        }

        private void BtnAddCurrent_Click(object sender, EventArgs e)
        {
            string ssid = Helpers.WlanHelper.GetCurrentSsid();
            bool isUsb = false;

            if (string.IsNullOrEmpty(ssid))
            {
                ssid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";
                isUsb = !string.IsNullOrEmpty(ssid);
            }

            if (string.IsNullOrEmpty(ssid))
            {
                MessageBox.Show(
                    "No WiFi or USB Tethering detected.\n\n" +
                    "Connect to your phone hotspot or a WiFi network first, then try again.",
                    "No Network Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_repo.FindBySsid(ssid) != null)
            {
                MessageBox.Show($"\"{ssid}\" is already in your protected list.", "Already Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var preset = PickPreset();
            if (preset == null) return;
            _repo.Add(new MeteredNetwork { Ssid = ssid, PresetId = preset.Id });
            LoadData();

            string type = isUsb ? "USB Tether" : "WiFi";
            MessageBox.Show($"✅ {type} \"{ssid}\" is now protected!\n\n" +
                           $"Using preset: {preset.Name}\n" +
                           "Hotshield will automatically block selected apps when you connect to this network.",
                           "Network Protected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAddManual_Click(object sender, EventArgs e)
        {
            string ssid = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the exact WiFi name (SSID) of the network:", "Add Network Manually", "");
            if (string.IsNullOrWhiteSpace(ssid)) return;
            if (_repo.FindBySsid(ssid) != null)
            {
                MessageBox.Show("\"" + ssid + "\" is already in your protected list.", "Already Added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var preset = PickPreset();
            if (preset == null) return;
            _repo.Add(new MeteredNetwork { Ssid = ssid, PresetId = preset.Id });
            LoadData();
        }

        private void BtnAssignPreset_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a network from the list first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string ssid = _grid.SelectedRows[0].Cells[0].Value.ToString();
            var net = _repo.FindBySsid(ssid);
            if (net == null) return;
            var preset = PickPreset();
            if (preset != null)
            {
                _repo.UpdatePreset(net.Id, preset.Id);
                LoadData();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a network from the list first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string ssid = _grid.SelectedRows[0].Cells[0].Value.ToString();
            if (MessageBox.Show($"Remove \"{ssid}\" from your protected list?\n\n" +
                               "Hotshield will no longer block apps when you connect to this network.",
                               "Remove Network?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var net = _repo.FindBySsid(ssid);
                if (net != null)
                {
                    _repo.Delete(net.Id);
                    LoadData();
                }
            }
        }

        private Preset? PickPreset()
        {
            var presets = PresetRepo.GetAll();
            if (presets.Count == 0)
            {
                MessageBox.Show("No protection modes found. Please restart Hotshield.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            using var picker = new Form
            {
                Text = "Choose Protection Mode",
                Size = new Size(380, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var descLabel = new Label
            {
                Text = "How do you want to protect this network?",
                Location = new Point(20, 12),
                Size = new Size(320, 20),
                Font = new Font("Segoe UI", 9F)
            };
            var combo = new ComboBox
            {
                Location = new Point(20, 40),
                Size = new Size(320, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            combo.DataSource = presets;
            combo.DisplayMember = "Name";
            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(140, 80),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.System
            };
            picker.Controls.Add(descLabel);
            picker.Controls.Add(combo);
            picker.Controls.Add(btnOk);
            return picker.ShowDialog() == DialogResult.OK ? combo.SelectedItem as Preset : null;
        }
    }
}