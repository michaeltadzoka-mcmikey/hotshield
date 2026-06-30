using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Hotshield.Core;
using Hotshield.Data;
using Hotshield.Models;

namespace Hotshield.UI
{
    public class AppConfigForm : Form
    {
        private readonly MeteredNetwork _network;
        private readonly int? _presetId;
        private TabControl _tabControl;
        private TabPage _tabRunning;
        private TabPage _tabAllApps;
        private TabPage _tabAllowed;
        private Label _titleLabel;
        private Label _statusBanner;
        private TextBox _searchBox;
        private Button _btnScan;
        private Label _scanStatusLabel;
        private Label _allowedCountLabel;
        private Button _btnSave;
        private Button _btnCancel;
        private List<AppRule> _allowedRules = new List<AppRule>();
        private List<AppExe> _availableApps = new List<AppExe>();
        private AppRuleRepo _ruleRepo = new AppRuleRepo();

        // Panels inside tabs
        private FlowLayoutPanel _runningPanel;
        private FlowLayoutPanel _availablePanel;
        private FlowLayoutPanel _allowedPanel;

        public AppConfigForm(MeteredNetwork network)
        {
            _network = network;
            _presetId = network.PresetId;
            InitializeComponent();
            LoadAllowedApps();
            LoadRunningApps();
            ScanForInstalledApps();
            UpdateStatusBanner();
        }

        private void InitializeComponent()
        {
            Text = "Allow Apps for: " + _network.Ssid;
            Size = new Size(900, 720);
            MinimumSize = new Size(850, 650);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;
            Icon = Resources.ShieldIcons.Green;

            _titleLabel = new Label { Text = "Allow apps on \"" + _network.Ssid + "\"", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(20, 12), Size = new Size(840, 28) };
            Controls.Add(_titleLabel);

            var sub = new Label { Text = "Only apps in the Allowed list can use internet. Everything else is blocked.", Font = new Font("Segoe UI", 9F), ForeColor = Color.Gray, Location = new Point(20, 44), Size = new Size(840, 18) };
            Controls.Add(sub);

            _statusBanner = new Label { Text = "", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 68), Size = new Size(840, 32), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(245, 158, 11) };
            Controls.Add(_statusBanner);

            // Tabs container — each tab holds its own panel
            _tabControl = new TabControl { Location = new Point(20, 108), Size = new Size(840, 440) };
            _tabRunning = new TabPage("Open Now");
            _tabAllApps = new TabPage("All Installed");
            _tabAllowed = new TabPage("Allowed Apps");
            _tabControl.TabPages.Add(_tabRunning);
            _tabControl.TabPages.Add(_tabAllApps);
            _tabControl.TabPages.Add(_tabAllowed);
            _tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (_tabControl.SelectedTab == _tabRunning) RefreshRunningApps();
                if (_tabControl.SelectedTab == _tabAllowed) LoadAllowedApps();
            };
            Controls.Add(_tabControl);

            // Search + Scan — placed below the tabs but still on main form
            _searchBox = new TextBox { Location = new Point(20, 560), Size = new Size(280, 26), PlaceholderText = "Search apps..." };
            _searchBox.TextChanged += (s, e) => { FilterAvailableApps(); if (_availablePanel != null) _availablePanel.VerticalScroll.Value = 0; };
            Controls.Add(_searchBox);

            _btnScan = new Button { Text = "Scan Installed Apps", Location = new Point(310, 560), Size = new Size(160, 26) };
            _btnScan.Click += (s, e) => ScanForInstalledApps();
            Controls.Add(_btnScan);

            _scanStatusLabel = new Label { Text = "", Location = new Point(480, 564), AutoSize = true };
            Controls.Add(_scanStatusLabel);

            _allowedCountLabel = new Label { Text = "Allowed: 0 apps (rest blocked)", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(20, 600), AutoSize = true };
            Controls.Add(_allowedCountLabel);

