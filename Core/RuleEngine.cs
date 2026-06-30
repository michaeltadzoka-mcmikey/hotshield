using System;
using System.Collections.Generic;
using System.Linq;
using Hotshield.Data;
using Hotshield.Models;

namespace Hotshield.Core
{
    public class RuleEngine
    {
        private readonly NetworkWatcher _watcher;
        private readonly MeteredNetworkRepo _networkRepo;
        private readonly AppRuleRepo _ruleRepo;
        // PresetRepo is static — no instance needed; use PresetRepo.GetAll() directly
        private readonly object _applyLock = new();
        private string _currentSsid = "";
        public bool IsOnMeteredNetwork { get; private set; }
        public string CurrentSsid => _currentSsid;
        public bool IsPaused { get; private set; }
        public DateTime ResumeTime { get; private set; }
        public event EventHandler<bool>? MeteredStatusChanged;
        public event EventHandler<string>? FirewallOperationFailed;
        public event EventHandler? PauseStateChanged;

        public RuleEngine()
        {
            _watcher = new NetworkWatcher();
            _networkRepo = new MeteredNetworkRepo();
            _ruleRepo = new AppRuleRepo();
            _watcher.NetworkChanged += OnNetworkChanged;
        }

        private void UpdateCurrentNetwork()
        {
            _currentSsid = Helpers.WlanHelper.GetCurrentSsid();
            if (string.IsNullOrEmpty(_currentSsid))
                _currentSsid = Helpers.AdapterDetector.GetUsbTetherAdapterName() ?? "";
            if (string.IsNullOrEmpty(_currentSsid))
                _currentSsid = Helpers.AdapterDetector.GetNonWifiNetworkKey() ?? "";
        }

        public void Initialise()
        {
            lock (_applyLock)
            {
                // Cleanup stale rules from previous runs
                CleanupStaleRules();
                UpdateCurrentNetwork();
                ApplyRulesForNetwork(_currentSsid);
            }
        }

        private void CleanupStaleRules()
        {
            try
            {
                // Remove any orphaned Hotshield rules that shouldn't be enabled
                var allRuleNames = _ruleRepo.GetAllFirewallRuleNames();
                foreach (var name in allRuleNames)
                {
                    FirewallManager.SetRuleEnabled(name, false);
                }
                FirewallManager.SetBlockRuleEnabled("Hotshield_Protection_Block", false);
                FirewallManager.DisableKillSwitch();
            }
            catch { }
        }

        private void OnNetworkChanged(object? s, string source)
        {
            lock (_applyLock)
            {
                UpdateCurrentNetwork();
                ApplyRulesForNetwork(_currentSsid);
            }
        }

        private void ApplyRulesForNetwork(string ssid)
        {
            bool anyFailure = false;
            if (SettingsRepo.Get("kill_switch_active") == "true")
            {
                DisableAllRules();
                var whitelist = WhitelistRepo.GetAllowedApps();
                if (!FirewallManager.EnableKillSwitch(whitelist))
                    anyFailure = true;
                IsOnMeteredNetwork = true;
                MeteredStatusChanged?.Invoke(this, true);
                if (anyFailure)
                    FirewallOperationFailed?.Invoke(this, "Failed to enable kill switch.");
                return;
            }
            DisableAllRules();
            if (IsPaused)
            {
                IsOnMeteredNetwork = false;
                MeteredStatusChanged?.Invoke(this, false);
                return;
            }
            if (string.IsNullOrEmpty(ssid))
            {
                IsOnMeteredNetwork = false;
                MeteredStatusChanged?.Invoke(this, false);
                return;
            }
            var net = _networkRepo.FindBySsid(ssid);
            if (net == null)
            {
                string? adapter = Helpers.AdapterDetector.GetUsbTetherAdapterName();
                if (adapter != null) net = _networkRepo.FindBySsid(adapter);
            }
            if (net != null && net.IsActive && net.PresetId != null)
            {
                var allowRules = _ruleRepo.GetActiveRulesForPreset(net.PresetId.Value)
                                         .Where(r => r.Action == "allow")
                                         .ToList();
                // Ensure blanket block rule exists
                string blockRuleName = "Hotshield_Protection_Block";
                try { FirewallManager.AddBlockRule(blockRuleName); } catch { /* may already exist */ }
                // Enable block rule FIRST
                if (!FirewallManager.SetBlockRuleEnabled(blockRuleName, true))
                    anyFailure = true;
                // Then enable allow rules
                foreach (var rule in allowRules)
                {
                    if (!string.IsNullOrEmpty(rule.FirewallRuleName))
                    {
                        if (!FirewallManager.SetRuleEnabled(rule.FirewallRuleName, true))
                            anyFailure = true;
                    }
                }
                IsOnMeteredNetwork = true;
                MeteredStatusChanged?.Invoke(this, true);
            }
            else if (net != null && !net.IsActive)
            {
                IsOnMeteredNetwork = false;
                MeteredStatusChanged?.Invoke(this, false);
            }
            else
            {
                IsOnMeteredNetwork = false;
                MeteredStatusChanged?.Invoke(this, false);
            }
            if (anyFailure)
                FirewallOperationFailed?.Invoke(this, "Some firewall rules failed. Check admin rights.");
        }

