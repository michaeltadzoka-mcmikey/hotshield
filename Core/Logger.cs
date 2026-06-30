using System;
using System.IO;

namespace Hotshield.Core
{
    public static class Logger
    {
        private static readonly string UserLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Hotshield", "hotshield.log");

        private static readonly string CommonLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Hotshield", "hotshield.log");

        public static void Log(string message)
        {
            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}" + Environment.NewLine;
            // Try user AppData first, then fall back to CommonApplicationData.
            if (TryAppend(UserLogPath, entry))
                return;

            TryAppend(CommonLogPath, entry);
        }

        private static bool TryAppend(string path, string entry)
        {
            try
            {
                var dir = Path.GetDirectoryName(path)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(path, entry);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
