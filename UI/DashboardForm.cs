using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Hotshield.Core;
using Hotshield.Data;
using Hotshield.Models;
using Hotshield.Helpers;

namespace Hotshield.UI
{
    public class DashboardForm : Form
    {
        private readonly RuleEngine _ruleEngine;
        private Label _connectionStatusLabel;
        private Label _savingsLabel;
        private FlowLayoutPanel _networksListPanel;
        private Button _btnProtectCurrent;
        private Button _btnBlockAll;
        private Button _btnPause;
        private Button _btnChooseApps;
        private Button _btnLiveView;
        private Button _btnSettings;
        private Label _statusLabel;
        private Label _currentNetName;
        private Panel _currentNetworkSection;
        private FlowLayoutPanel _quickActions;
        private MeteredNetworkRepo _networkRepo = new MeteredNetworkRepo();
        private Timer _refreshTimer;
        private AppRuleRepo _ruleRepo = new AppRuleRepo();
        private ToolTip _toolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 500, ReshowDelay = 200 };
        private List<TableLayoutPanel> _networkCards = new List<TableLayoutPanel>();

        public DashboardForm(RuleEngine engine)
        {
            _ruleEngine = engine ?? throw new ArgumentNullException(nameof(engine));
            InitializeComponent();
            _ruleEngine.MeteredStatusChanged += OnStateChanged;
            _ruleEngine.PauseStateChanged += OnPauseChanged;
            _ruleEngine.FirewallOperationFailed += OnFirewallFailed;
            if (SettingsRepo.Get("dark_mode") == "true")
                ThemeManager.SetDarkMode(true);
            ThemeManager.ApplyToForm(this);
            RefreshAll();

            _refreshTimer = new Timer { Interval = 5000 };
            _refreshTimer.Tick += (s, e) => UpdateStatusTextOnly();
            _refreshTimer.Start();
        }

        private void InitializeComponent()
        {
            Text = "Hotshield — Data Guardian";
            Size = new Size(860, 720);
            MinimumSize = new Size(720, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 245, 245);
            Icon = Resources.ShieldIcons.Green;
            AutoScroll = true;

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 170, BackColor = Color.FromArgb(31, 41, 55), Padding = new Padding(24, 8, 24, 6) };
            var appTitle = new Label { Text = "Hotshield", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(24, 6), AutoSize = true };
            topPanel.Controls.Add(appTitle);
            var descLabel = new Label { Text = "Choose which apps are allowed to use data. Everything else is blocked.", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(209, 213, 219), Location = new Point(24, 38), Size = new Size(800, 18) };
            topPanel.Controls.Add(descLabel);

            _connectionStatusLabel = new Label { Text = "Checking network...", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(252, 211, 77), Location = new Point(24, 64), AutoSize = true };
            topPanel.Controls.Add(_connectionStatusLabel);

            _savingsLabel = new Label { Text = "", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(52, 211, 153), Location = new Point(500, 64), AutoSize = true };
            topPanel.Controls.Add(_savingsLabel);

            _currentNetName = new Label { Text = "", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(24, 88), AutoSize = true };
            topPanel.Controls.Add(_currentNetName);

            _statusLabel = new Label { Text = "", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(209, 213, 219), Location = new Point(24, 108), AutoSize = true };
            topPanel.Controls.Add(_statusLabel);
            Controls.Add(topPanel);

            var mainPanel = new Panel { Location = new Point(12, 178), Size = new Size(ClientSize.Width - 24, ClientSize.Height - 190), AutoScroll = true };
            mainPanel.Resize += (s, e) => { mainPanel.Size = new Size(ClientSize.Width - 24, ClientSize.Height - 190); };

