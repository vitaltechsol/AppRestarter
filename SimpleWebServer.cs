using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class SimpleWebServer
    {
        private readonly List<ApplicationDetails> _apps;
        private readonly Action<string> _logAction;
        private HttpListener _httpListener;
        private volatile bool _running = true;
        private readonly string _htmlFilePath;
        private int _port;

        public SimpleWebServer(List<ApplicationDetails> apps, Action<string> logAction, string htmlFilePath)
        {
            _apps = apps ?? throw new ArgumentNullException(nameof(apps));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
            _htmlFilePath = htmlFilePath;
        }

        public void Start(int port = 8080)
        {
            _port = port;
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://+:{port}/");
            try
            {
                _httpListener.Start();
                _logAction($"HTTP server started on port {port}");
                Task.Run(() => ServerLoop());
            }
            catch (Exception ex)
            {
                _logAction($"Failed to start HTTP server: {ex.Message}");
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
                    else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/apps")
                    {
                        var appsToSend = _apps.Select(app => new
                        {
                            app.Name,
                            app.ProcessName,
                            app.RestartPath,
                            app.ClientIP
                        }).ToList();

                        var json = JsonSerializer.Serialize(appsToSend);
                        var buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.Close();
                    }
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/restart")
                    {
                        using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var restartRequest = JsonSerializer.Deserialize<RestartRequest>(body);

                        var app = _apps.FirstOrDefault(a => a.ProcessName == restartRequest?.ProcessName);
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

        public event EventHandler<ApplicationDetails> RestartRequested;

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
        public string ProcessName { get; set; }
    }

}
