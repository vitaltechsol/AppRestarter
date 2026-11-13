using System;
using System.Diagnostics;
using System.IO;

namespace AppRestarter
{
    internal static class FirewallHelper
    {
        private const string RuleNameTcp = "AppRestarter";
        private const string RuleNameUdp = "AppRestarter UDP";

        // Written once both rules are confirmed. Checked on every subsequent startup
        // so we skip the expensive netsh process spawns entirely.
        private static readonly string FlagFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "firewall_configured.flag");

        /// <summary>
        /// Ensures inbound firewall rules exist for AppRestarter's TCP and UDP ports.
        /// Skips all netsh calls after the first successful run (flag file check is free).
        /// Silently skips if the rules cannot be added due to insufficient privileges.
        /// </summary>
        public static void EnsureFirewallRules(int appPort, int webPort, int discoveryPort, Action<string> log = null)
        {
            if (File.Exists(FlagFile)) return;

            try
            {
                bool tcpOk = TcpRuleExists();
                if (!tcpOk)
                {
                    int code = RunNetsh(
                        $"advfirewall firewall add rule name=\"{RuleNameTcp}\" " +
                        $"dir=in action=allow protocol=TCP localport={appPort},{webPort} " +
                        $"profile=any description=\"AppRestarter inbound TCP\"");

                    tcpOk = code == 0;
                    if (tcpOk)
                        log?.Invoke($"Firewall: Added inbound TCP rule for ports {appPort},{webPort}.");
                    else
                        log?.Invoke("Firewall: Could not add TCP rule (run as administrator to configure automatically).");
                }

                bool udpOk = UdpRuleExists();
                if (!udpOk)
                {
                    int code = RunNetsh(
                        $"advfirewall firewall add rule name=\"{RuleNameUdp}\" " +
                        $"dir=in action=allow protocol=UDP localport={discoveryPort} " +
                        $"profile=any description=\"AppRestarter inbound UDP discovery\"");

                    udpOk = code == 0;
                    if (udpOk)
                        log?.Invoke($"Firewall: Added inbound UDP rule for port {discoveryPort}.");
                    else
                        log?.Invoke("Firewall: Could not add UDP rule (run as administrator to configure automatically).");
                }

                // Both rules confirmed — write the flag so future startups skip this entirely.
                if (tcpOk && udpOk)
                {
                    File.WriteAllText(FlagFile, DateTime.Now.ToString("O"));
                    log?.Invoke("Firewall: Rules confirmed. Future startups will skip this check.");
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Firewall: Setup skipped — {ex.Message}");
            }
        }

        private static bool TcpRuleExists()
        {
            return RunNetsh($"advfirewall firewall show rule name=\"{RuleNameTcp}\"") == 0;
        }

        private static bool UdpRuleExists()
        {
            return RunNetsh($"advfirewall firewall show rule name=\"{RuleNameUdp}\"") == 0;
        }

        private static int RunNetsh(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var proc = Process.Start(psi);
                proc.WaitForExit(5000);
                return proc.ExitCode;
            }
            catch
            {
                return -1;
            }
        }
    }
}
