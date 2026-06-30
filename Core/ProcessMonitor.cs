using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hotshield.Core
{
    public static class ProcessMonitor
    {
        public static List<ProcessInfo> GetNetworkProcesses()
        {
            var pidMap = GetPortToPidMap();
            var processes = Process.GetProcesses()
                .Where(p => pidMap.ContainsValue(p.Id))
                .Select(p =>
                {
                    try
                    {
                        return new ProcessInfo
                        {
                            ProcessName = p.ProcessName,
                            ExePath = p.MainModule?.FileName ?? "",
                            Pid = p.Id,
                            HasNetworkActivity = true
                        };
                    }
                    catch { return null; }
                })
                .Where(pi => pi != null && !string.IsNullOrEmpty(pi.ExePath))
                .DistinctBy(pi => pi!.ExePath)
                .ToList();
            return processes!;
        }

        private static Dictionary<int, int> GetPortToPidMap()
        {
            var map = new Dictionary<int, int>();
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                string output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit();

                foreach (string line in output.Split('\n'))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    string localAddr = parts[1];
                    int colon = localAddr.LastIndexOf(':');
                    if (colon < 0) continue;
                    if (!int.TryParse(localAddr.Substring(colon + 1), out int port)) continue;

                    if (int.TryParse(parts[^1], out int pid))
                        map[port] = pid;
                }
            }
            catch (Exception ex) { Logger.Log($"netstat error: {ex.Message}"); }
            return map;
        }
    }

    public class ProcessInfo
    {
        public string ProcessName { get; set; } = "";
        public string ExePath { get; set; } = "";
        public int Pid { get; set; }
        public bool HasNetworkActivity { get; set; }
    }
}
