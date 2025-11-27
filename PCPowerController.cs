using System;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace AppRestarter
{
    public static class PcPowerController
    {
        public static Task RestartAsync(PcInfo pc, int port, Action<string> log)
            => SendPcCommand(pc, port, log, RemoteActionType.PcRestart);

        public static Task ShutdownAsync(PcInfo pc, int port, Action<string> log)
            => SendPcCommand(pc, port, log, RemoteActionType.PcShutdown);

        private static async Task SendPcCommand(PcInfo pc, int port, Action<string> log, RemoteActionType action)
        {
            try
            {
                log?.Invoke($"Sending {action} to {pc.Name} ({pc.IP})");

                using var client = new TcpClient(pc.IP, port)
                {
                    SendTimeout = 3000
                };
                using var stream = client.GetStream();

                var msg = new ApplicationDetails
                {
                    ActionType = action,
                    Name = pc.Name,
                    ClientIP = pc.IP
                };

                var serializer = new DataContractSerializer(typeof(ApplicationDetails));
                serializer.WriteObject(stream, msg);
                stream.Flush();

                log?.Invoke($"{action} TCP command sent to {pc.Name}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"Failed sending PC {action} to {pc.Name} ({pc.IP}): {ex.Message}");
            }
        }
    }
}
