using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Hotshield.Data;
using Hotshield.Models;

namespace Hotshield.UI
{
    public class RulesControl : UserControl
    {
        private DataGridView _grid;
        private AppRuleRepo _repo = new AppRuleRepo();
        private ComboBox _filterGroup;

        public RulesControl()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            var header = new Label
            {
                Text = "🔧 Firewall Rules",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };
            Controls.Add(header);

            var filterLabel = new Label { Text = "Filter by group:", Location = new Point(12, 40), AutoSize = true };
            Controls.Add(filterLabel);
            _filterGroup = new ComboBox
            {
                Location = new Point(120, 38),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _filterGroup.Items.Add("All");
            _filterGroup.Items.AddRange(new[] { "Windows System", "Microsoft Apps", "Browser", "Gaming", "Communication", "Custom" });
            _filterGroup.SelectedIndex = 0;
            _filterGroup.SelectedIndexChanged += (s, e) => LoadData();
            Controls.Add(_filterGroup);

            _grid = new DataGridView
            {
                Location = new Point(12, 70),
                Size = new Size(ClientSize.Width - 24, 350),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };
            _grid.Columns.Add("AppName", "Application");
            _grid.Columns.Add("Action", "Action");
            _grid.Columns.Add("ServiceName", "Service");
            _grid.Columns.Add("Group", "Group");
            _grid.Columns.Add("Active", "Active");
            Controls.Add(_grid);

            var toolBar = new FlowLayoutPanel
            {
                Location = new Point(12, 430),
                Size = new Size(ClientSize.Width - 24, 36),
                FlowDirection = FlowDirection.LeftToRight
            };
            var btnAdd = new Button { Text = "➕ Add Rule", Size = new Size(100, 30), FlatStyle = FlatStyle.System };
            btnAdd.Click += BtnAdd_Click;
            toolBar.Controls.Add(btnAdd);
            var btnEdit = new Button { Text = "✏️ Edit", Size = new Size(80, 30), FlatStyle = FlatStyle.System };
            btnEdit.Click += BtnEdit_Click;
            toolBar.Controls.Add(btnEdit);
            var btnToggle = new Button { Text = "Toggle", Size = new Size(80, 30), FlatStyle = FlatStyle.System };
            btnToggle.Click += BtnToggleActive_Click;
            toolBar.Controls.Add(btnToggle);
            var btnDelete = new Button { Text = "🗑️ Delete", Size = new Size(80, 30), FlatStyle = FlatStyle.System, ForeColor = Color.Red };
            btnDelete.Click += BtnDelete_Click;
            toolBar.Controls.Add(btnDelete);
            var btnFromProc = new Button { Text = "From Processes", Size = new Size(110, 30), FlatStyle = FlatStyle.System };
            btnFromProc.Click += BtnAddFromProcesses_Click;
            toolBar.Controls.Add(btnFromProc);
            Controls.Add(toolBar);
        }

        private void LoadData()
        {
            _grid.Rows.Clear();
            List<AppRule> rules = _repo.GetAll();
            string filter = _filterGroup.SelectedItem?.ToString();
            if (filter != null && filter != "All")
                rules = rules.Where(r => r.RuleGroup == filter).ToList();

            foreach (var r in rules)
            {
                string actionText = r.Action == "block" ? "🔴 Blocked" : "🟢 Allowed";
                string activeText = r.IsActive ? "Yes" : "No";
                _grid.Rows.Add(r.AppName, actionText, r.ServiceName ?? "", r.RuleGroup, activeText);
            }
        }

        private AppRule? SelectedRule()
        {
            if (_grid.SelectedRows.Count == 0) return null;
            // Find by app name and group (not robust, but okay for now)
            string appName = _grid.SelectedRows[0].Cells[0].Value.ToString();
            var rules = _repo.GetAll();
            return rules.FirstOrDefault(r => r.AppName == appName);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var dialog = new RuleEditForm();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _repo.Insert(dialog.Rule);
                LoadData();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var rule = SelectedRule();
            if (rule == null) return;
            using var dialog = new RuleEditForm(rule);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _repo.Update(dialog.Rule);
                LoadData();
            }
        }

        private void BtnToggleActive_Click(object sender, EventArgs e)
        {
            var rule = SelectedRule();
            if (rule == null) return;
            rule.IsActive = !rule.IsActive;
            _repo.Update(rule);
            if (!string.IsNullOrEmpty(rule.FirewallRuleName))
                Core.FirewallManager.SetRuleEnabled(rule.FirewallRuleName, rule.IsActive);
            LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var rule = SelectedRule();
            if (rule == null) return;
            if (MessageBox.Show($"Delete rule for \"{rule.AppName}\"?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!string.IsNullOrEmpty(rule.FirewallRuleName))
                    Core.FirewallManager.RemoveRule(rule.FirewallRuleName);
                _repo.Delete(rule.Id);
                LoadData();
            }
        }

        private void BtnAddFromProcesses_Click(object sender, EventArgs e)
        {
            var processes = Core.ProcessMonitor.GetNetworkProcesses();
            using var form = new Form
            {
                Text = "Select process",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent
            };
            var list = new ListBox { Dock = DockStyle.Fill };
            list.DataSource = processes.Select(p => $"{p.ProcessName} - {p.ExePath}").ToList();
            var btnOk = new Button { Text = "Add", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
            form.Controls.Add(list);
            form.Controls.Add(btnOk);
            if (form.ShowDialog() == DialogResult.OK && list.SelectedItem != null)
            {
                var selected = processes[list.SelectedIndex];
                var rule = new AppRule { AppName = selected.ProcessName, ExePath = selected.ExePath, Action = "block", RuleGroup = "Custom" };
                using var edit = new RuleEditForm(rule);
                if (edit.ShowDialog() == DialogResult.OK)
                {
                    _repo.Insert(edit.Rule);
                    LoadData();
                }
            }
        }
    }
}
