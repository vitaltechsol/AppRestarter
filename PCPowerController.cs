using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AppRestarter
{
    public static class PcPowerController
    {
        public static Task ShutdownAsync(PcInfo pc, Action<string> log, int timeoutMs = 10000)
            => RunShutdownCommandAsync(pc, "/s /t 0 /f", log, timeoutMs);

        public static Task RestartAsync(PcInfo pc, Action<string> log, int timeoutMs = 10000)
            => RunShutdownCommandAsync(pc, "/r /t 0 /f", log, timeoutMs);

        private static async Task RunShutdownCommandAsync(
            PcInfo pc,
            string coreArgs,
            Action<string> log,
            int timeoutMs)
        {
            if (pc == null || string.IsNullOrWhiteSpace(pc.IP))
            {
                log?.Invoke("PC info is missing IP, cannot send shutdown/restart command.");
                return;
            }

            // Use Windows shutdown command against a remote computer
            string args = $"{coreArgs} /m \\\\{pc.IP}";
            var psi = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    log?.Invoke($"Failed to start shutdown.exe for {pc.Name} ({pc.IP}).");
                    return;
                }

                if (timeoutMs > 0)
                {
                    await Task.Run(() => proc.WaitForExit(timeoutMs));
                }

                log?.Invoke($"Sent command '{coreArgs}' to {pc.Name} ({pc.IP}).");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Error sending shutdown/restart to {pc.Name} ({pc.IP}): {ex.Message}");
            }
        }
    }
}
