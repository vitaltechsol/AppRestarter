using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management; // Optional fallback; can remove if you don't want WMI
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace AppRestarter
{
    public static class ProcessKiller
    {
        // ---- Native interop for fast path retrieval ----
        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Kill processes according to the rule:
        /// - If ProcessName is specified:
        ///     * If RestartPath provided -> kill ONLY the process whose full path equals RestartPath (FIRST match; expect one instance per path).
        ///     * Else -> kill ALL processes with that name.
        /// - If ProcessName is not specified:
        ///     * Kill by full path (FIRST match; expect one instance per path).
        /// Returns the number of processes terminated and logs via AddToLog.
        /// </summary>
        public static int Kill(ApplicationDetails app, Action<string> AddToLog)
        {
            if (app == null)
            {
                AddToLog?.Invoke("No application details provided.");
                return 0;
            } else
            {
                AddToLog?.Invoke($"Stopping '{app.Name}'");
            }

            string procName = app.ProcessName?.Trim();
            string restartPathRaw = app.RestartPath?.Trim().Trim('"');
            string targetPath = NormalizePathSafe(restartPathRaw);

            // 1) Prefer kill-by-name if provided
            if (!string.IsNullOrWhiteSpace(procName))
            {
                int count = 0;

                try
                {
                    var byName = Process.GetProcessesByName(procName);

                    if (byName.Length == 0)
                    {
                        AddToLog?.Invoke($"No running processes found by name '{procName}'.");
                        return 0;
                    }

                    // If a path is supplied, double-check matches by path and kill ONLY the first match
                    if (!string.IsNullOrWhiteSpace(targetPath))
                    {
                        foreach (var p in byName)
                        {
                            try
                            {
                                if (!TryGetProcessPathFast(p, out var exePath)) continue;
                                var norm = NormalizePathSafe(exePath);
                                if (norm != null && string.Equals(norm, targetPath, StringComparison.Ordinal))
                                {
                                    p.Kill();
                                    count++;
                                    AddToLog?.Invoke($"Stopped '{p.ProcessName}' by name+path: {exePath}");
                                    break; // one instance per path
                                }
                            }
                            catch (Exception ex)
                            {
                                AddToLog?.Invoke($"Failed to stop '{p.ProcessName}' (PID {p.Id}) by name+path: {ex.Message}");
                            }
                        }

                        if (count == 0)
                            AddToLog?.Invoke($"No process named '{procName}' matched path '{restartPathRaw}'.");
                    }
                    else
                    {
                        // No path provided: kill all by name (legacy behavior)
                        foreach (var p in byName)
                        {
                            try
                            {
                                p.Kill();
                                count++;
                                AddToLog?.Invoke($"Stopped '{p.ProcessName}' (PID {p.Id}) by name.");
                            }
                            catch (Exception ex)
                            {
                                AddToLog?.Invoke($"Failed to stop '{p.ProcessName}' (PID {p.Id}) by name: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddToLog?.Invoke($"Error enumerating processes by name '{procName}': {ex.Message}");
                }

                return count;
            }

            // 2) If no name, kill-by-path (FIRST match only)
            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                int count = 0;
                try
                {
                    // Narrow by filename first for speed
                    string baseName = SafeGetFileNameWithoutExtension(restartPathRaw);
                    if (!string.IsNullOrWhiteSpace(baseName))
                    {
                        foreach (var p in Process.GetProcessesByName(baseName))
                        {
                            try
                            {
                                if (!TryGetProcessPathFast(p, out var exePath)) continue;
                                var norm = NormalizePathSafe(exePath);
                                if (norm != null && string.Equals(norm, targetPath, StringComparison.Ordinal))
                                {
                                    p.Kill();
                                    count++;
                                    AddToLog?.Invoke($"Stopped '{p.ProcessName}' (PID {p.Id}) | Path: {exePath}");
                                    return count; // one instance per path
                                }
                            }
                            catch (Exception ex)
                            {
                                AddToLog?.Invoke($"Failed to stop PID {p.Id} | Path: {ex.Message}");
                            }
                        }
                    }

                    // Fall back: scan all processes once
                    foreach (var p in Process.GetProcesses())
                    {
                        try
                        {
                            if (!TryGetProcessPathFast(p, out var exePath)) continue;
                            var norm = NormalizePathSafe(exePath);
                            if (norm != null && string.Equals(norm, targetPath, StringComparison.Ordinal))
                            {
                                p.Kill();
                                count++;
                                AddToLog?.Invoke($"Stopped '{p.ProcessName}' | Path: {exePath}");
                                break; // one instance per path
                            }
                        }
                        catch (Exception ex)
                        {
                            AddToLog?.Invoke($"Failed to stop PID {p.Id} by path: {ex.Message}");
                        }
                    }

                    if (count == 0)
                        AddToLog?.Invoke($"No running process matched path '{restartPathRaw}'.");
                }
                catch (Exception ex)
                {
                    AddToLog?.Invoke($"Error enumerating processes for path '{restartPathRaw}': {ex.Message}");
                }

                return count;
            }

            AddToLog?.Invoke("ProcessKiller: Neither ProcessName nor RestartPath provided; nothing to stop.");
            return 0;
        }

        /// <summary>
        /// Is the app currently running?
        /// If ProcessName is specified:
        ///   - With path: true if any process by that name matches full path (first match).
        ///   - Without path: true if any process by that name exists.
        /// Else if RestartPath is specified:
        ///   - true if any process matches that full path (first match).
        /// </summary>
        public static bool IsRunning(ApplicationDetails app)
        {
            if (app == null) return false;

            string procName = app.ProcessName?.Trim();
            string restartPathRaw = app.RestartPath?.Trim().Trim('"');
            string targetPath = NormalizePathSafe(restartPathRaw);

            // Name present
            if (!string.IsNullOrWhiteSpace(procName))
            {
                try
                {
                    var byName = Process.GetProcessesByName(procName);
                    if (byName.Length == 0) return false;

                    // If a path is given, match by path (first success returns true)
                    if (!string.IsNullOrWhiteSpace(targetPath))
                    {
                        foreach (var p in byName)
                        {
                            if (TryGetProcessPathFast(p, out var exePath))
                            {
                                var norm = NormalizePathSafe(exePath);
                                if (norm != null && string.Equals(norm, targetPath, StringComparison.Ordinal))
                                    return true;
                            }
                        }
                        return false;
                    }

                    // No path filter: any by-name process is "running"
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // No name: check by path
            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                try
                {
                    string baseName = SafeGetFileNameWithoutExtension(restartPathRaw);
                    if (!string.IsNullOrWhiteSpace(baseName))
                    {
                        foreach (var p in Process.GetProcessesByName(baseName))
                        {
                            if (TryGetProcessPathFast(p, out var exePath))
                            {
                                var norm = NormalizePathSafe(exePath);
                                if (norm != null && string.Equals(norm, targetPath, StringComparison.Ordinal))
                                    return true;
                            }
                        }
                    }

                    foreach (var p in Process.GetProcesses())
                    {
                        if (TryGetProcessPathFast(p, out var exePath))
                        {
                            var norm = NormalizePathSafe(exePath);
                            if (norm != null && string.Equals(norm, targetPath, StringComparison.Ordinal))
                                return true;
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            return false;
        }

        // ---- Helpers ----

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
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Fast, low-permission way to read full image path.
        /// Avoids WMI and avoids MainModule when possible.
        /// </summary>
        private static bool TryGetProcessPathFast(Process proc, out string exePath)
        {
            exePath = null;

            // Try native fast call first
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

            // Fall back to MainModule (can be slow/throw)
            try
            {
                exePath = proc.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exePath)) return true;
            }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }
            catch (NotSupportedException) { }

            // Final fallback: WMI (slowest; optional)
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {proc.Id}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    exePath = mo["ExecutablePath"] as string;
                    if (!string.IsNullOrWhiteSpace(exePath)) return true;
                }
            }
            catch (ManagementException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }
    }
}
