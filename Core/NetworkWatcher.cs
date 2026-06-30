using System;
using System.Net.NetworkInformation;

namespace Hotshield.Core
{
    public class NetworkWatcher : IDisposable
    {
        /// <summary>
        /// Fires with the raw SSID (WiFi name) when detected, or empty string when disconnected.
        /// </summary>
        public event EventHandler<string>? NetworkChanged;
        private string _lastSsid = "";

        public NetworkWatcher()
        {
            NetworkChange.NetworkAddressChanged += OnChange;
            NetworkChange.NetworkAvailabilityChanged += OnChange;
        }

        private void OnChange(object? s, EventArgs e)
        {
            string current = Helpers.WlanHelper.GetCurrentSsid();

            // If no WiFi SSID, check USB tether
            if (string.IsNullOrEmpty(current))
            {
                string? tetherName = Helpers.AdapterDetector.GetUsbTetherAdapterName();
                if (!string.IsNullOrEmpty(tetherName))
                    current = tetherName; // use adapter name as identifier
            }

            if (current != _lastSsid)
            {
                _lastSsid = current;
                NetworkChanged?.Invoke(this, current);
            }
        }

        public void Dispose()
        {
            NetworkChange.NetworkAddressChanged -= OnChange;
            NetworkChange.NetworkAvailabilityChanged -= OnChange;
        }
    }
}