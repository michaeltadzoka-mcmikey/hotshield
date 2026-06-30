using System;
using System.Security.Principal;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Hotshield.Core;

namespace Hotshield
{
    static class Program
    {
        private const string PipeName = "HotshieldDashboardPipe";

        [STAThread]
        static void Main()
        {
            // Log immediately on process start so we can diagnose startup/elevation failures
            try { Logger.Log($"Hotshield invoked by {WindowsIdentity.GetCurrent()?.Name}"); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Proactive elevation check (app.manifest handles the UAC prompt)
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show(
                    "Hotshield needs administrator permission to control which apps use your data.\n\n" +
                    "Please restart: Right-click the app → 'Run as administrator'.",
                    "Hotshield — Administrator Permission Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var mutex = new System.Threading.Mutex(true, "HotshieldSingleInstance", out bool isNew);
                if (!isNew)
                {
                    // Another instance is running - signal it to show dashboard
                    Logger.Log("Another instance detected - signaling to show dashboard");
                    SignalExistingInstance();
                    return;
                }

                Logger.Log("Hotshield starting");
                try
                {
                    Data.Database.Initialise();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Database.Init error: {ex.Message}");
                    MessageBox.Show($"Database initialization failed: {ex.Message}", "Hotshield", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    var trayIcon = new UI.TrayIcon();
                    // Show dashboard immediately on startup (user clicked the exe)
                    trayIcon.ShowDashboardAsync();
                    
                    // Start listening for dashboard show requests
                    StartPipeServer(trayIcon);
                    Application.Run(trayIcon);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Application.Run error: {ex}");
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Hotshield", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                try { Logger.Log($"Startup fatal error: {ex}"); } catch { }
                MessageBox.Show($"Startup failed: {ex.Message}", "Hotshield", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void SignalExistingInstance()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000); // 1 second timeout
                using var writer = new StreamWriter(client);
                writer.WriteLine("SHOW_DASHBOARD");
                writer.Flush();
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to signal existing instance: {ex.Message}");
                MessageBox.Show("Hotshield is already running. Check the system tray.",
                    "Hotshield", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static void StartPipeServer(UI.TrayIcon trayIcon)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                        await server.WaitForConnectionAsync();
                        using var reader = new StreamReader(server);
                        string? message = await reader.ReadLineAsync();
                        
                        if (message == "SHOW_DASHBOARD")
                        {
                            // Invoke on UI thread
                            trayIcon.ShowDashboardFromExternal();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Pipe server error: {ex.Message}");
                    }
                }
            });
        }
    }
}