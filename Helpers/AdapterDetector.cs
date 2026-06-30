using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Hotshield.Helpers
{
    public static class AdapterDetector
    {
        public static bool IsUsbTetherActive()
        {
            return GetUsbTetherAdapter() != null;
        }

        public static string? GetUsbTetherAdapterName()
        {
            return GetUsbTetherAdapter()?.Name;
        }

        private static NetworkInterface? GetUsbTetherAdapter()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni =>
                    ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.Description.IndexOf("RNDIS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ni.Description.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ni.Description.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ni.Description.IndexOf("Apple Mobile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ni.Description.IndexOf("Remote NDIS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ni.Description.IndexOf("USB Ethernet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ni.Description.IndexOf("SAMSUNG Mobile", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        public static string GetActiveConnectionSource()
        {
            var usb = GetUsbTetherAdapter();
            if (usb != null)
                return $"USB Tether: {MapAdapterFriendlyName(usb)}";

            try
            {
                var ssid = WlanHelper.GetCurrentSsid();
                if (!string.IsNullOrEmpty(ssid))
                    return $"Wi-Fi: {ssid}";
            }
            catch { }

            var wifi = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                      ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
            if (wifi != null)
                return $"Wi-Fi: {wifi.Name}";

            var eth = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(ni => { try { return ni.Speed; } catch { return 0L; } })
                .FirstOrDefault();

            if (eth != null)
                return $"{eth.NetworkInterfaceType}: {MapAdapterFriendlyName(eth)}";

            return "No Network";
        }

        private static string MapAdapterFriendlyName(NetworkInterface ni)
        {
            try
            {
                var desc = ni.Description ?? ni.Name ?? "Adapter";
                var name = ni.Name ?? desc;

                var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "RNDIS", "USB Tether" },
                    { "Android", "Android Tether" },
                    { "iPhone", "iPhone Tether" },
                    { "Realtek", "Ethernet" },
                    { "Intel", "Ethernet" },
                    { "Qualcomm", "Wi-Fi" },
                    { "Broadcom", "Wi-Fi" },
                    { "Wireless", "Wi-Fi" },
                    { "Wi-Fi", "Wi-Fi" },
                    { "Ethernet", "Ethernet" }
                };

                foreach (var kv in mappings)
                {
                    if (desc.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                        return name;
                }

                return desc.Length > 0 ? desc : name;
            }
            catch
            {
                return ni.Name ?? "Adapter";
            }
        }

        /// <summary>
        /// Returns a stable identifier for non-WiFi connections (Ethernet, USB tether)
        /// using the adapter's MAC address. Allows non-WiFi connections to be tracked
        /// as networks in the same way as SSIDs.
        /// </summary>
        public static string GetNonWifiNetworkKey()
        {
            try
            {
                var active = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                         n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                         n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                                         n.NetworkInterfaceType != NetworkInterfaceType.Wireless80211);
                if (active != null)
                {
                    var mac = active.GetPhysicalAddress();
                    if (mac != null && mac.GetAddressBytes().Length > 0)
                    {
                        string macStr = string.Join("", mac.GetAddressBytes().Select(b => b.ToString("X2")));
                        return $"NET-{active.Name}-{macStr}";
                    }
                    return $"NET-{active.Name}";
                }
            }
            catch { }
            return "";
        }
    }
}