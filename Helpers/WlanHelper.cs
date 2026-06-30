using System;
using System.Diagnostics;
using System.Linq;

namespace Hotshield.Helpers
{
    public static class WlanHelper
    {
        public static string GetCurrentSsid()
        {
            string ssid = GetSsidViaNetsh();
            return string.IsNullOrEmpty(ssid) ? "" : ssid;
        }

        private static string GetSsidViaNetsh()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return "";

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Handle both English and localized Windows with different line endings
                var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Match lines like "SSID" or "SSID" but NOT "BSSID"
                    // Handle both "SSID : value" and "SSID:value" formats
                    if (trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) &&
                        !trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase) &&
                        !trimmed.StartsWith("SSID Broadcast", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find the colon - handle cases with or without spaces
                        int colon = trimmed.IndexOf(':');
                        if (colon >= 0 && colon < trimmed.Length - 1)
                        {
                            string value = trimmed.Substring(colon + 1).Trim();
                            // Remove quotes if present
                            value = value.Trim('"').Trim('\'');
                            return value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Log($"Netsh SSID error: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// Checks if the WiFi adapter exists and is connected to any network.
        /// Returns true even if SSID is not yet assigned (connecting state).
        /// </summary>
        public static bool IsWifiAdapterAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If output contains "SSID" anywhere (excluding error messages), WiFi is available
                return output.Contains("SSID", StringComparison.OrdinalIgnoreCase) &&
                       !output.Contains("There is no", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}