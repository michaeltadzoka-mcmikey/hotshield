using System;
using System.Windows.Forms;
using Hotshield.Core;
using Hotshield.Data;
using Hotshield.Resources;
using Hotshield.Helpers;

namespace Hotshield.UI
{
    public class TrayIcon : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _menu;
        private RuleEngine _ruleEngine;
        private DashboardForm? _dashboard;
        private ToolStripMenuItem _toggleItem;
        private ToolStripMenuItem _killItem;
        private ToolStripMenuItem _networkItem;

        public TrayIcon()
        {
            if (SettingsRepo.Get("first_run_complete") != "true")
            {
                var wizard = new WizardForm();
                wizard.ShowDialog();
            }

            _ruleEngine = new RuleEngine();
            _ruleEngine.MeteredStatusChanged += (s, e) => UpdateMenu();
            _ruleEngine.FirewallOperationFailed += (s, msg) => ShowWarning(msg);
            _ruleEngine.PauseStateChanged += (s, e) => UpdateMenu();

            BuildTrayMenu();
            BuildTrayIcon();
            _ruleEngine.Initialise();
            UpdateMenu();
            UpdateTooltip();
        }

        private void BuildTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = ShieldIcons.Green,
                Text = "Hotshield — Data Guardian",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _trayIcon.DoubleClick += (s, e) => ShowDashboard();
        }

        private void BuildTrayMenu()
        {
            _menu = new ContextMenuStrip();

            // 1. Open Dashboard
            var openItem = new ToolStripMenuItem("📊  Open Dashboard");
            openItem.Click += (s, e) => ShowDashboard();
            _menu.Items.Add(openItem);

            _menu.Items.Add(new ToolStripSeparator());

            // 2. Turn Protection ON/OFF (context-sensitive, one button does it all)
            _toggleItem = new ToolStripMenuItem("🛡️  Turn ON Protection");
            _toggleItem.Click += (s, e) => ToggleProtection();
            _menu.Items.Add(_toggleItem);

            // 3. Emergency Block All
            _killItem = new ToolStripMenuItem("🚫  Block All Internet");
            _killItem.Click += (s, e) =>
            {
                bool current = SettingsRepo.Get("kill_switch_active") == "true";
                _ruleEngine.ToggleKillSwitch(!current);
                UpdateMenu();
                UpdateTooltip();
            };
            _menu.Items.Add(_killItem);

            _menu.Items.Add(new ToolStripSeparator());

            // 4. Network status
            _networkItem = new ToolStripMenuItem("📡  Network: Checking...");
            _networkItem.Enabled = false;
            _menu.Items.Add(_networkItem);

            // 5. Choose Apps for current network (quick access)
            var chooseAppsItem = new ToolStripMenuItem("✅  Choose Apps to Allow...");
            chooseAppsItem.Click += (s, e) =>
            {
                string ssid = Helpers.WlanHelper.GetCurrentSsid();
                if (string.IsNullOrEmpty(ssid))
                    ssid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";
                if (!string.IsNullOrEmpty(ssid))
                {
                    var net = new MeteredNetworkRepo().FindBySsid(ssid);
                    if (net != null)
                    {
                        ShowDashboard();
                        new AppConfigForm(net).ShowDialog();
                    }
                }
            };
            _menu.Items.Add(chooseAppsItem);

            _menu.Items.Add(new ToolStripSeparator());

            // 6. Exit - clearly warns
            var exitItem = new ToolStripMenuItem("🚪  Exit (Stops All Protection)");
            exitItem.Click += OnExit;
            _menu.Items.Add(exitItem);
        }

        private void ToggleProtection()
        {
            bool killActive = SettingsRepo.Get("kill_switch_active") == "true";

            if (killActive)
            {
                _ruleEngine.ToggleKillSwitch(false);
            }
            else if (_ruleEngine.IsPaused)
            {
                _ruleEngine.ResumeProtection();
            }
            else if (_ruleEngine.IsOnMeteredNetwork)
            {
                _ruleEngine.PauseProtection(60);
            }
            else
            {
                // Try to protect current network
                string ssid = Helpers.WlanHelper.GetCurrentSsid();
                if (string.IsNullOrEmpty(ssid))
                    ssid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";

                if (!string.IsNullOrEmpty(ssid))
                {
                    var repo = new MeteredNetworkRepo();
                    if (repo.FindBySsid(ssid) == null)
                    {
                        var presets = PresetRepo.GetAll();
                        var hotspot = presets.Find(p => p.Name == "Hotspot Mode");
                        if (hotspot != null)
                        {
                            repo.Add(new Models.MeteredNetwork { Ssid = ssid, PresetId = hotspot.Id, IsActive = true });
                            _ruleEngine.Initialise();
                        }
                    }
                }
            }

            UpdateMenu();
            UpdateTooltip();
        }

        private void UpdateMenu()
        {
            bool killActive = SettingsRepo.Get("kill_switch_active") == "true";
            bool paused = _ruleEngine.IsPaused;

            // Protection toggle text
            if (killActive)
                _toggleItem.Text = "🚫  Turn OFF Emergency Block";
            else if (paused)
                _toggleItem.Text = "▶️  Resume Protection";
            else if (_ruleEngine.IsOnMeteredNetwork)
                _toggleItem.Text = "⏸️  Stop Protection";
            else
                _toggleItem.Text = "🛡️  Turn ON Protection";

            // Kill switch text
            _killItem.Text = killActive ? "✅  Unblock All Internet" : "🚫  Block All Internet";

            // Network status
            string source = Helpers.AdapterDetector.GetActiveConnectionSource();
            if (string.IsNullOrEmpty(source) || source == "No Network")
                _networkItem.Text = "📡  No network";
            else if (killActive)
                _networkItem.Text = $"📡  {source} [ALL BLOCKED]";
            else if (paused)
                _networkItem.Text = $"📡  {source} [Protection OFF]";
            else if (_ruleEngine.IsOnMeteredNetwork)
                _networkItem.Text = $"📡  {source} [Protected]";
            else
                _networkItem.Text = $"📡  {source}";

            // Tray icon color
            if (killActive)
                _trayIcon.Icon = ShieldIcons.Red;
            else if (paused)
                _trayIcon.Icon = ShieldIcons.Green;
            else if (_ruleEngine.IsOnMeteredNetwork)
                _trayIcon.Icon = ShieldIcons.Amber;
            else
                _trayIcon.Icon = ShieldIcons.Green;
        }

        private void UpdateTooltip()
        {
            string source = Helpers.AdapterDetector.GetActiveConnectionSource();
            string status = SettingsRepo.Get("kill_switch_active") == "true" ? "ALL BLOCKED" :
                            _ruleEngine.IsPaused ? "Protection OFF" :
                            _ruleEngine.IsOnMeteredNetwork ? "Protected" : "Monitoring";
            _trayIcon.Text = $"Hotshield — {status} — {source}";
        }

        private void ShowWarning(string message)
        {
            _trayIcon.ShowBalloonTip(5000, "Hotshield", message, ToolTipIcon.Warning);
        }

        private void ShowDashboard()
        {
            if (_dashboard == null || _dashboard.IsDisposed)
            {
                _dashboard = new DashboardForm(_ruleEngine);
                _dashboard.FormClosing += (s, e) =>
                {
                    if (e.CloseReason == CloseReason.UserClosing)
                    {
                        e.Cancel = true;
                        _dashboard.Hide();
                    }
                };
                _dashboard.Show();
            }
            else
            {
                if (_dashboard.Visible)
                    _dashboard.BringToFront();
                else
                    _dashboard.Show();
            }
        }

        public void ShowDashboardFromExternal()
        {
            if (_trayIcon.ContextMenuStrip?.InvokeRequired == true)
                _trayIcon.ContextMenuStrip.Invoke(() => ShowDashboard());
            else
                ShowDashboard();
        }

        public void ShowDashboardAsync()
        {
            var timer = new Timer { Interval = 200 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                ShowDashboard();
            };
            timer.Start();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            // Confirm before exiting - protection will stop
            var result = MessageBox.Show(
                "Are you sure you want to close Hotshield?\n\n" +
                "All network protection will stop and apps will be able to use data freely.\n\n" +
                "You can restart Hotshield from the Start menu.",
                "Exit Hotshield?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            _trayIcon.Visible = false;

            bool cleanupDone = false;
            var cleanupThread = new System.Threading.Thread(() =>
            {
                try
                {
                    var ruleNames = new Data.AppRuleRepo().GetAllFirewallRuleNames();
                    foreach (var name in ruleNames)
                        FirewallManager.SetRuleEnabled(name, false);
                    FirewallManager.DisableKillSwitch();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exit cleanup error: {ex.Message}");
                }
                cleanupDone = true;
            });
            cleanupThread.Start();

            for (int i = 0; i < 30 && !cleanupDone; i++)
            {
                System.Threading.Thread.Sleep(100);
                Application.DoEvents();
            }

            Application.Exit();
        }
    }
}