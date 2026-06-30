using System;
using System.Text.RegularExpressions;

namespace Hotshield.Helpers
{
    /// <summary>
    /// Auto-generates user-friendly display names from raw SSIDs.
    /// e.g. "Tadzoka_MiFi_5G" → "Tadzoka MiFi 5G"
    ///      "AndroidAP_ABCD" → "Phone Hotspot"
    ///      "MTN-WiFi-4F2A" → "MTN WiFi"
    /// </summary>
    public static class NameFormatter
    {
        public static string GetDisplayName(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
                return "Unknown Network";

            // Check for USB tether indicators
            if (ssid.IndexOf("RNDIS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ssid.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ssid.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ssid.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ssid.IndexOf("Tether", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "USB Tether";
            }

            string name = ssid;

            // Remove trailing hex-like suffixes: _ABCD, -ABCD, #ABCD
            name = Regex.Replace(name, @"[_\-][A-Fa-f0-9]{4,8}$", "");
            name = Regex.Replace(name, @"[_\-][A-Fa-f0-9]{2}[:][A-Fa-f0-9]{2}[:][A-Fa-f0-9]{2}$", "");

            // Remove common suffixes
            name = Regex.Replace(name, @"[-_]\d[Gg]$", "");     // _5G, -2G
            name = Regex.Replace(name, @"[-_]\d[Gg][Hz]+$", ""); // _5GHz

            // Replace underscores and hyphens with spaces
            name = name.Replace('_', ' ');
            name = name.Replace('-', ' ');

            // Remove multiple spaces
            name = Regex.Replace(name, @"\s+", " ").Trim();

            // If name is too short or looks like a device ID, give a friendly fallback
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            {
                return ssid.Length > 8 ? ssid.Substring(0, 8) + "..." : $"WiFi ({ssid})";
            }

            // Capitalize first letter of each word
            var words = name.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
        }
    }
}