using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRestarter
{
    public sealed class AppStatusManager : IDisposable
    {
        public enum AppRunVisualState
        {
            Stopped,
            Running,
            UnexpectedlyStopped,   // RED
            AutoStartPending       // ORANGE
        }

        public static readonly Color StatusGray = Color.FromArgb(55, 65, 81);
        public static readonly Color StatusGreen = Color.FromArgb(34, 197, 94);
        public static readonly Color StatusRed = Color.FromArgb(228, 8, 10);
        public static readonly Color StatusOrange = Color.FromArgb(255, 203, 91);

        private readonly Control _uiInvoker;
        private readonly Action<string> _log;
        private readonly Func<bool> _isAppsViewActive;
        private readonly Func<List<ApplicationDetails>> _getAppsSnapshot;
        private readonly Func<int> _getAppPort;
        private readonly Func<int> _getTimeoutMs;

        // Stable-keyed state (works across clones/deserialization/batch requests)
        private readonly Dictionary<string, Control> _appStatusIndicators = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AppRunVisualState> _lastAppRunStates = new(StringComparer.OrdinalIgnoreCase);

        // green at any time" tracking
        private readonly HashSet<string> _hasEverBeenRunning = new(StringComparer.OrdinalIgnoreCase);

        // "stopped by us" tracking
        // If true and app is stopped, we show GRAY (not RED) until it runs again
        private readonly Dictionary<string, bool> _lastStopWasByUs = new(StringComparer.OrdinalIgnoreCase);

        private System.Windows.Forms.Timer _appStatusTimer;
        private volatile bool _statusRefreshInProgress;

        public AppStatusManager(
            Control uiInvoker,
            Action<string> log,
            Func<bool> isAppsViewActive,
            Func<List<ApplicationDetails>> getAppsSnapshot,
            Func<int> getAppPort,
            Func<int> getTimeoutMs)
        {
            _uiInvoker = uiInvoker ?? throw new ArgumentNullException(nameof(uiInvoker));
            _log = log ?? (_ => { });
            _isAppsViewActive = isAppsViewActive ?? (() => true);
            _getAppsSnapshot = getAppsSnapshot ?? throw new ArgumentNullException(nameof(getAppsSnapshot));
            _getAppPort = getAppPort ?? throw new ArgumentNullException(nameof(getAppPort));
            _getTimeoutMs = getTimeoutMs ?? (() => 8000);
        }

        public void Dispose()
        {
            try
            {
                if (_appStatusTimer != null)
                {
                    _appStatusTimer.Stop();
                    _appStatusTimer.Tick -= AppStatusTimer_Tick;
                    _appStatusTimer.Dispose();
                    _appStatusTimer = null;
                }
            }
            catch { }
        }

        // ----------------- KEYING -----------------

        private static string GetAppKey(ApplicationDetails app)
        {
            if (app == null) return string.Empty;

            var ip = (app.ClientIP ?? string.Empty).Trim();

            var path = (app.RestartPath ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(path))
                return $"{ip}|PATH|{path}";

            var proc = (app.ProcessName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(proc))
                return $"{ip}|PROC|{proc}";

            var name = (app.Name ?? string.Empty).Trim();
            return $"{ip}|NAME|{name}";
        }

        // ----------------- PUBLIC STATE HOOKS (call from Start/Stop buttons) -----------------

        /// <summary>
        /// Call this when YOU initiate a stop via the Stop button (local or remote).
        /// This suppresses RED while the app is stopped, until it runs again.
        /// </summary>
        public void MarkStopInitiatedByUs(ApplicationDetails app)
        {
            var key = GetAppKey(app);
            if (string.IsNullOrWhiteSpace(key)) return;

            _lastStopWasByUs[key] = true;
        }

        /// <summary>
        /// Optional but recommended: call this when YOU initiate a start via Start button.
        /// It clears the "stopped by us" suppression immediately.
        /// </summary>
        public void MarkStartInitiatedByUs(ApplicationDetails app)
        {
            var key = GetAppKey(app);
            if (string.IsNullOrWhiteSpace(key)) return;

            _lastStopWasByUs[key] = false;
        }

        // ----------------- UI INDICATORS -----------------

        public void ClearIndicators() => _appStatusIndicators.Clear();

        public void RegisterIndicator(ApplicationDetails app, Control indicatorControl)
        {
            if (app == null || indicatorControl == null) return;

            var key = GetAppKey(app);
            if (string.IsNullOrWhiteSpace(key)) return;

            _appStatusIndicators[key] = indicatorControl;
        }

        public bool TryGetLastState(ApplicationDetails app, out AppRunVisualState state)
        {
            state = AppRunVisualState.Stopped;
            var key = GetAppKey(app);
            return !string.IsNullOrWhiteSpace(key) && _lastAppRunStates.TryGetValue(key, out state);
        }

        public void EnsureTimerStarted(int intervalMs = 3000)
        {
            if (_appStatusTimer != null) return;

            _appStatusTimer = new System.Windows.Forms.Timer();
            _appStatusTimer.Interval = Math.Max(500, intervalMs);
            _appStatusTimer.Tick += AppStatusTimer_Tick;
            _appStatusTimer.Start();
        }

        private void AppStatusTimer_Tick(object sender, EventArgs e) => Refresh();

        // ----------------- REFRESH -----------------

        public void Refresh()
        {
            if (!_isAppsViewActive()) return;
            if (_statusRefreshInProgress) return;

            _statusRefreshInProgress = true;
            var appsSnapshot = _getAppsSnapshot?.Invoke() ?? new List<ApplicationDetails>();

            Task.Run(() =>
            {
                try
                {
                    var results = new List<(ApplicationDetails app, AppRunVisualState state)>();

                    // Local
                    foreach (var app in appsSnapshot.Where(a => string.IsNullOrWhiteSpace(a.ClientIP)))
                    {
                        bool isRunning = ProcessTerminator.IsRunning(app);
                        results.Add((app, ComputeVisualState(app, isRunning)));
                    }

                    // Remote grouped batch
                    var groups = appsSnapshot
                        .Where(a => !string.IsNullOrWhiteSpace(a.ClientIP))
                        .GroupBy(a => a.ClientIP);

                    foreach (var g in groups)
                        results.AddRange(GetRemoteBatchStatesForGroup(g.Key, g.ToList()));

                    if (_uiInvoker != null && !_uiInvoker.IsDisposed && _uiInvoker.IsHandleCreated)
                    {
                        _uiInvoker.BeginInvoke(new Action(() =>
                        {
                            foreach (var r in results)
                                UpdateIndicator(r.app, r.state);
                        }));
                    }
                }
                finally
                {
                    _statusRefreshInProgress = false;
                }
            });
        }

        // ----------------- NEW RED LOGIC -----------------

        private AppRunVisualState ComputeVisualState(ApplicationDetails app, bool isRunning)
        {
            if (app == null) return AppRunVisualState.Stopped;

            var key = GetAppKey(app);
            if (string.IsNullOrWhiteSpace(key))
                return isRunning ? AppRunVisualState.Running : AppRunVisualState.Stopped;

            // Once we see it running, it qualifies for "was green at any time"
            if (isRunning)
            {
                _hasEverBeenRunning.Add(key);

                // Any observed running clears stop-suppression automatically
                _lastStopWasByUs[key] = false;

                return AppRunVisualState.Running;
            }

            // Not running:
            bool hasEverRun = _hasEverBeenRunning.Contains(key);
            bool stopWasByUs = _lastStopWasByUs.TryGetValue(key, out var v) && v;

            // Orange only before it has ever been seen running (optional feature retained)
            if (!hasEverRun && app.AutoStart && app.AutoStartDelayInSeconds > 0)
                return AppRunVisualState.AutoStartPending;

            // If it was green at least once and we didn't stop it => RED
            if (hasEverRun && !stopWasByUs)
                return AppRunVisualState.UnexpectedlyStopped;

            // Otherwise gray
            return AppRunVisualState.Stopped;
        }

        private void UpdateIndicator(ApplicationDetails app, AppRunVisualState state)
        {
            if (app == null) return;

            var key = GetAppKey(app);
            if (string.IsNullOrWhiteSpace(key)) return;

            _lastAppRunStates[key] = state;

            if (!_appStatusIndicators.TryGetValue(key, out var ctrl) || ctrl == null)
                return;

            Color color = StatusGray;
            switch (state)
            {
                case AppRunVisualState.Running: color = StatusGreen; break;
                case AppRunVisualState.UnexpectedlyStopped: color = StatusRed; break;
                case AppRunVisualState.AutoStartPending: color = StatusOrange; break;
                default: color = StatusGray; break;
            }

            if (ctrl is Label lbl) lbl.ForeColor = color;
            else ctrl.BackColor = color;
        }

        // ----------------- REMOTE BATCH -----------------

        private List<(ApplicationDetails app, AppRunVisualState state)> GetRemoteBatchStatesForGroup(
            string clientIp,
            List<ApplicationDetails> appsOnThisPc)
        {
            var results = new List<(ApplicationDetails app, AppRunVisualState state)>();

            if (string.IsNullOrWhiteSpace(clientIp) || appsOnThisPc == null || appsOnThisPc.Count == 0)
                return results;

            try
            {
                _log($"Batch status -> {clientIp}: {appsOnThisPc.Count} app(s)");

                var batchRequest = new AppStatusBatchRequest
                {
                    ActionType = RemoteActionType.AppStatusBatch,
                    Apps = appsOnThisPc.Select(a => new ApplicationDetails
                    {
                        Name = a.Name,
                        ProcessName = a.ProcessName,
                        RestartPath = a.RestartPath,
                        ClientIP = null, // remote treats as local
                        AutoStart = a.AutoStart,
                        AutoStartDelayInSeconds = a.AutoStartDelayInSeconds,
                        NoWarn = a.NoWarn,
                        StartMinimized = a.StartMinimized,
                        GroupName = a.GroupName
                    }).ToList()
                };

                using var client = new TcpClient(AddressFamily.InterNetwork);
                client.SendTimeout = 3000;
                client.ReceiveTimeout = 3000;

                client.Connect(clientIp, _getAppPort());

                using var stream = client.GetStream();
                var serializer = new DataContractSerializer(typeof(AppStatusBatchRequest));
                serializer.WriteObject(stream, batchRequest);
                stream.Flush();

                try { client.Client.Shutdown(SocketShutdown.Send); } catch { }

                using var ms = new System.IO.MemoryStream();
                byte[] buffer = new byte[2048];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                    if (ms.Length > 64 * 1024) break;
                }

                string reply = Encoding.UTF8.GetString(ms.ToArray());
                if (string.IsNullOrWhiteSpace(reply))
                {
                    _log($"Batch status <- {clientIp}: EMPTY reply");
                    foreach (var app in appsOnThisPc)
                        results.Add((app, ComputeVisualState(app, isRunning: false)));
                    return results;
                }

                var lines = reply
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .ToArray();

                _log($"Batch status <- {clientIp}: bytes={ms.Length}, lines={lines.Length}, head='{(lines.Length > 0 ? lines[0] : "")}'");

                if (lines.Length == 0 || !lines[0].Equals("STATUSBATCH", StringComparison.OrdinalIgnoreCase))
                {
                    _log($"Batch status <- {clientIp}: INVALID header");
                    foreach (var app in appsOnThisPc)
                        results.Add((app, ComputeVisualState(app, isRunning: false)));
                    return results;
                }

                var mapByPath = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split('|');
                    if (parts.Length < 5) continue;

                    string path = parts[2] ?? string.Empty;
                    bool isRunning = parts[3].Equals("Running", StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(path))
                        mapByPath[path] = isRunning;
                }

                foreach (var app in appsOnThisPc)
                {
                    bool isRunning = false;

                    if (!string.IsNullOrWhiteSpace(app.RestartPath) &&
                        mapByPath.TryGetValue(app.RestartPath, out var running))
                    {
                        isRunning = running;
                    }

                    results.Add((app, ComputeVisualState(app, isRunning)));
                }
            }
            catch (Exception ex)
            {
                _log($"Error checking remote batch status for {clientIp}: {ex.Message}");
                foreach (var app in appsOnThisPc)
                    results.Add((app, ComputeVisualState(app, isRunning: false)));
            }

            return results;
        }

        // ----------------- TCP SERVER HELPERS -----------------

        public void HandleAppStatusBatchReceived(NetworkStream stream, AppStatusBatchRequest batchRequest)
        {
            if (stream == null || batchRequest?.Apps == null)
                return;

            try
            {
                var lines = new List<string>();
                foreach (var app in batchRequest.Apps)
                {
                    string appStatus = BuildStatusLine(app, includeHeader: false, batchFormat: true);
                    lines.Add(appStatus);
                }

                string payload = "STATUSBATCH\n" + string.Join("\n", lines);
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                _log("Error sending batch app status over TCP: " + ex.Message);
            }
        }

        public void SendAppStatusResponse(NetworkStream stream, ApplicationDetails app)
        {
            if (stream == null || app == null)
                return;

            try
            {
                string payload = BuildStatusLine(app, includeHeader: true, batchFormat: false);
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                _log("Error sending app status over TCP: " + ex.Message);
            }
        }

        public string BuildStatusLine(ApplicationDetails app, bool includeHeader, bool batchFormat)
        {
            bool isRunning = ProcessTerminator.IsRunning(app);
            var state = ComputeVisualState(app, isRunning);

            string color = state switch
            {
                AppRunVisualState.Running => "green",
                AppRunVisualState.UnexpectedlyStopped => "red",
                AppRunVisualState.AutoStartPending => "orange",
                _ => "gray"
            };

            string runningText = isRunning ? "Running" : "Stopped";
            string name = app?.Name ?? string.Empty;
            string proc = app?.ProcessName ?? string.Empty;
            string path = app?.RestartPath ?? string.Empty;

            if (batchFormat)
                return $"{name}|{proc}|{path}|{runningText}|{color}";

            return includeHeader
                ? $"STATUS|{name}|{proc}|{runningText}|{color}"
                : $"{name}|{proc}|{runningText}|{color}";
        }
    }
}
