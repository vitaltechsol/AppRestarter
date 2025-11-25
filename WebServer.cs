using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRestarter
{
    public class WebServer
    {
        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly Action<string> _logAction;
        private HttpListener _httpListener;
        private volatile bool _running = true;
        private readonly string _htmlFilePath;
        private int _port;

        public WebServer(List<ApplicationDetails> apps,
                         List<PcInfo> pcs,
                         Action<string> logAction,
                         string htmlFilePath)
        {
            _apps = apps ?? throw new ArgumentNullException(nameof(apps));
            _pcs = pcs ?? throw new ArgumentNullException(nameof(pcs));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));

            // If relative, resolve to the EXE directory so auto-start scenarios work
            if (!Path.IsPathRooted(htmlFilePath))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                htmlFilePath = Path.Combine(baseDir, htmlFilePath);
            }

            _htmlFilePath = htmlFilePath;
        }

        public void Start(int port = 8090)
        {
            _port = port;
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://+:{port}/");
            try
            {
                _httpListener.Start();
                _logAction($"Web Server started on port {port}. index at: {_htmlFilePath}");
                Task.Run(() => ServerLoop());
            }
            catch (Exception ex)
            {
                _logAction($"Failed to start Web Server: {ex.Message}. Make sure this app is running in administrator mode");
            }
        }

        public void Stop()
        {
            _running = false;
            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch { }
        }

        private async Task ServerLoop()
        {
            while (_running)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    // ---- ROOT: serve SPA ----
                    if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/")
                    {
                        // Serve the raw index.html file as-is
                        if (File.Exists(_htmlFilePath))
                        {
                            byte[] buffer = await File.ReadAllBytesAsync(_htmlFilePath);
                            response.ContentType = "text/html; charset=utf-8";
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.OutputStream.Close();
                        }
                        else
                        {
                            response.StatusCode = 404;
                            byte[] buffer = Encoding.UTF8.GetBytes("index.html not found.");
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.OutputStream.Close();
                        }
                    }
                    // ---- APPS: list ----
                    else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/apps")
                    {
                        var appsToSend = _apps.Select(app => new
                        {
                            app.Name,
                            app.ProcessName,
                            app.RestartPath,
                            app.ClientIP,
                            app.NoWarn,
                            app.GroupName
                        }).ToList();

                        var json = JsonSerializer.Serialize(appsToSend);
                        var buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.Close();
                    }
                    // ---- APPS: restart single ----
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/restart")
                    {
                        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var restartRequest = JsonSerializer.Deserialize<RestartRequest>(body);

                        var app = _apps.FirstOrDefault(a => a.Name == restartRequest?.Name);
                        if (app != null)
                        {
                            // Fire the restart event/callback for the app
                            RestartRequested?.Invoke(this, app);

                            response.StatusCode = 200;
                            var respBuf = Encoding.UTF8.GetBytes("Restart triggered");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                        }
                        else
                        {
                            response.StatusCode = 404;
                            var respBuf = Encoding.UTF8.GetBytes("App not found");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                        }
                        response.Close();
                    }
                    // ---- APPS: stop single ----
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/stop")
                    {
                        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var stopRequest = JsonSerializer.Deserialize<RestartRequest>(body);

                        var app = _apps.FirstOrDefault(a => a.Name == stopRequest?.Name);
                        if (app != null)
                        {
                            // Fire the stop event/callback for the app
                            StopRequested?.Invoke(this, app);

                            response.StatusCode = 200;
                            var respBuf = Encoding.UTF8.GetBytes("Stop triggered");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                        }
                        else
                        {
                            response.StatusCode = 404;
                            var respBuf = Encoding.UTF8.GetBytes("App not found");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                        }
                        response.Close();
                    }
                    // ---- APPS: restart group ----
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/restart-group")
                    {
                        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var groupRequest = JsonSerializer.Deserialize<RestartGroupRequest>(body);

                        var groupName = groupRequest?.GroupName;
                        if (string.IsNullOrWhiteSpace(groupName))
                        {
                            response.StatusCode = 400;
                            var respBuf = Encoding.UTF8.GetBytes("Missing GroupName");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                            response.Close();
                            continue;
                        }

                        var appsInGroup = _apps
                            .Where(a => string.Equals(a.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (!appsInGroup.Any())
                        {
                            response.StatusCode = 404;
                            var respBuf = Encoding.UTF8.GetBytes("No apps found in group");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                            response.Close();
                            continue;
                        }

                        foreach (var app in appsInGroup)
                        {
                            RestartRequested?.Invoke(this, app);
                        }

                        response.StatusCode = 200;
                        var successMsg = $"Restart triggered for {appsInGroup.Count} app(s) in group '{groupName}'.";
                        var successBuf = Encoding.UTF8.GetBytes(successMsg);
                        response.ContentLength64 = successBuf.Length;
                        await response.OutputStream.WriteAsync(successBuf, 0, successBuf.Length);
                        response.Close();
                    }
                    // ---- PCS: list ----
                    else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/pcs")
                    {
                        var pcsToSend = _pcs.Select(pc => new
                        {
                            pc.Name,
                            pc.IP
                        }).ToList();

                        var json = JsonSerializer.Serialize(pcsToSend);
                        var buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.Close();
                    }
                    // ---- PCS: shutdown ----
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/pc/shutdown")
                    {
                        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var pcReq = JsonSerializer.Deserialize<PcRequest>(body);

                        var pc = FindPc(pcReq);
                        if (pc == null)
                        {
                            response.StatusCode = 404;
                            var respBuf = Encoding.UTF8.GetBytes("PC not found");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                            response.Close();
                            continue;
                        }

                        await PcPowerController.ShutdownAsync(pc, _logAction);
                        response.StatusCode = 200;
                        var okBuf = Encoding.UTF8.GetBytes("Shutdown command sent");
                        response.ContentLength64 = okBuf.Length;
                        await response.OutputStream.WriteAsync(okBuf, 0, okBuf.Length);
                        response.Close();
                    }
                    // ---- PCS: restart ----
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/pc/restart")
                    {
                        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var pcReq = JsonSerializer.Deserialize<PcRequest>(body);

                        var pc = FindPc(pcReq);
                        if (pc == null)
                        {
                            response.StatusCode = 404;
                            var respBuf = Encoding.UTF8.GetBytes("PC not found");
                            response.ContentLength64 = respBuf.Length;
                            await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length);
                            response.Close();
                            continue;
                        }

                        await PcPowerController.RestartAsync(pc, _logAction);
                        response.StatusCode = 200;
                        var okBuf = Encoding.UTF8.GetBytes("Restart command sent");
                        response.ContentLength64 = okBuf.Length;
                        await response.OutputStream.WriteAsync(okBuf, 0, okBuf.Length);
                        response.Close();
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.Close();
                    }
                }
                catch (HttpListenerException)
                {
                    // Listener stopped, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logAction("HTTP Server error: " + ex.Message);
                }
            }
        }

        private PcInfo FindPc(PcRequest pcReq)
        {
            if (pcReq == null) return null;

            PcInfo pc = null;

            if (!string.IsNullOrWhiteSpace(pcReq.Name))
            {
                pc = _pcs.FirstOrDefault(p =>
                    string.Equals(p.Name, pcReq.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (pc == null && !string.IsNullOrWhiteSpace(pcReq.IP))
            {
                pc = _pcs.FirstOrDefault(p =>
                    string.Equals(p.IP, pcReq.IP, StringComparison.OrdinalIgnoreCase));
            }

            return pc;
        }

        public event EventHandler<ApplicationDetails> RestartRequested;
        public event EventHandler<ApplicationDetails> StopRequested;

        public void OpenWebInterfaceInBrowser()
        {
            try
            {
                string ipAddress = GetLocalIPv4();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    _logAction("No valid IPv4 address found.");
                    return;
                }

                string url = $"http://{ipAddress}:{_port}/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                _logAction($"Opened web interface: {url}");
            }
            catch (Exception ex)
            {
                _logAction($"Failed to open web interface: {ex.Message}");
            }
        }

        private string GetLocalIPv4()
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    var props = ni.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
            return null;
        }
    }

    public class RestartRequest
    {
        public string Name { get; set; }
    }

    public class RestartGroupRequest
    {
        public string GroupName { get; set; }
    }

    public class PcRequest
    {
        public string Name { get; set; }
        public string IP { get; set; }
    }
}
