using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppRestarter
{
    /// <summary>
    /// UDP-based LAN discovery service.
    /// Call <see cref="StartListening"/> once at app startup so this machine responds to
    /// discovery broadcasts from other AppRestarter instances.
    /// Call <see cref="DiscoverPeersAsync"/> (static) to scan for other machines.
    /// </summary>
    public sealed class PcDiscoveryService : IDisposable
    {
        public const int DiscoveryPort = 2026;

        private const string RequestMessage  = "APPRESTARTERDISC";
        private const string ResponsePrefix  = "APPRESTARTERHERE|";

        private UdpClient _listener;
        private CancellationTokenSource _cts;
        private bool _disposed;

        // ------------------------------------------------------------------ listener

        /// <summary>
        /// Starts listening for discovery broadcasts on <see cref="DiscoveryPort"/>.
        /// When a discover request arrives this machine replies with its hostname and IP.
        /// </summary>
        public void StartListening()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new UdpClient(DiscoveryPort) { EnableBroadcast = true };
                _ = ListenLoopAsync(_cts.Token);
            }
            catch
            {
                // Port already in use or insufficient permissions — silently skip.
            }
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await _listener.ReceiveAsync(ct);
                    var msg = Encoding.UTF8.GetString(result.Buffer);

                    if (msg == RequestMessage)
                    {
                        var hostname = Dns.GetHostName();
                        var localIp  = GetLocalIPAddress();
                        var response = Encoding.UTF8.GetBytes($"{ResponsePrefix}{hostname}|{localIp}");
                        await _listener.SendAsync(response, response.Length, result.RemoteEndPoint);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* network hiccup — keep listening */ }
            }
        }

        // ------------------------------------------------------------------ scanner (static)

        /// <summary>
        /// Broadcasts a discovery request and collects responses from AppRestarter peers.
        /// Automatically excludes this machine's own IP.
        /// </summary>
        /// <param name="timeoutMs">How long to wait for replies (default 2500 ms).</param>
        public static async Task<List<(string Name, string IP)>> DiscoverPeersAsync(int timeoutMs = 2500)
        {
            var results  = new List<(string Name, string IP)>();
            var localIp  = GetLocalIPAddress();

            UdpClient scanner = null;
            try
            {
                scanner = new UdpClient();
                scanner.EnableBroadcast = true;
                scanner.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

                var request = Encoding.UTF8.GetBytes(RequestMessage);
                await scanner.SendAsync(request, request.Length,
                    new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                using var cts = new CancellationTokenSource(timeoutMs);
                try
                {
                    while (true)
                    {
                        var result = await scanner.ReceiveAsync(cts.Token);
                        var text   = Encoding.UTF8.GetString(result.Buffer);

                        if (text.StartsWith(ResponsePrefix))
                        {
                            var payload = text.Substring(ResponsePrefix.Length).Split('|');
                            if (payload.Length >= 2)
                            {
                                var name = payload[0];
                                var ip   = payload[1];

                                // Skip self and duplicates
                                if (!string.Equals(ip, localIp, StringComparison.OrdinalIgnoreCase)
                                    && !results.Exists(r => r.IP == ip))
                                {
                                    results.Add((name, ip));
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { /* timeout — expected */ }
            }
            catch { /* broadcast not supported on this adapter, etc. */ }
            finally
            {
                scanner?.Dispose();
            }

            return results;
        }

        // ------------------------------------------------------------------ helpers

        internal static string GetLocalIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // ------------------------------------------------------------------ IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _listener?.Close();
            _listener?.Dispose();
        }
    }
}
