using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AppRestarter
{
    public class WebServer
    {
        private readonly List<ApplicationDetails> _apps;
        private readonly List<PcInfo> _pcs;
        private readonly AppSettings _settings;
        private readonly Action<string> _logAction;
        private HttpListener _httpListener;
        private volatile bool _running = true;
        private readonly string _htmlFilePath;
        private int _port;

        // Optional provider for app status snapshots (color/running) for the web UI.
        private readonly Func<List<AppStatusManager.AppWebStatus>> _statusProvider;

        // WebSocket clients for realtime status updates
        private readonly List<WebSocket> _wsClients = new List<WebSocket>();
        private readonly object _wsLock = new object();
        private System.Threading.Timer _wsBroadcastTimer;

        // Used to cancel websocket receive loops + background timers when stopping.
        private CancellationTokenSource _serverCts = new CancellationTokenSource();

        public WebServer(
            List<ApplicationDetails> apps,
            List<PcInfo> pcs,
            Action<string> logAction,
            string htmlFilePath,
            AppSettings settings,
            Func<List<AppStatusManager.AppWebStatus>> statusProvider = null)
        {
            _apps = apps ?? throw new ArgumentNullException(nameof(apps));
            _pcs = pcs ?? throw new ArgumentNullException(nameof(pcs));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _statusProvider = statusProvider;

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
            // If Start is called again for any reason, shut down the previous listener/sockets first.
            try { Stop(); } catch { }

            _running = true;
            _port = port;

            _serverCts = new CancellationTokenSource();

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://+:{port}/");

            try
            {
                _httpListener.Start();

                // Periodically broadcast app statuses to websocket clients (best-effort realtime)
                _wsBroadcastTimer = new System.Threading.Timer(async _ =>
                {
                    try
                    {
                        if (!_running) return;
                        await BroadcastStatusToWebSocketsAsync();
                    }
                    catch { }
                }, null, dueTime: 1000, period: 2000);

                _logAction($"Web Server started on port {port}. index at: {_htmlFilePath}");

                // Important: accept loop only accepts; each request handled concurrently.
                Task.Run(() => ServerLoop(_serverCts.Token));
            }
            catch (Exception ex)
            {
                _logAction($"Failed to start Web Server: {ex.Message}. Make sure this app is running in administrator mode");
            }
        }

        public void Stop()
        {
            _running = false;

            try { _serverCts?.Cancel(); } catch { }

            try { _wsBroadcastTimer?.Dispose(); } catch { }
            _wsBroadcastTimer = null;

            // Close websocket clients so browsers disconnect immediately (instead of hanging).
            try { CloseAllWebSockets(); } catch { }

            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch { }
            finally
            {
                _httpListener = null;
            }
        }

        private void CloseAllWebSockets()
        {
            List<WebSocket> clients;
            lock (_wsLock)
            {
                clients = _wsClients.ToList();
                _wsClients.Clear();
            }

            if (clients.Count == 0) return;

            foreach (var ws in clients)
            {
                try
                {
                    if (ws == null) continue;

                    // Best-effort close handshake, then abort to guarantee immediate shutdown.
                    if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", cts.Token).Wait(1000);
                        }
                        catch { }
                    }

                    try { ws.Abort(); } catch { }
                    try { ws.Dispose(); } catch { }
                }
                catch { }
            }
        }

        private async Task ServerLoop(CancellationToken token)
        {
            while (_running && !token.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    var listener = _httpListener;
                    if (listener == null || !listener.IsListening)
                    {
                        await Task.Delay(50, token).ConfigureAwait(false);
                        continue;
                    }

                    context = await listener.GetContextAsync().ConfigureAwait(false);

                    // Handle every request concurrently so a websocket does NOT block /app-status.
                    _ = Task.Run(() => HandleContextAsync(context, token));
                }
                catch (HttpListenerException)
                {
                    // Listener stopped, exit gracefully
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logAction("HTTP Server accept-loop error: " + ex.Message);
                    try { await Task.Delay(100, token).ConfigureAwait(false); } catch { }
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext context, CancellationToken token)
        {
            var request = context.Request;
            var response = context.Response;

            // For HTTP routes we always close response; for WS routes, the WS owns the stream.
            bool isWebSocket = false;

            try
            {
                var path = request.Url.AbsolutePath;

                // ---- ROOT: serve SPA ----
                if (request.HttpMethod == "GET" && path == "/")
                {
                    if (File.Exists(_htmlFilePath))
                    {
                        byte[] buffer = await File.ReadAllBytesAsync(_htmlFilePath).ConfigureAwait(false);
                        response.ContentType = "text/html; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    }
                    else
                    {
                        response.StatusCode = 404;
                        byte[] buffer = Encoding.UTF8.GetBytes("index.html not found.");
                        response.ContentType = "text/plain; charset=utf-8";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    }
                    return;
                }

                // ---- APP STATUS: JSON snapshot for web UI ----
                if (request.HttpMethod == "GET" && path == "/app-status")
                {
                    var statuses = _statusProvider?.Invoke() ?? new List<AppStatusManager.AppWebStatus>();
                    var json = JsonSerializer.Serialize(statuses);
                    var buffer = Encoding.UTF8.GetBytes(json);

                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    return;
                }

                // ---- WEBSOCKET: realtime status stream ----
                if (path == "/ws" && request.IsWebSocketRequest)
                {
                    isWebSocket = true;

                    HttpListenerWebSocketContext wsContext;
                    try
                    {
                        wsContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                    }
                    catch
                    {
                        response.StatusCode = 500;
                        return;
                    }

                    var ws = wsContext.WebSocket;

                    lock (_wsLock) _wsClients.Add(ws);

                    // Send initial snapshot immediately
                    try { await SendStatusSnapshotAsync(ws).ConfigureAwait(false); } catch { }

                    var recvBuf = new byte[1024];
                    try
                    {
                        while (_running && ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                        {
                            // cancellation makes sure this loop ends when app exits
                            var result = await ws.ReceiveAsync(new ArraySegment<byte>(recvBuf), token).ConfigureAwait(false);
                            if (result.MessageType == WebSocketMessageType.Close)
                                break;
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                    finally
                    {
                        lock (_wsLock) _wsClients.Remove(ws);

                        try
                        {
                            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                            {
                                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token).ConfigureAwait(false);
                            }
                        }
                        catch { }

                        try { ws.Abort(); } catch { }
                        try { ws.Dispose(); } catch { }
                    }

                    return;
                }

                // ---- APPS: list ----
                if (request.HttpMethod == "GET" && path == "/apps")
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
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    return;
                }

                // ---- APPS: restart single ----
                if (request.HttpMethod == "POST" && path == "/restart")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var restartRequest = JsonSerializer.Deserialize<RestartRequest>(body);

                    var app = _apps.FirstOrDefault(a => a.Name == restartRequest?.Name);
                    if (app != null)
                    {
                        RestartRequested?.Invoke(this, app);
                        response.StatusCode = 200;
                        var respBuf = Encoding.UTF8.GetBytes("Restart triggered");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                    }
                    else
                    {
                        response.StatusCode = 404;
                        var respBuf = Encoding.UTF8.GetBytes("App not found");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                    }
                    return;
                }

                // ---- APPS: stop single ----
                if (request.HttpMethod == "POST" && path == "/stop")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var stopRequest = JsonSerializer.Deserialize<RestartRequest>(body);

                    var app = _apps.FirstOrDefault(a => a.Name == stopRequest?.Name);
                    if (app != null)
                    {
                        StopRequested?.Invoke(this, app);
                        response.StatusCode = 200;
                        var respBuf = Encoding.UTF8.GetBytes("Stop triggered");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                    }
                    else
                    {
                        response.StatusCode = 404;
                        var respBuf = Encoding.UTF8.GetBytes("App not found");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                    }
                    return;
                }

                // ---- APPS: restart group ----
                if (request.HttpMethod == "POST" && path == "/restart-group")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var groupRequest = JsonSerializer.Deserialize<RestartGroupRequest>(body);

                    var groupName = groupRequest?.GroupName;
                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        response.StatusCode = 400;
                        var respBuf = Encoding.UTF8.GetBytes("Missing GroupName");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                        return;
                    }

                    var appsInGroup = _apps
                        .Where(a => string.Equals(a.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (!appsInGroup.Any())
                    {
                        response.StatusCode = 404;
                        var respBuf = Encoding.UTF8.GetBytes("No apps found in group");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                        return;
                    }

                    foreach (var app in appsInGroup)
                        RestartRequested?.Invoke(this, app);

                    response.StatusCode = 200;
                    var successMsg = $"Restart triggered for {appsInGroup.Count} app(s) in group '{groupName}'.";
                    var successBuf = Encoding.UTF8.GetBytes(successMsg);
                    response.ContentLength64 = successBuf.Length;
                    await response.OutputStream.WriteAsync(successBuf, 0, successBuf.Length).ConfigureAwait(false);
                    return;
                }

                // ---- PCS: list ----
                if (request.HttpMethod == "GET" && path == "/pcs")
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
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    return;
                }

                // ---- PCS: shutdown ----
                if (request.HttpMethod == "POST" && path == "/pc/shutdown")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var pcReq = JsonSerializer.Deserialize<PcRequest>(body);

                    var pc = FindPc(pcReq);
                    if (pc == null)
                    {
                        response.StatusCode = 404;
                        var respBuf = Encoding.UTF8.GetBytes("PC not found");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                        return;
                    }

                    await PcPowerController.ShutdownAsync(pc, _settings.AppPort, _logAction).ConfigureAwait(false);
                    response.StatusCode = 200;
                    var okBuf = Encoding.UTF8.GetBytes("Shutdown command sent");
                    response.ContentLength64 = okBuf.Length;
                    await response.OutputStream.WriteAsync(okBuf, 0, okBuf.Length).ConfigureAwait(false);
                    return;
                }

                // ---- PCS: restart ----
                if (request.HttpMethod == "POST" && path == "/pc/restart")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var pcReq = JsonSerializer.Deserialize<PcRequest>(body);

                    var pc = FindPc(pcReq);
                    if (pc == null)
                    {
                        response.StatusCode = 404;
                        var respBuf = Encoding.UTF8.GetBytes("PC not found");
                        response.ContentLength64 = respBuf.Length;
                        await response.OutputStream.WriteAsync(respBuf, 0, respBuf.Length).ConfigureAwait(false);
                        return;
                    }

                    await PcPowerController.RestartAsync(pc, _settings.AppPort, _logAction).ConfigureAwait(false);
                    response.StatusCode = 200;
                    var okBuf = Encoding.UTF8.GetBytes("Restart command sent");
                    response.ContentLength64 = okBuf.Length;
                    await response.OutputStream.WriteAsync(okBuf, 0, okBuf.Length).ConfigureAwait(false);
                    return;
                }

                // Default 404
                response.StatusCode = 404;
            }
            catch (OperationCanceledException)
            {
                // stopping
                try { response.StatusCode = 503; } catch { }
            }
            catch (Exception ex)
            {
                _logAction("HTTP Server request error: " + ex.Message);
                try { response.StatusCode = 500; } catch { }
            }
            finally
            {
                // IMPORTANT: Always close HTTP responses so the browser never "spins forever".
                // Do NOT close here for websocket requests (the WS owns the connection).
                if (!isWebSocket)
                {
                    try { response.OutputStream.Close(); } catch { }
                    try { response.Close(); } catch { }
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

        private async Task SendStatusSnapshotAsync(WebSocket ws)
        {
            if (ws == null || ws.State != WebSocketState.Open) return;

            var statuses = _statusProvider?.Invoke() ?? new List<AppStatusManager.AppWebStatus>();
            var json = JsonSerializer.Serialize(statuses);
            var bytes = Encoding.UTF8.GetBytes(json);

            await ws.SendAsync(new ArraySegment<byte>(bytes),
                               WebSocketMessageType.Text,
                               endOfMessage: true,
                               cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        private async Task BroadcastStatusToWebSocketsAsync()
        {
            List<WebSocket> clients;
            lock (_wsLock) clients = _wsClients.ToList();

            if (clients.Count == 0) return;

            var statuses = _statusProvider?.Invoke() ?? new List<AppStatusManager.AppWebStatus>();
            var json = JsonSerializer.Serialize(statuses);
            var bytes = Encoding.UTF8.GetBytes(json);

            foreach (var ws in clients)
            {
                try
                {
                    if (ws.State != WebSocketState.Open) continue;

                    await ws.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: CancellationToken.None
                    ).ConfigureAwait(false);
                }
                catch
                {
                    lock (_wsLock) _wsClients.Remove(ws);
                    try { ws.Abort(); } catch { }
                    try { ws.Dispose(); } catch { }
                }
            }
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
