using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AppRestarter
{
    public static class ProcessTerminator
    {
        // Native interop for window enumeration/close and fast image path lookups
        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const int WM_CLOSE = 0x0010;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        private const uint GA_ROOT = 2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Attempts a graceful shutdown first, then force-kills if the process doesn't exit within timeoutMs.
        /// Selection policy:
        ///  - If ProcessName is specified:
        ///      * If RestartPath provided -> target ONLY the process whose full path matches (first match; one instance per path).
        ///      * Else -> target all processes with that name.
        ///  - If ProcessName is not specified:
        ///      * Target by full path (first match; one instance per path).
        /// Returns the number of processes that were terminated (gracefully or by force).
        /// </summary>
        public static async Task<int> StopAsync(ApplicationDetails app, Action<string> log, int timeoutMs = 8000)
        {
            if (app == null) { log?.Invoke("Terminator: No app details."); return 0; }

            log?.Invoke($"Stopping {app.Name}.");

            var targets = SelectTargets(app, log);
            if (targets.Count == 0)
            {
                log?.Invoke($"No running processes found for {app.Name}.");
                return 0;
            }

            int stopped = 0;

            foreach (var p in targets)
            {
                try
                {
                    bool attemptedGraceful = false;

                    // 1) Try main window first (if there is one)
                    if (TrySendCloseMainWindow(p, log, app))
                    {
                        attemptedGraceful = true;
                        // important: there may be more top-level windows for the same PID
                        SendWmCloseToAllTopLevelWindows(p.Id, log, app);

                        if (await WaitForExitWithTimeoutAsync(p, timeoutMs).ConfigureAwait(false))
                        {
                            log?.Invoke($"Gracefully stopped {p.ProcessName} (PID {p.Id}).");
                            stopped++;
                            continue;
                        }
                        else
                        {
                            log?.Invoke($"{p.ProcessName} (PID {p.Id}) did not exit within {timeoutMs} ms after main WM_CLOSE; will force.");
                        }
                    }
                    else
                    {
                        // 2) No main window or couldn't close it — still try ALL windows
                        if (SendWmCloseToAllTopLevelWindows(p.Id, log, app))
                        {
                            attemptedGraceful = true;
                            if (await WaitForExitWithTimeoutAsync(p, timeoutMs).ConfigureAwait(false))
                            {
                                log?.Invoke($"Gracefully stopped (all windows) {p.ProcessName} (PID {p.Id}).");
                                stopped++;
                                continue;
                            }
                            else
                            {
                                log?.Invoke($"{p.ProcessName} (PID {p.Id}) did not exit within {timeoutMs} ms after posting WM_CLOSE to all windows; will force.");
                            }
                        }
                    }

                    // 3) Fallback: force kill
                    try
                    {
                        p.Kill();
                        if (await WaitForExitWithTimeoutAsync(p, 2000).ConfigureAwait(false))
                            log?.Invoke($"Force-killed {p.ProcessName} (PID {p.Id}).");
                        else
                            log?.Invoke($"Force-kill requested for {p.ProcessName} (PID {p.Id}).");
                        stopped++;
                    }
                    catch (Exception exKill)
                    {
                        log?.Invoke($"Failed to force-kill {p.ProcessName} (PID {p.Id}): {exKill.Message}");
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Error stopping {p.ProcessName} (PID {p.Id}): {ex.Message}");
                }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            return stopped;
        }

        // ---- Target selection (matches your policy) ----

        private static List<Process> SelectTargets(ApplicationDetails app, Action<string> log)
        {
            var result = new List<Process>();

            string procName = app.ProcessName?.Trim();
            string rawPath = app.RestartPath?.Trim().Trim('"');
            string targetPath = NormalizePathSafe(rawPath);

            if (!string.IsNullOrWhiteSpace(procName))
            {
                var byName = Process.GetProcessesByName(procName);

                if (byName.Length == 0) return result;

                // If a path is given, only target the first by matching full path
                if (!string.IsNullOrWhiteSpace(targetPath))
                {
                    foreach (var p in byName)
                    {
                        if (TryGetProcessPathFast(p, out var exe))
                        {
                            if (string.Equals(NormalizePathSafe(exe), targetPath, StringComparison.Ordinal))
                            {
                                result.Add(p);
                                return result; // one instance per path
                            }
                        }
                    }
                    // If not found, nothing to stop by name+path
                    return result;
                }

                // No path filter -> all of them
                result.AddRange(byName);
                return result;
            }

            // No name: try by path (one instance per path)
            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                // Narrow by base name first for speed
                string baseName = SafeGetFileNameWithoutExtension(rawPath);
                if (!string.IsNullOrWhiteSpace(baseName))
                {
                    foreach (var p in Process.GetProcessesByName(baseName))
                    {
                        if (TryGetProcessPathFast(p, out var exe))
                        {
                            if (string.Equals(NormalizePathSafe(exe), targetPath, StringComparison.Ordinal))
                            {
                                result.Add(p);
                                return result;
                            }
                        }
                    }
                }

                // Fallback: scan all processes once
                foreach (var p in Process.GetProcesses())
                {
                    if (TryGetProcessPathFast(p, out var exe))
                    {
                        if (string.Equals(NormalizePathSafe(exe), targetPath, StringComparison.Ordinal))
                        {
                            result.Add(p);
                            return result;
                        }
                    }
                    else
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
            }

            return result;
        }

        // ---- Graceful signaling helpers ----

        private static bool TrySendCloseMainWindow(Process p, Action<string> log, ApplicationDetails app)
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    if (p.CloseMainWindow())
                    {
                        log?.Invoke($"Closing process for {app.Name} (PID {p.Id}).");
                        return true;
                    }
                }
            }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
            return false;
        }

        private static bool SendWmCloseToAllTopLevelWindows(int pid, Action<string> log, ApplicationDetails app)
        {
            bool anySent = false;
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    // we want to close *all* top-level windows for this PID, visible or not,
                    // because some apps show dialog/tool windows that might be hidden
                    var root = GetAncestor(hWnd, GA_ROOT);
                    if (root != hWnd) return true;

                    GetWindowThreadProcessId(hWnd, out uint windowPid);
                    if (windowPid == (uint)pid)
                    {
                        if (PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
                        {
                            anySent = true;
                        }
                    }
                    return true; // continue enumeration
                }, IntPtr.Zero);
            }
            catch { }
            if (anySent) log?.Invoke($"Closing all {app.Name} windows (PID {pid}).");
            return anySent;
        }

        // ---- Path helpers ----

        private static string SafeGetFileNameWithoutExtension(string p)
        {
            try { return Path.GetFileNameWithoutExtension(p); } catch { return null; }
        }

        private static string NormalizePathSafe(string p)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(p)) return null;
                var full = Path.GetFullPath(p);
                return full.TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
            }
            catch { return null; }
        }

        private static async Task<bool> WaitForExitWithTimeoutAsync(Process p, int timeoutMs)
        {
            if (p == null) return true;
            try
            {
                if (p.HasExited) return true;

#if NET5_0_OR_GREATER
                var wait = p.WaitForExitAsync();
                var delay = Task.Delay(timeoutMs);
                var completed = await Task.WhenAny(wait, delay).ConfigureAwait(false);
                if (completed == wait) return true;
#else
                var exited = await Task.Run(() => p.WaitForExit(timeoutMs)).ConfigureAwait(false);
                if (exited) return true;
#endif

                var deadline = DateTime.UtcNow.AddMilliseconds(250);
                while (DateTime.UtcNow < deadline)
                {
                    if (p.HasExited) return true;
                    await Task.Delay(50).ConfigureAwait(false);
                }

                return p.HasExited;
            }
            catch
            {
                try { return p.HasExited; } catch { return false; }
            }
        }

        private static bool TryGetProcessPathFast(Process proc, out string exePath)
        {
            exePath = null;

            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, proc.Id);
                if (hProcess != IntPtr.Zero)
                {
                    var sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        exePath = sb.ToString();
                        return !string.IsNullOrWhiteSpace(exePath);
                    }
                }
            }
            catch { }
            finally
            {
                if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
            }

            try
            {
                exePath = proc.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exePath)) return true;
            }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }
            catch (NotSupportedException) { }

            return false;
        }

        /// <summary>
        /// Returns true if any process matches the same selection policy used for termination
        /// (by process name first, otherwise by executable path).
        /// </summary>
        public static bool IsRunning(ApplicationDetails app, Action<string> log = null)
        {
            try
            {
                var targets = SelectTargets(app, log);
                foreach (var p in targets) { try { p.Dispose(); } catch { } }
                return targets.Count > 0;
            }
            catch { return false; }
        }
    }
}