            _currentNetworkSection = new Panel { Location = new Point(0, 0), Size = new Size(mainPanel.Width - 20, 50), BackColor = Color.White };
            _quickActions = new FlowLayoutPanel { Location = new Point(12, 8), Size = new Size(_currentNetworkSection.Width - 24, 38), FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _quickActions.Resize += (s, e) => { _quickActions.Size = new Size(_currentNetworkSection.Width - 24, 38); };

            _btnProtectCurrent = new Button { Text = "Turn ON Protection", Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.FromArgb(245, 158, 11), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(200, 36), Cursor = Cursors.Hand };
            _btnProtectCurrent.FlatAppearance.BorderSize = 0;
            _btnProtectCurrent.Click += BtnProtectCurrent_Click;
            _toolTip.SetToolTip(_btnProtectCurrent, "Start blocking apps on this network to save data");
            _quickActions.Controls.Add(_btnProtectCurrent);

            _btnBlockAll = new Button { Text = "Block All Internet", Size = new Size(170, 36), FlatStyle = FlatStyle.System, ForeColor = Color.FromArgb(220, 38, 38) };
            _btnBlockAll.Click += (s, e) => ToggleKillSwitch();
            _toolTip.SetToolTip(_btnBlockAll, "Emergency: instantly block ALL internet traffic");
            _quickActions.Controls.Add(_btnBlockAll);

            _btnChooseApps = new Button { Text = "Choose Apps to Allow", Size = new Size(190, 36), FlatStyle = FlatStyle.System, Enabled = false };
            _btnChooseApps.Click += (s, e) =>
            {
                string currentSsid = Helpers.WlanHelper.GetCurrentSsid();
                if (string.IsNullOrEmpty(currentSsid)) currentSsid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";
                if (!string.IsNullOrEmpty(currentSsid))
                {
                    var net = _networkRepo.FindBySsid(currentSsid);
                    if (net != null) OpenAppConfig(net);
                }
            };
            _toolTip.SetToolTip(_btnChooseApps, "Select which apps are allowed to use data on the current network");
            _quickActions.Controls.Add(_btnChooseApps);

            _btnPause = new Button { Text = "Stop Protection", Size = new Size(170, 36), FlatStyle = FlatStyle.System };
            _btnPause.Click += (s, e) => TogglePause();
            _toolTip.SetToolTip(_btnPause, "Temporarily stop blocking apps for a set time");
            _quickActions.Controls.Add(_btnPause);

            _currentNetworkSection.Controls.Add(_quickActions);
            mainPanel.Controls.Add(_currentNetworkSection);

            var networksLabel = new Label { Text = "Connection History", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81), Location = new Point(0, 56), AutoSize = true };
            mainPanel.Controls.Add(networksLabel);

            _networksListPanel = new FlowLayoutPanel { Location = new Point(0, 76), Size = new Size(mainPanel.Width - 20, 220), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BorderStyle = BorderStyle.Fixed3D, BackColor = Color.White };
            _networksListPanel.Resize += (s, e) => { foreach (var card in _networkCards) card.Width = _networksListPanel.Width - 6; };
            mainPanel.Controls.Add(_networksListPanel);

            var addNetBtn = new Button { Text = "Add Network", Location = new Point(0, 302), Size = new Size(140, 30), FlatStyle = FlatStyle.System };
            _toolTip.SetToolTip(addNetBtn, "Manually add a WiFi network by typing its name");
            addNetBtn.Click += (s, e) => AddNetworkManually();
            mainPanel.Controls.Add(addNetBtn);

            var bottomNav = new Panel { Location = new Point(0, 340), Size = new Size(mainPanel.Width, 36), BackColor = Color.Transparent };
            _btnLiveView = new Button { Text = "Live Processes", Size = new Size(140, 32), FlatStyle = FlatStyle.System, Location = new Point(0, 0) };
            _btnLiveView.Click += (s, e) => ShowLiveView();
            bottomNav.Controls.Add(_btnLiveView);
            _btnSettings = new Button { Text = "Settings", Size = new Size(100, 32), FlatStyle = FlatStyle.System, Location = new Point(150, 0) };
            _btnSettings.Click += (s, e) => ShowSettings();
            bottomNav.Controls.Add(_btnSettings);

