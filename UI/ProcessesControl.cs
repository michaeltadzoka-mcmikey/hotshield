using System;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Hotshield.Core;
using Hotshield.Data;

namespace Hotshield.UI
{
    public class ProcessesControl : UserControl
    {
        private DataGridView _grid;
        private AppRuleRepo _ruleRepo = new AppRuleRepo();

        public ProcessesControl()
        {
            InitializeComponent();
            LoadProcesses();
        }

        private void InitializeComponent()
        {
            var header = new Label
            {
                Text = "📡 Network Activity",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };
            Controls.Add(header);

            var info = new Label
            {
                Text = "These processes are currently using the internet. Select one to add a firewall rule.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.DimGray,
                Location = new Point(12, 35),
                AutoSize = true
            };
            Controls.Add(info);

            _grid = new DataGridView
            {
                Location = new Point(12, 60),
                Size = new Size(ClientSize.Width - 24, 350),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };
            _grid.Columns.Add("ProcessName", "Process");
            _grid.Columns.Add("ExePath", "Path");
            _grid.Columns.Add("Status", "Status");
            Controls.Add(_grid);

            var btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(12, 420),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.System
            };
            btnRefresh.Click += (s, e) => LoadProcesses();
            Controls.Add(btnRefresh);

            var btnBlock = new Button
            {
                Text = "🔴 Block Selected",
                Location = new Point(120, 420),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.System,
                ForeColor = Color.Red
            };
            btnBlock.Click += BtnBlock_Click;
            Controls.Add(btnBlock);

            var btnAllow = new Button
            {
                Text = "🟢 Allow Selected",
                Location = new Point(260, 420),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.System,
                ForeColor = Color.FromArgb(16, 185, 129)
            };
            btnAllow.Click += BtnAllow_Click;
            Controls.Add(btnAllow);
        }

        private void LoadProcesses()
        {
            _grid.Rows.Clear();
            var processes = ProcessMonitor.GetNetworkProcesses();
            var allRules = _ruleRepo.GetAll().Where(r => r.IsActive).ToList();

            foreach (var proc in processes)
            {
                string status = "Not in rules";
                var match = allRules.FirstOrDefault(r =>
                    r.ExePath.Equals(proc.ExePath, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    status = match.Action == "block" ? "Blocked" : "Allowed";

                var row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell { Value = proc.ProcessName });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = proc.ExePath });
                var statusCell = new DataGridViewTextBoxCell { Value = status };
                // Color the status cell
                row.Cells.Add(statusCell);
                _grid.Rows.Add(row);
            }
        }

        private void BtnBlock_Click(object sender, EventArgs e) => AddRule("block");
        private void BtnAllow_Click(object sender, EventArgs e) => AddRule("allow");

        private void AddRule(string action)
        {
            if (_grid.SelectedRows.Count == 0) return;
            string exePath = _grid.SelectedRows[0].Cells[1].Value.ToString()!;
            string name = _grid.SelectedRows[0].Cells[0].Value.ToString()!;
            var rule = new Models.AppRule
            {
                AppName = name,
                ExePath = exePath,
                Action = action,
                RuleGroup = "Custom"
            };
            using var edit = new RuleEditForm(rule);
            if (edit.ShowDialog() == DialogResult.OK)
            {
                _ruleRepo.Insert(edit.Rule);
                LoadProcesses();
            }
        }
    }
}
