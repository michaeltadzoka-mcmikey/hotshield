using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hotshield.Core
{
    public static class FirewallManager
    {
        private const string RulePrefix = "Hotshield_";
        private const string KillSwitchBlockRuleName = "Hotshield_KillSwitch";
        private const string KillSwitchAllowPrefix = "Hotshield_KS_Allow_";

        public static string AddRule(int ruleId, string appName, string exePath,
                                     string action, string direction = "out")
        {
            string ruleName = $"{RulePrefix}{ruleId}";
            string actionArg = action == "block" ? "block" : "allow";
            string args = $"advfirewall firewall add rule " +
                         $"name=\"{ruleName}\" " +
                         $"dir={direction} " +
                         $"program=\"{exePath}\" " +
                         $"action={actionArg} " +
                         $"enable=yes " +
                         $"profile=any";
            if (!RunNetsh(args))
                throw new InvalidOperationException($"Failed to add firewall rule: {ruleName}");
            return ruleName;
        }

        public static string AddServiceRule(int ruleId, string appName, string serviceName,
                                            string action, string direction = "out")
        {
            string ruleName = $"{RulePrefix}{ruleId}";
            string actionArg = action == "block" ? "block" : "allow";
            string args = $"advfirewall firewall add rule " +
                         $"name=\"{ruleName}\" " +
                         $"dir={direction} " +
                         $"program=\"%SystemRoot%\\system32\\svchost.exe\" " +
                         $"service={serviceName} " +
                         $"action={actionArg} " +
                         $"enable=yes " +
                         $"profile=any";
            if (!RunNetsh(args))
                throw new InvalidOperationException($"Failed to add service firewall rule: {ruleName}");
            return ruleName;
        }

        public static bool SetRuleEnabled(string ruleName, bool enable)
        {
            string toggle = enable ? "yes" : "no";
            string args = $"advfirewall firewall set rule name=\"{ruleName}\" new enable={toggle}";
            return RunNetsh(args);
        }

        public static bool RemoveRule(string ruleName)
        {
            string args = $"advfirewall firewall delete rule name=\"{ruleName}\"";
            return RunNetsh(args);
        }

        public static void RemoveAllHotshieldRules(IEnumerable<string> ruleNames)
        {
            foreach (var name in ruleNames)
                if (!string.IsNullOrEmpty(name))
                    RemoveRule(name);
        }

        // Add a BLANKET BLOCK rule — blocks ALL outbound traffic (Fix 4: ras added for USB tether)
        public static string AddBlockRule(string ruleName)
        {
            RunNetsh($"advfirewall firewall delete rule name=\"{ruleName}\"");
            string args = $"advfirewall firewall add rule " +
                         $"name=\"{ruleName}\" " +
                         $"dir=out " +
                         $"protocol=any " +
                         $"interfacetype=wireless,lan,ras " +
                         $"action=block " +
                         $"enable=yes " +
                         $"profile=any";
            if (!RunNetsh(args))
                throw new InvalidOperationException($"Failed to add block rule: {ruleName}");
            return ruleName;
        }

        public static bool SetBlockRuleEnabled(string ruleName, bool enable)
        {
            return SetRuleEnabled(ruleName, enable);
        }

        public static bool EnableKillSwitch(List<string>? whitelistExePaths = null)
        {
            DisableKillSwitch();
            if (whitelistExePaths != null)
            {
                for (int i = 0; i < whitelistExePaths.Count; i++)
                {
                    string ruleName = $"{KillSwitchAllowPrefix}{i}";
                    string args = $"advfirewall firewall add rule name=\"{ruleName}\" dir=out program=\"{whitelistExePaths[i]}\" action=allow enable=yes profile=any";
                    if (!RunNetsh(args)) return false;
                }
            }
            string blockArgs = $"advfirewall firewall add rule name=\"{KillSwitchBlockRuleName}\" dir=out action=block enable=yes profile=any";
            return RunNetsh(blockArgs);
        }

        public static bool DisableKillSwitch()
        {
            bool ok = true;
            ok &= RunNetsh($"advfirewall firewall delete rule name=\"{KillSwitchBlockRuleName}\"");
            var allowRules = GetRuleNamesByPrefix(KillSwitchAllowPrefix);
            foreach (var name in allowRules) ok &= RemoveRule(name);
            return ok;
        }

        private static List<string> GetRuleNamesByPrefix(string prefix)
        {
            var list = new List<string>();
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall show rule name=all verbose",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                string output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit();
                foreach (string line in output.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("Rule Name:", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = trimmed.Substring(10).Trim();
                        if (name.StartsWith(prefix)) list.Add(name);
                    }
                }
            }
            catch { }
            return list;
        }

        public static bool VerifyRuleExists(string ruleName)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                string output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit();
                return output.Contains("Rule Name:", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static bool RunNetsh(string args)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                p?.WaitForExit();
                return p?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.Log($"netsh error: {ex.Message}");
                return false;
            }
        }
    }
}