            var btnExport = new Button { Text = "Export", Size = new Size(80, 32), FlatStyle = FlatStyle.System, Location = new Point(400, 0) };
            btnExport.Click += (s, e) => Helpers.ExportHelper.ExportToJson();
            bottomNav.Controls.Add(btnExport);
            var btnImport = new Button { Text = "Import", Size = new Size(80, 32), FlatStyle = FlatStyle.System, Location = new Point(490, 0) };
            btnImport.Click += (s, e) => Helpers.ExportHelper.ImportFromJson();
            bottomNav.Controls.Add(btnImport);

            mainPanel.Controls.Add(bottomNav);
            Controls.Add(mainPanel);
        }

        private void UpdateStatusTextOnly()
        {
            if (InvokeRequired) { Invoke(() => UpdateStatusTextOnly()); return; }
            string source = Helpers.AdapterDetector.GetActiveConnectionSource();
            bool killActive = SettingsRepo.Get("kill_switch_active") == "true";
            bool paused = _ruleEngine.IsPaused;

            if (string.IsNullOrEmpty(source) || source == "No Network")
            {
                _connectionStatusLabel.Text = "No network detected";
                _currentNetName.Text = "";
                _statusLabel.Text = "Connect to WiFi or USB tether to get started.";
                return;
            }

            _connectionStatusLabel.Text = "Connected: " + source;
            _currentNetName.Text = "";

            if (killActive)
            {
                _statusLabel.Text = "EMERGENCY BLOCK: All internet blocked.";
                _statusLabel.ForeColor = Color.Red;
            }
            else if (paused)
            {
                _statusLabel.Text = "Protection stopped until " + _ruleEngine.ResumeTime.ToString("HH:mm");
                _statusLabel.ForeColor = Color.Gray;
            }
            else if (_ruleEngine.IsOnMeteredNetwork)
            {
                var counts = _ruleEngine.GetActiveRuleCounts();
                _statusLabel.Text = "Protection ON — " + counts.Allowing + " app(s) allowed, " + counts.Blocking + " blocked";
                _statusLabel.ForeColor = Color.FromArgb(52, 211, 153);
            }
            else
            {
                _statusLabel.Text = "This network is not protected. Click Turn ON Protection to start.";
                _statusLabel.ForeColor = Color.FromArgb(209, 213, 219);
            }
        }

        private void RefreshAll()
        {
            UpdateConnectionStatus();
            RebuildNetworkCards();
            UpdateSavingsLabel();
        }

        private void UpdateConnectionStatus()
        {
            string source = Helpers.AdapterDetector.GetActiveConnectionSource();
            bool killActive = SettingsRepo.Get("kill_switch_active") == "true";
            bool paused = _ruleEngine.IsPaused;

            if (string.IsNullOrEmpty(source) || source == "No Network")
            {
                _connectionStatusLabel.Text = "No network detected";
                _connectionStatusLabel.ForeColor = Color.FromArgb(209, 213, 219);
                _currentNetName.Text = "";
                _statusLabel.Text = "Connect to a WiFi or USB tether to get started.";
                _statusLabel.ForeColor = Color.FromArgb(209, 213, 219);
                _btnProtectCurrent.Enabled = false;
                _btnChooseApps.Enabled = false;
                _btnProtectCurrent.Text = "Turn ON Protection";
                return;
            }

            _connectionStatusLabel.Text = "Connected: " + source;
            _connectionStatusLabel.ForeColor = Color.FromArgb(252, 211, 77);

            string currentSsid = Helpers.WlanHelper.GetCurrentSsid();
            if (string.IsNullOrEmpty(currentSsid))
                currentSsid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";

            // Auto-add new network to history if not present
            if (!string.IsNullOrEmpty(currentSsid) && _networkRepo.FindBySsid(currentSsid) == null)
            {
                try
                {
                    _networkRepo.Add(new MeteredNetwork { Ssid = currentSsid, PresetId = null, IsActive = false });
                }
                catch { }
            }

            var currentNet = _networkRepo.FindBySsid(currentSsid);
            if (currentNet != null)
            {
                string displayName = NameFormatter.GetDisplayName(currentNet.Ssid);
                _currentNetName.Text = "Current: " + displayName;
                _currentNetName.ForeColor = Color.White;
            }
            else if (!string.IsNullOrEmpty(currentSsid))
            {
                string displayName = NameFormatter.GetDisplayName(currentSsid);
                _currentNetName.Text = displayName;
                _currentNetName.ForeColor = Color.FromArgb(209, 213, 219);
            }
            else
            {
                _currentNetName.Text = "";
            }

            if (killActive)
            {
                _statusLabel.Text = "EMERGENCY: All internet is blocked";
                _statusLabel.ForeColor = Color.Red;
                _btnProtectCurrent.Text = "Turn OFF Emergency Block";
                _btnProtectCurrent.Enabled = true;
                _btnChooseApps.Enabled = false;
                _btnBlockAll.Text = "Unblock All";
                _btnBlockAll.BackColor = Color.FromArgb(254, 226, 226);
            }
            else if (paused)
            {
                _statusLabel.Text = "Protection stopped — resumes at " + _ruleEngine.ResumeTime.ToString("HH:mm");
                _statusLabel.ForeColor = Color.Gray;
                _btnProtectCurrent.Text = "Resume Protection";
                _btnProtectCurrent.Enabled = true;
                _btnChooseApps.Enabled = false;
                _btnBlockAll.Text = "Block All Internet";
                _btnBlockAll.BackColor = SystemColors.Control;
            }
            else if (_ruleEngine.IsOnMeteredNetwork && currentNet != null && currentNet.IsActive)
            {
                var counts = _ruleEngine.GetActiveRuleCounts();
                _statusLabel.Text = "Protection is ON — " + counts.Allowing + " app(s) allowed, " + counts.Blocking + " blocked";
                _statusLabel.ForeColor = Color.FromArgb(52, 211, 153);
                _btnProtectCurrent.Text = "Turn OFF Protection";
                _btnProtectCurrent.Enabled = true;
                _btnChooseApps.Enabled = true;
                _btnBlockAll.Text = "Block All Internet";
                _btnBlockAll.BackColor = SystemColors.Control;
            }
            else if (_ruleEngine.IsOnMeteredNetwork && currentNet != null && !currentNet.IsActive)
            {
                _statusLabel.Text = "Protection is OFF for this network (toggle ON below)";
                _statusLabel.ForeColor = Color.Gray;
                _btnProtectCurrent.Text = "Turn ON Protection";
                _btnProtectCurrent.Enabled = true;
                _btnChooseApps.Enabled = true;
                _btnBlockAll.Text = "Block All Internet";
                _btnBlockAll.BackColor = SystemColors.Control;
            }
            else
            {
                _statusLabel.Text = "This network is not yet protected.";
                _statusLabel.ForeColor = Color.FromArgb(209, 213, 219);
                _btnProtectCurrent.Text = "Turn ON Protection";
                _btnProtectCurrent.Enabled = true;
                _btnChooseApps.Enabled = false;
                _btnBlockAll.Text = "Block All Internet";
                _btnBlockAll.BackColor = SystemColors.Control;
            }
        }

        private void RebuildNetworkCards()
        {
            _networksListPanel.Controls.Clear();
            _networkCards.Clear();

            var networks = _networkRepo.GetAll();
            string currentSsid = Helpers.WlanHelper.GetCurrentSsid();
            string currentUsb = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";

            if (networks.Count == 0)
            {
                _networksListPanel.Controls.Add(new Label { Text = "No networks yet. Connect to WiFi and click Turn ON Protection above.", ForeColor = Color.Gray, Padding = new Padding(12), AutoSize = true });
                return;
            }

            int panelWidth = _networksListPanel.Width - 6;

            foreach (var net in networks)
            {
                bool isCurrent = net.Ssid.Equals(currentSsid, StringComparison.OrdinalIgnoreCase) ||
                                 net.Ssid.Equals(currentUsb, StringComparison.OrdinalIgnoreCase);

                int allowedCount = 0;
                if (net.PresetId != null)
                {
                    var presetRules = _ruleRepo.GetActiveRulesForPreset(net.PresetId);
                    allowedCount = presetRules.Count(r => r.Action == "allow");
                }

                string displayName = NameFormatter.GetDisplayName(net.Ssid);

                var card = new TableLayoutPanel
                {
                    Width = panelWidth,
                    Height = 44,
                    Margin = new Padding(2, 2, 2, 2),
                    BackColor = isCurrent ? Color.FromArgb(254, 249, 195) : Color.White,
                    ColumnCount = 4,
                    RowCount = 2,
                    Padding = new Padding(6, 2, 6, 2)
                };
                card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
                card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
                card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));

                var nameLabel = new Label
                {
                    Text = (isCurrent ? "Current: " : "History: ") + displayName,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(31, 41, 55),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };
                card.Controls.Add(nameLabel, 0, 0);
                card.SetRowSpan(nameLabel, 2);

                string infoText = isCurrent ? "Active now" : "Offline";
                infoText += " | " + (allowedCount > 0 ? allowedCount + " app(s) allowed" : "No apps allowed");
                var infoLabel = new Label
                {
                    Text = infoText,
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                card.Controls.Add(infoLabel, 1, 0);
                card.SetRowSpan(infoLabel, 2);

                var btnToggle = new Button
                {
                    Text = net.IsActive ? "Protection ON" : "Protection OFF",
                    Size = new Size(125, 28),
                    FlatStyle = FlatStyle.System,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    BackColor = net.IsActive ? Color.FromArgb(220, 252, 231) : Color.FromArgb(243, 244, 246),
                    Tag = net,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2, 8, 2, 8)
                };
                _toolTip.SetToolTip(btnToggle, net.IsActive ? "Turn off protection" : "Turn on protection");
                btnToggle.Click += (s, e) =>
                {
                    var btn = s as Button;
                    if (btn?.Tag is MeteredNetwork network)
                    {
                        bool newState = !network.IsActive;
                        if (newState == true)
                        {
                            _ruleEngine.EnsureNetworkHasPreset(network);
                        }
                        _networkRepo.UpdateIsActive(network.Id, newState);
                        network.IsActive = newState;
                        _ruleEngine.Initialise();
                        RebuildNetworkCards();
                        UpdateConnectionStatus();
                    }
                };
                card.Controls.Add(btnToggle, 2, 0);
                card.SetRowSpan(btnToggle, 2);

                var btnApps = new Button
                {
                    Text = "Allow Apps",
                    Size = new Size(140, 28),
                    FlatStyle = FlatStyle.System,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2, 8, 2, 8),
                    Tag = net
                };
                _toolTip.SetToolTip(btnApps, "Select which apps can use data on this network");
                btnApps.Click += (s, e) =>
                {
                    var btn = s as Button;
                    if (btn?.Tag is MeteredNetwork network)
                        OpenAppConfig(network);
                };
                card.Controls.Add(btnApps, 3, 0);
                card.SetRowSpan(btnApps, 2);

                var btnDelete = new Button
                {
                    Text = "X",
                    Size = new Size(22, 22),
                    FlatStyle = FlatStyle.System,
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                    Location = new Point(card.Width - 28, 1),
                    Tag = net,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                _toolTip.SetToolTip(btnDelete, "Remove this network");
                btnDelete.Click += (s, e) =>
                {
                    var btn = s as Button;
                    if (btn?.Tag is MeteredNetwork network)
                    {
                        if (MessageBox.Show("Remove " + NameFormatter.GetDisplayName(network.Ssid) + "?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _networkRepo.Delete(network.Id);
                            RefreshAll();
                        }
                    }
                };
                card.Controls.Add(btnDelete);

                _networkCards.Add(card);
                _networksListPanel.Controls.Add(card);
            }
        }

        private void UpdateSavingsLabel()
        {
            int totalActiveRules = _ruleRepo.GetAllFirewallRuleNames().Count;
            if (_ruleEngine.IsOnMeteredNetwork)
                _savingsLabel.Text = totalActiveRules + " app(s) blocked";
            else
                _savingsLabel.Text = totalActiveRules + " rule(s) configured";
        }

        private void OpenAppConfig(MeteredNetwork network)
        {
            using var config = new AppConfigForm(network);
            config.ShowDialog();
            RefreshAll();
        }

        private void BtnProtectCurrent_Click(object? sender, EventArgs e)
        {
            bool killActive = SettingsRepo.Get("kill_switch_active") == "true";
            bool paused = _ruleEngine.IsPaused;

            if (killActive)
            {
                _ruleEngine.ToggleKillSwitch(false);
                RefreshAll();
                return;
            }

            if (paused)
            {
                _ruleEngine.ResumeProtection();
                RefreshAll();
                return;
            }

            string currentSsid = Helpers.WlanHelper.GetCurrentSsid();
            if (string.IsNullOrEmpty(currentSsid))
                currentSsid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";

            if (!string.IsNullOrEmpty(currentSsid))
            {
                var existing = _networkRepo.FindBySsid(currentSsid);
                if (existing != null)
                {
                    bool newState = !existing.IsActive;
                    // Centralized preset assignment — throws if preset missing
                    if (newState == true)
                    {
                        _ruleEngine.EnsureNetworkHasPreset(existing);
                    }
                    _networkRepo.UpdateIsActive(existing.Id, newState);
                    existing.IsActive = newState;
                    _ruleEngine.Initialise();
                    RefreshAll();
                    return;
                }
            }

            bool isUsb = false;
            string ssid = Helpers.WlanHelper.GetCurrentSsid();
            if (string.IsNullOrEmpty(ssid))
            {
                ssid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";
                isUsb = !string.IsNullOrEmpty(ssid);
            }

            if (string.IsNullOrEmpty(ssid))
            {
                MessageBox.Show("No WiFi or USB tether detected.\nConnect to a network first.", "No Network", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var presets = PresetRepo.GetAll();
            var hotspot = presets.FirstOrDefault(p => p.Name == "Hotspot Mode");
            if (hotspot == null) return;

            _networkRepo.Add(new MeteredNetwork { Ssid = ssid, PresetId = hotspot.Id, IsActive = true });
            _ruleEngine.Initialise();

            string type = isUsb ? "USB Tether" : "WiFi";
            string displayName = NameFormatter.GetDisplayName(ssid);
            MessageBox.Show(type + " \"" + displayName + "\" is now protected.\nEverything is blocked. Click Allow Apps to choose exceptions.", "Protected", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshAll();
        }

        private void ToggleKillSwitch()
        {
            bool current = SettingsRepo.Get("kill_switch_active") == "true";
            _ruleEngine.ToggleKillSwitch(!current);
            UpdateConnectionStatus();
            UpdateSavingsLabel();
        }

        private void TogglePause()
        {
            if (_ruleEngine.IsPaused)
            {
                _ruleEngine.ResumeProtection();
                _btnPause.Text = "Stop Protection";
            }
            else
            {
                using var form = new Form
                {
                    Text = "Stop Protection",
                    Size = new System.Drawing.Size(300, 160),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                var label = new Label { Text = "Stop protection for how long?", Location = new Point(12, 12), AutoSize = true, Font = new Font("Segoe UI", 9F) };
                var combo = new ComboBox { Location = new Point(12, 36), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
                combo.Items.AddRange(new object[] { "5 minutes", "15 minutes", "30 minutes", "1 hour" });
                combo.SelectedIndex = 0;
                var btnOk = new Button { Text = "Stop", Location = new Point(180, 36), Size = new Size(90, 26), FlatStyle = FlatStyle.System };
                btnOk.Click += (s2, e2) =>
                {
                    int minutes = combo.SelectedIndex switch { 0 => 5, 1 => 15, 2 => 30, 3 => 60, _ => 5 };
                    _ruleEngine.PauseProtection(minutes);
                    _btnPause.Text = "Resume";
                    form.Close();
                };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(180, 72), Size = new Size(90, 26), FlatStyle = FlatStyle.System, DialogResult = DialogResult.Cancel };
                form.Controls.AddRange(new Control[] { label, combo, btnOk, btnCancel });
                form.ShowDialog();
            }
            UpdateConnectionStatus();
        }

        private void AddNetworkManually()
        {
            string ssid = Microsoft.VisualBasic.Interaction.InputBox("Enter WiFi name (SSID):", "Add Network", "");
            if (string.IsNullOrWhiteSpace(ssid)) return;
            if (_networkRepo.FindBySsid(ssid) != null)
            {
                MessageBox.Show("Network already in your list.", "Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var presets = PresetRepo.GetAll();
            var hotspot = presets.FirstOrDefault(p => p.Name == "Hotspot Mode");
            if (hotspot == null) return;
            _networkRepo.Add(new MeteredNetwork { Ssid = ssid, PresetId = hotspot.Id, IsActive = true });
            RefreshAll();
        }

        private void ShowLiveView()
        {
            using var liveForm = new Form { Text = "Live Processes", Size = new System.Drawing.Size(600, 450), StartPosition = FormStartPosition.CenterParent, Font = new Font("Segoe UI", 9F) };
            var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
            grid.Columns.Add("Process", "Process");
            grid.Columns.Add("Path", "Path");
            grid.Columns.Add("Status", "Status");
            var refreshBtn = new Button { Text = "Refresh", Dock = DockStyle.Bottom, Size = new Size(0, 32) };
            void LoadProcesses()
            {
                grid.Rows.Clear();
                var processes = ProcessMonitor.GetNetworkProcesses();
                foreach (var p in processes)
                {
                    grid.Rows.Add(p.ProcessName, p.ExePath, _ruleEngine.IsOnMeteredNetwork ? "Monitored" : "Unknown");
                }
            }
            refreshBtn.Click += (s, e) => LoadProcesses();
            liveForm.Controls.Add(grid);
            liveForm.Controls.Add(refreshBtn);
            LoadProcesses();
            liveForm.ShowDialog();
        }

        private void ShowSettings()
        {
            using var form = new Form { Text = "Settings", Size = new System.Drawing.Size(500, 350), StartPosition = FormStartPosition.CenterParent, Font = new Font("Segoe UI", 9F), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var chkStartup = new CheckBox { Text = "Launch on Windows startup", Location = new Point(20, 20), AutoSize = true, Checked = Helpers.StartupHelper.IsEnabled() };
            var chkNotifications = new CheckBox { Text = "Show notifications", Location = new Point(20, 50), AutoSize = true, Checked = SettingsRepo.Get("show_notifications") == "true" };
            var btnReset = new Button { Text = "Reset All Data", Location = new Point(20, 100), Size = new Size(140, 30), FlatStyle = FlatStyle.System, ForeColor = Color.Red };
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("Remove all networks and reset to defaults?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hotshield", "hotshield.db");
                    try
                    {
                        if (File.Exists(dbPath)) File.Delete(dbPath);
                        Data.Database.Initialise();
                        MessageBox.Show("Data reset.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            var btnSave = new Button { Text = "Save", Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.FromArgb(22, 163, 74), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(300, 200), Size = new Size(100, 35) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                if (chkStartup.Checked) Helpers.StartupHelper.Enable(); else Helpers.StartupHelper.Disable();
                SettingsRepo.Set("show_notifications", chkNotifications.Checked ? "true" : "false");
                form.Close();
            };
            form.Controls.AddRange(new Control[] { chkStartup, chkNotifications, btnReset, btnSave });
            form.ShowDialog();
        }

        private void OnStateChanged(object? sender, bool isMetered)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatusTextOnly()); return; }
            UpdateStatusTextOnly();
        }

        private void OnPauseChanged(object? sender, EventArgs e)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatusTextOnly()); return; }
            UpdateStatusTextOnly();
        }

        private void OnFirewallFailed(object? sender, string message)
        {
            if (InvokeRequired) { Invoke(() => ShowBalloonWarning(message)); return; }
            ShowBalloonWarning(message);
        }

        private void ShowBalloonWarning(string message)
        {
            if (SettingsRepo.Get("show_notifications") == "true")
            {
                // Use tray icon if available, otherwise just update status
                _statusLabel.Text = "WARNING: " + message;
                _statusLabel.ForeColor = Color.Red;
            }
        }
    }
}