using System;
using System.Diagnostics;

namespace AppRestarter
{
    internal static class FirewallHelper
    {
        private const string RuleNameTcp = "AppRestarter";
        private const string RuleNameUdp = "AppRestarter UDP";

        /// <summary>
        /// Ensures inbound firewall rules exist for AppRestarter's TCP and UDP ports.
        /// Silently skips if the rules already exist or if the process lacks privileges.
        /// </summary>
        public static void EnsureFirewallRules(int appPort, int webPort, int discoveryPort, Action<string> log = null)
        {
            try
            {
                if (!TcpRuleExists())
                {
                    int code = RunNetsh(
                        $"advfirewall firewall add rule name=\"{RuleNameTcp}\" " +
                        $"dir=in action=allow protocol=TCP localport={appPort},{webPort} " +
                        $"profile=any description=\"AppRestarter inbound TCP\"");

                    if (code == 0)
                        log?.Invoke($"Firewall: Added inbound TCP rule for ports {appPort},{webPort}.");
                    else
                        log?.Invoke("Firewall: Could not add TCP rule (run as administrator to configure automatically).");
                }

                if (!UdpRuleExists())
                {
                    int code = RunNetsh(
                        $"advfirewall firewall add rule name=\"{RuleNameUdp}\" " +
                        $"dir=in action=allow protocol=UDP localport={discoveryPort} " +
                        $"profile=any description=\"AppRestarter inbound UDP discovery\"");

                    if (code == 0)
                        log?.Invoke($"Firewall: Added inbound UDP rule for port {discoveryPort}.");
                    else
                        log?.Invoke("Firewall: Could not add UDP rule (run as administrator to configure automatically).");
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
