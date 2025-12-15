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
        public bool VerboseLogging { get; set; } = false;

        private readonly Control _uiInvoker;
        private readonly Action<string> _log;
        private readonly Func<bool> _isAppsViewActive;
        private readonly Func<List<ApplicationDetails>> _getAppsSnapshot;
        private readonly Func<int> _getAppPort;
        private readonly Func<int> _getTimeoutMs;

        // Stable-keyed state (works across clones/deserialization/batch requests)
        private readonly Dictionary<string, Control> _appStatusIndicators = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AppRunVisualState> _lastAppRunStates = new(StringComparer.OrdinalIgnoreCase);
        // Don't hammer offline PCs every tick
        private readonly Dictionary<string, DateTime> _nextRemoteRetryUtc = new(StringComparer.OrdinalIgnoreCase);

        // Limit simultaneous remote connects so you don't create a connection storm
        private readonly System.Threading.SemaphoreSlim _remoteConcurrency = new(initialCount: 4, maxCount: 4);

        // NEW: "green at any time" tracking
        private readonly HashSet<string> _hasEverBeenRunning = new(StringComparer.OrdinalIgnoreCase);

        // NEW: "stopped by us" tracking
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

        public void Refresh() => Refresh(force: false);

        public void Refresh(bool force)
        {
            if (!force && !_isAppsViewActive()) return;
            if (_statusRefreshInProgress) return;

            _statusRefreshInProgress = true;

            var appsSnapshot = _getAppsSnapshot?.Invoke() ?? new List<ApplicationDetails>();

            Task.Run(async () =>
            {
                try
                {
                    // 1) Local updates first (never blocked by remote)
                    var localResults = new List<(ApplicationDetails app, AppRunVisualState state)>();
                    foreach (var app in appsSnapshot.Where(a => string.IsNullOrWhiteSpace(a.ClientIP)))
                    {
                        bool isRunning = ProcessTerminator.IsRunning(app);
                        localResults.Add((app, ComputeVisualState(app, isRunning)));
                    }

                    SafeUiUpdate(localResults);

                    // 2) Remote updates in parallel (per-IP)
                    var remoteGroups = appsSnapshot
                        .Where(a => !string.IsNullOrWhiteSpace(a.ClientIP))
                        .GroupBy(a => a.ClientIP)
                        .ToList();

                    var tasks = new List<Task>();

                    foreach (var g in remoteGroups)
                    {
                        string ip = g.Key;

                        // cooldown: skip retrying offline PCs too often
                        if (_nextRemoteRetryUtc.TryGetValue(ip, out var nextUtc) && DateTime.UtcNow < nextUtc)
                            continue;

                        var appsForIp = g.ToList();

                        tasks.Add(Task.Run(async () =>
                        {
                            await _remoteConcurrency.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                // Per-IP hard timeout (connect+read+parse)
                                var timeoutMs = 1500; // tune as you like
                                var remoteTask = GetRemoteBatchStatesForGroupAsync(ip, appsForIp);

                                var completed = await Task.WhenAny(remoteTask, Task.Delay(timeoutMs)).ConfigureAwait(false);
                                if (completed != remoteTask)
                                {
                                    // timed out: mark offline briefly and don't block others
                                    _nextRemoteRetryUtc[ip] = DateTime.UtcNow.AddSeconds(15);
                                    if (VerboseLogging)
                                        _log($"Batch status <- {ip}: TIMEOUT (cooldown 15s)");
                                    
                                    // You can either:
                                    // A) do nothing (keep last indicator state), OR
                                    // B) set gray/unknown here.
                                    // I recommend A to avoid flicker.
                                    return;
                                }

                                var results = await remoteTask.ConfigureAwait(false);

                                // Success: allow immediate retries next tick
                                _nextRemoteRetryUtc[ip] = DateTime.UtcNow;

                                SafeUiUpdate(results);
                            }
                            catch (SocketException)
                            {
                                _nextRemoteRetryUtc[ip] = DateTime.UtcNow.AddSeconds(15);
                                if(VerboseLogging)
                                    _log($"Batch status <- {ip}: offline (cooldown 15s)");
                            }
                            catch (Exception ex)
                            {
                                _nextRemoteRetryUtc[ip] = DateTime.UtcNow.AddSeconds(15);
                                if (VerboseLogging)
                                    _log($"Batch status <- {ip}: {ex.Message} (cooldown 15s)");
                            }
                            finally
                            {
                                _remoteConcurrency.Release();
                            }
                        }));
                    }


                    // Don't block the whole UI on remotes, but do allow the refresh flag to reset
                    // after remote tasks finish (or you can set it earlier if you prefer).
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                finally
                {
                    _statusRefreshInProgress = false;
                }
            });
        }

        private void SafeUiUpdate(List<(ApplicationDetails app, AppRunVisualState state)> results)
        {
            if (results == null || results.Count == 0) return;

            if (_uiInvoker != null && !_uiInvoker.IsDisposed && _uiInvoker.IsHandleCreated)
            {
                _uiInvoker.BeginInvoke(new Action(() =>
                {
                    foreach (var r in results)
                        UpdateIndicator(r.app, r.state);
                }));
            }
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

        private async Task<List<(ApplicationDetails app, AppRunVisualState state)>> GetRemoteBatchStatesForGroupAsync(
            string clientIp,
            List<ApplicationDetails> appsOnThisPc)
        {
            // call existing sync function inside Task.Run
            // (works fine because we already cap concurrency + have a hard timeout outside)
            return await Task.Run(() => GetRemoteBatchStatesForGroup(clientIp, appsOnThisPc)).ConfigureAwait(false);
        }

        private List<(ApplicationDetails app, AppRunVisualState state)> GetRemoteBatchStatesForGroup(
             string clientIp,
             List<ApplicationDetails> appsOnThisPc)
        {
            var results = new List<(ApplicationDetails app, AppRunVisualState state)>();

            if (string.IsNullOrWhiteSpace(clientIp) || appsOnThisPc == null || appsOnThisPc.Count == 0)
                return results;

            try
            {
                if (VerboseLogging)
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
                    if (VerboseLogging)
                        _log($"Batch status <- {clientIp}: EMPTY reply");
                    foreach (var app in appsOnThisPc)
                        results.Add((app, ComputeVisualState(app, isRunning: false)));
                    return results;
                }

                var lines = reply
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .ToArray();
                if (VerboseLogging)
                    _log($"Batch status <- {clientIp}: bytes={ms.Length}, lines={lines.Length}, head='{(lines.Length > 0 ? lines[0] : "")}'");

                if (lines.Length == 0 || !lines[0].Equals("STATUSBATCH", StringComparison.OrdinalIgnoreCase))
                {
                    if (VerboseLogging)
                        _log($"Batch status <- {clientIp}: INVALID header");
                    foreach (var app in appsOnThisPc)
                        results.Add((app, ComputeVisualState(app, isRunning: false)));
                    return results;
                }

                // Map by RestartPath: (isRunning, color)
                var mapByPath = new Dictionary<string, (bool isRunning, string color)>(StringComparer.OrdinalIgnoreCase);

                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split('|');
                    if (parts.Length < 5) continue;

                    string path = parts[2] ?? string.Empty;
                    bool isRunning = parts[3].Equals("Running", StringComparison.OrdinalIgnoreCase);
                    string color = (parts[4] ?? "").Trim().ToLowerInvariant();

                    if (!string.IsNullOrWhiteSpace(path))
                        mapByPath[path] = (isRunning, color);
                }

                foreach (var app in appsOnThisPc)
                {
                    // default fallback
                    bool isRunning = false;
                    string color = "gray";

                    if (!string.IsNullOrWhiteSpace(app.RestartPath) &&
                        mapByPath.TryGetValue(app.RestartPath, out var v))
                    {
                        isRunning = v.isRunning;
                        color = v.color;
                    }

                    // Honor the remote-reported color for non-running states (orange/red/gray).
                    // Green is implied by isRunning anyway.
                    AppRunVisualState state;
                    if (isRunning)
                    {
                        state = AppRunVisualState.Running;
                    }
                    else
                    {
                        state = color switch
                        {
                            "orange" => AppRunVisualState.AutoStartPending,
                            "red" => AppRunVisualState.UnexpectedlyStopped,
                            _ => AppRunVisualState.Stopped
                        };
                    }

                    results.Add((app, state));
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
                // Snapshot of THIS machine's configured apps (important for remote orange)
                var localApps = _getAppsSnapshot?.Invoke() ?? new List<ApplicationDetails>();

                var lines = new List<string>();

                foreach (var reqApp in batchRequest.Apps)
                {
                    // Try to find the real local app config so AutoStartDelayInSeconds is accurate
                    ApplicationDetails appToUse = null;

                    if (!string.IsNullOrWhiteSpace(reqApp?.RestartPath))
                    {
                        appToUse = localApps.FirstOrDefault(a =>
                            !string.IsNullOrWhiteSpace(a.RestartPath) &&
                            string.Equals(a.RestartPath, reqApp.RestartPath, StringComparison.OrdinalIgnoreCase));
                    }

                    if (appToUse == null && !string.IsNullOrWhiteSpace(reqApp?.ProcessName))
                    {
                        appToUse = localApps.FirstOrDefault(a =>
                            !string.IsNullOrWhiteSpace(a.ProcessName) &&
                            string.Equals(a.ProcessName, reqApp.ProcessName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (appToUse == null && !string.IsNullOrWhiteSpace(reqApp?.Name))
                    {
                        appToUse = localApps.FirstOrDefault(a =>
                            !string.IsNullOrWhiteSpace(a.Name) &&
                            string.Equals(a.Name, reqApp.Name, StringComparison.OrdinalIgnoreCase));
                    }

                    // Fallback to request object if not found
                    appToUse ??= reqApp;

                    // Important: treat as local on this machine when computing key/state
                    if (appToUse != null)
                        appToUse.ClientIP = null;

                    // Name|ProcessName|RestartPath|Running|Color  (Color includes orange)
                    string line = BuildStatusLine(appToUse, includeHeader: false, batchFormat: true);
                    lines.Add(line);
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

        // ---------------------------
        // Web status snapshot helpers
        // ---------------------------
        public sealed class AppWebStatus
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string ProcessName { get; set; }
            public string RestartPath { get; set; }
            public string ClientIP { get; set; }
            public string RunningText { get; set; } // "Running" / "Stopped"
            public string Color { get; set; }       // "green" / "red" / "orange" / "gray"
        }

        /// <summary>
        /// Builds a JSON-friendly status snapshot for the web app using the SAME color logic as WinForms.
        /// </summary>
        public List<AppWebStatus> BuildWebStatusSnapshot(IEnumerable<ApplicationDetails> apps)
        {
            var list = new List<AppWebStatus>();
            if (apps == null) return list;

            // Ensure remote statuses get refreshed even when the Apps view isn't active.
            try
            {
                if (apps.Any(a => !string.IsNullOrWhiteSpace(a?.ClientIP)))
                    Refresh(force: true);
            }
            catch { }

            foreach (var app in apps)
            {
                bool isRunning = ProcessTerminator.IsRunning(app);
                // Prefer last-known state (includes remote batch results) when available.
                if (!TryGetLastState(app, out var state))
                {
                    var isRemote = !string.IsNullOrWhiteSpace(app?.ClientIP);
                    if (!isRemote)
                    {
                        state = ComputeVisualState(app, isRunning);
                    }
                    else
                    {
                        // Remote state not known yet: default to computed stopped/gray.
                        state = ComputeVisualState(app, isRunning: false);
                    }
                }

                string color = state switch
                {
                    AppRunVisualState.Running => "green",
                    AppRunVisualState.UnexpectedlyStopped => "red",
                    AppRunVisualState.AutoStartPending => "orange",
                    _ => "gray"
                };

                list.Add(new AppWebStatus
                {
                    Key = GetAppKey(app),
                    Name = app?.Name ?? string.Empty,
                    ProcessName = app?.ProcessName ?? string.Empty,
                    RestartPath = app?.RestartPath ?? string.Empty,
                    ClientIP = app?.ClientIP ?? string.Empty,
                    RunningText = isRunning ? "Running" : "Stopped",
                    Color = color
                });
            }

            return list;
        }

    }
}
