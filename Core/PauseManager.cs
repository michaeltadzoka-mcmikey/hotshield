using System;
using System.Timers;
using Hotshield.Data;

namespace Hotshield.Core
{
    public class PauseManager : IDisposable
    {
        private System.Timers.Timer? _timer;
        public bool IsPaused { get; private set; }
        public DateTime ResumeTime { get; private set; }
        public event EventHandler? PauseStateChanged;

        public void Pause(int minutes)
        {
            if (IsPaused) return;
            IsPaused = true;
            ResumeTime = DateTime.Now.AddMinutes(minutes);
            SettingsRepo.Set("pause_active", "true");
            SettingsRepo.Set("pause_resume_time", ResumeTime.ToString("o"));

            // Disable all active rules (but keep kill switch disabled)
            var ruleNames = new AppRuleRepo().GetAllFirewallRuleNames();
            foreach (var name in ruleNames)
                FirewallManager.SetRuleEnabled(name, false);

            _timer = new System.Timers.Timer(minutes * 60 * 1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = false;
            _timer.Start();

            PauseStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            SettingsRepo.Set("pause_active", "false");
            // Re‑apply rules for current network
            var engine = new RuleEngine(); // we'll wire the actual engine reference later
            // This is a simplified call; we'll integrate properly in the main tray code.

            PauseStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Resume();
        }

        public void Dispose() => _timer?.Dispose();
    }
}
