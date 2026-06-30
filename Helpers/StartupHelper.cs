using Microsoft.Win32;
using System.Windows.Forms;

namespace Hotshield.Helpers
{
    public static class StartupHelper
    {
        private const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "Hotshield";

        public static void Enable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
            key?.SetValue(AppName, Application.ExecutablePath);
        }
        public static void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
            key?.DeleteValue(AppName, false);
        }
        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
            return key?.GetValue(AppName) != null;
        }
    }
}