        private void DisableAllRules()
        {
            var allRuleNames = _ruleRepo.GetAllFirewallRuleNames();
            foreach (var name in allRuleNames)
                FirewallManager.SetRuleEnabled(name, false);
            FirewallManager.SetBlockRuleEnabled("Hotshield_Protection_Block", false);
            if (SettingsRepo.Get("kill_switch_active") == "true")
                FirewallManager.DisableKillSwitch();
        }

        public (int Blocking, int Allowing) GetActiveRuleCounts()
        {
            lock (_applyLock)
            {
                if (SettingsRepo.Get("kill_switch_active") == "true")
                {
                    int whitelistCount = WhitelistRepo.GetAllowedApps().Count;
                    return (1, whitelistCount);
                }
                if (IsPaused || string.IsNullOrEmpty(_currentSsid))
                    return (0, 0);
                var net = _networkRepo.FindBySsid(_currentSsid);
                if (net == null || net.PresetId == null) return (0, 0);
                var rules = _ruleRepo.GetActiveRulesForPreset(net.PresetId.Value);
                int allow = rules.Count(r => r.Action == "allow");
                return (1, allow);
            }
        }

        // Ensures a network has a usable preset before activation.
        // This is the ONLY place preset-assignment logic should live.
        public void EnsureNetworkHasPreset(MeteredNetwork net)
        {
            if (net.PresetId != null) return;

            var hotspotPreset = PresetRepo.GetAll()
                .FirstOrDefault(p => p.Name == "Hotspot Mode");

            if (hotspotPreset == null)
                throw new InvalidOperationException(
                    "Hotspot Mode preset missing — database seed failed. Run Database.Initialise() again.");

            _networkRepo.UpdatePreset(net.Id, hotspotPreset.Id);
            net.PresetId = hotspotPreset.Id;
        }

        public void ToggleKillSwitch(bool enable)
        {
            lock (_applyLock)
            {
                SettingsRepo.Set("kill_switch_active", enable ? "true" : "false");
                if (enable)
                {
                    DisableAllRules();
                    var whitelist = WhitelistRepo.GetAllowedApps();
                    if (!FirewallManager.EnableKillSwitch(whitelist))
                        FirewallOperationFailed?.Invoke(this, "Failed to enable kill switch.");
                    IsOnMeteredNetwork = true;
                    MeteredStatusChanged?.Invoke(this, true);
                }
                else
                {
                    FirewallManager.DisableKillSwitch();
                    ApplyRulesForNetwork(_currentSsid);
                }
            }
        }

        public void PauseProtection(int minutes)
        {
            lock (_applyLock)
            {
                if (IsPaused) return;
                IsPaused = true;
                ResumeTime = DateTime.Now.AddMinutes(minutes);
                SettingsRepo.Set("pause_active", "true");
                SettingsRepo.Set("pause_resume_time", ResumeTime.ToString("o"));
                DisableAllRules();
                var timer = new System.Timers.Timer(minutes * 60 * 1000);
                timer.Elapsed += (s, e) => ResumeProtection();
                timer.AutoReset = false;
                timer.Start();
                PauseStateChanged?.Invoke(this, EventArgs.Empty);
                MeteredStatusChanged?.Invoke(this, false);
            }
        }

        public void ResumeProtection()
        {
            lock (_applyLock)
            {
                if (!IsPaused) return;
                IsPaused = false;
                SettingsRepo.Set("pause_active", "false");
                ApplyRulesForNetwork(_currentSsid);
                PauseStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}