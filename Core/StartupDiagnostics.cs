using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Hotshield.Core
{
    internal static class StartupDiagnostics
    {
        [ModuleInitializer]
        public static void Init()
        {
            try
            {
                Logger.Log($"ModuleInitializer invoked. PID={Environment.ProcessId}; User={Environment.UserName}");
            }
            catch { }

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { Logger.Log($"UnhandledException: {e.ExceptionObject}"); } catch { }
            };

            Application.ThreadException += (s, e) =>
            {
                try { Logger.Log($"ThreadException: {e.Exception}"); } catch { }
            };
        }
    }
}