            _btnSave = new Button { Text = "Save & Close", Location = new Point(20, 640), Size = new Size(120, 36), BackColor = Color.Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnSave.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(_btnSave);

            _btnCancel = new Button { Text = "Cancel", Location = new Point(150, 640), Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
            Controls.Add(_btnCancel);
        }

        private void UpdateStatusBanner()
        {
            bool isActive = _network.IsActive;
            if (isActive)
            {
                _statusBanner.Text = "Protection is ACTIVE — only allowed apps can use data";
                _statusBanner.BackColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                _statusBanner.Text = "Protection is OFF — all apps can use data. Turn it on to start blocking.";
                _statusBanner.BackColor = Color.FromArgb(245, 158, 11);
            }
        }

        // ===== TAB 1: Open Now =====
        private void LoadRunningApps()
        {
            _runningPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            _tabRunning.Controls.Add(_runningPanel);
            RefreshRunningApps();
        }

        public void RefreshRunningApps()
        {
            if (_runningPanel == null) return;
            _runningPanel.Controls.Clear();

            try
            {
                var processes = ProcessMonitor.GetNetworkProcesses();
                if (processes.Count == 0)
                {
                    _runningPanel.Controls.Add(new Label { Text = "No apps are using the network right now.", ForeColor = Color.Gray, Padding = new Padding(10), AutoSize = true });
                    return;
                }

                int w = _runningPanel.Width - 20;
                foreach (var p in processes)
                {
                    if (_allowedRules.Any(r => r.ExePath.Equals(p.ExePath, StringComparison.OrdinalIgnoreCase))) continue;

                    string displayName = p.ProcessName;
                    if (displayName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && displayName.Length > 4)
                        displayName = displayName.Substring(0, displayName.Length - 4);

                    var row = new Panel { Width = w, Height = 30, Margin = new Padding(2), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                    row.Controls.Add(new Label { Text = displayName, Location = new Point(6, 6), Size = new Size(350, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
                    var btnAllow = new Button { Text = "Allow", Size = new Size(70, 24), Location = new Point(Math.Max(w - 80, 0), 3), FlatStyle = FlatStyle.System, Tag = p };
                    btnAllow.Click += (s, e) => { var b = s as Button; if (b?.Tag is ProcessInfo pi) AllowRunningApp(pi); };
                    row.Controls.Add(btnAllow);
                    _runningPanel.Controls.Add(row);
                }
            }
            catch
            {
                _runningPanel.Controls.Add(new Label { Text = "Could not load running apps.", ForeColor = Color.Gray, AutoSize = true });
            }
        }

        private void AllowRunningApp(ProcessInfo pi)
        {
            AllowApp(pi.ProcessName, pi.ExePath);
            RefreshRunningApps();
        }

        // ===== TAB 2: All Installed =====
        private void ScanForInstalledApps()
        {
            _availableApps.Clear();
            if (_tabAllApps.Controls.Count > 0)
            {
                var existing = _tabAllApps.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                if (existing != null) existing.Controls.Clear();
            }
            _scanStatusLabel.Text = "Scanning...";
            Application.DoEvents();

            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<AppExe>();

            string[] roots = {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    var exes = Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories);
                    foreach (var exe in exes)
                    {
                        try
                        {
                            string name = Path.GetFileNameWithoutExtension(exe);
                            if (name.Length <= 2) continue;

                            if (name == "uninstall" || name == "setup" || name == "install" || name == "Installer" ||
                                name.StartsWith("vc_redist") || name.StartsWith("dotnet") || name == "Windows10Upgrade" ||
                                (name.EndsWith("_proxy") && !name.StartsWith("brave") && !name.StartsWith("chrome") && !name.StartsWith("msedge")))
                            {
                                continue;
                            }

                            if (!found.Contains(name))
                            {
                                found.Add(name);
                                list.Add(new AppExe { Name = name, Path = exe });
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            _availableApps = list.OrderBy(a => a.Name).ToList();
            _scanStatusLabel.Text = "Found " + _availableApps.Count + " apps";
            RenderAvailableApps(_availableApps);
        }

        private void FilterAvailableApps()
        {
            string s = _searchBox.Text?.ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrEmpty(s) ? _availableApps : _availableApps.Where(a => a.Name.ToLowerInvariant().Contains(s)).ToList();
            RenderAvailableApps(filtered);
        }

        private void RenderAvailableApps(List<AppExe> apps)
        {
            if (_availablePanel != null) { _tabAllApps.Controls.Remove(_availablePanel); }
            _availablePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            _tabAllApps.Controls.Add(_availablePanel);

            if (apps.Count == 0)
            {
                _availablePanel.Controls.Add(new Label { Text = "No apps found. Click 'Scan Installed Apps' above.", ForeColor = Color.Gray, Padding = new Padding(10), AutoSize = true });
                return;
            }

            int w = _availablePanel.Width - 20;
            foreach (var app in apps)
            {
                bool anyAllowed = _allowedRules.Any(r => r.ExePath.Equals(app.Path, StringComparison.OrdinalIgnoreCase));
                if (anyAllowed) continue;

                var row = new Panel { Width = w, Height = 28, Margin = new Padding(2), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                row.Controls.Add(new Label { Text = app.Name, Location = new Point(6, 6), Size = new Size(450, 18), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
                var btnAllow = new Button { Text = "Allow", Size = new Size(70, 22), Location = new Point(Math.Max(w - 80, 0), 3), FlatStyle = FlatStyle.System, Tag = app };
                btnAllow.Click += (s, e) => { var b = s as Button; if (b?.Tag is AppExe a) AllowApp(a.Name, a.Path); };
                row.Controls.Add(btnAllow);
                _availablePanel.Controls.Add(row);
            }
        }

        // ===== TAB 3: Allowed Apps =====
        private void LoadAllowedApps()
        {
            _allowedRules.Clear();
            if (_presetId != null)
            {
                _allowedRules.AddRange(_ruleRepo.GetActiveRulesForPreset(_presetId.Value).Where(r => r.Action == "allow"));
            }
            RenderAllowedApps();
            _allowedCountLabel.Text = "Allowed: " + _allowedRules.Count + " apps (rest blocked)";
        }

        private void RenderAllowedApps()
        {
            if (_allowedPanel != null) { _tabAllowed.Controls.Remove(_allowedPanel); }
            _allowedPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.FromArgb(220, 252, 231) };
            _tabAllowed.Controls.Add(_allowedPanel);

            if (_allowedRules.Count == 0)
            {
                _allowedPanel.Controls.Add(new Label { Text = "No apps are currently allowed. Everything is blocked.", ForeColor = Color.Gray, Padding = new Padding(10), AutoSize = true });
                return;
            }

            int w = _allowedPanel.Width - 20;
            foreach (var rule in _allowedRules.ToList())
            {
                var row = new Panel { Width = w, Height = 28, Margin = new Padding(2), BorderStyle = BorderStyle.FixedSingle };
                row.Controls.Add(new Label { Text = rule.AppName, Location = new Point(6, 6), Size = new Size(300, 18), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });

                string ruleStatus = !string.IsNullOrEmpty(rule.FirewallRuleName) ? "Active" : "Pending";
                row.Controls.Add(new Label { Text = ruleStatus, Location = new Point(320, 6), Size = new Size(80, 18), ForeColor = Color.Gray, Font = new Font("Segoe UI", 8F) });

                var btn = new Button { Text = "Remove", Size = new Size(80, 22), Location = new Point(Math.Max(w - 90, 0), 3), ForeColor = Color.Red, Tag = rule };
                btn.Click += (s, e) => { var b = s as Button; if (b?.Tag is AppRule r) RemoveAllowedApp(r); };
                row.Controls.Add(btn);
                _allowedPanel.Controls.Add(row);
            }
        }

        // ===== Shared: Allow / Remove =====
        private void AllowApp(string appName, string exePath)
        {
            if (_allowedRules.Any(r => r.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase))) return;

            var existing = _ruleRepo.GetAll().FirstOrDefault(r => r.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase));
            AppRule rule;
            int id;

            if (existing != null)
            {
                rule = existing; rule.Action = "allow"; _ruleRepo.Update(rule); id = rule.Id;
            }
            else
            {
                rule = new AppRule { AppName = appName, ExePath = exePath, ServiceName = null, Action = "allow", Direction = "outbound", RuleGroup = "Custom", IsActive = true };
                id = _ruleRepo.Insert(rule);
                try
                {
                    string fw = FirewallManager.AddRule(id, rule.AppName, rule.ExePath, "allow", rule.Direction);
                    rule.FirewallRuleName = fw;
                    _ruleRepo.Update(rule);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Firewall rule failed: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (_presetId != null) _ruleRepo.LinkRuleToPreset(_presetId.Value, id);
            LoadAllowedApps();
            RefreshRunningApps();
            if (_tabControl.SelectedTab == _tabAllApps) FilterAvailableApps();
            UpdateStatusBanner();
        }

        private void RemoveAllowedApp(AppRule rule)
        {
            if (_presetId != null) _ruleRepo.UnlinkRuleFromPreset(_presetId.Value, rule.Id);
            if (!string.IsNullOrEmpty(rule.FirewallRuleName)) FirewallManager.SetRuleEnabled(rule.FirewallRuleName, false);
            _allowedRules.RemoveAll(r => r.Id == rule.Id);
            RenderAllowedApps();
            RefreshRunningApps();
            if (_tabControl.SelectedTab == _tabAllApps) FilterAvailableApps();
            UpdateStatusBanner();
            _allowedCountLabel.Text = "Allowed: " + _allowedRules.Count + " apps (rest blocked)";
        }
    }

    public class AppExe
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}