using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AppRestarter.Models;

namespace AppRestarter
{
    public class RoutineExecutor
    {
        private readonly List<ApplicationDetails> _applications;
        private readonly Action<string> _log;

        // Win32 API imports for keyboard and mouse input
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public RoutineExecutor(List<ApplicationDetails> applications, Action<string> log = null)
        {
            _applications = applications;
            _log = log;
        }

        public async Task ExecuteRoutineAsync(Routine routine, CancellationToken cancellationToken = default)
        {
            foreach (var step in routine.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Process Wait Conditions
                foreach (var wait in step.WaitConditions)
                {
                    await ProcessWaitConditionAsync(wait, cancellationToken);
                }

                // 2. Execute Action
                if (step.Action != null)
                {
                    await ExecuteActionAsync(step.Action, cancellationToken);
                }
            }
        }

        private async Task ProcessWaitConditionAsync(WaitCondition wait, CancellationToken cancellationToken)
        {
             if (wait.Type == WaitType.TimeDelay)
             {
                 await Task.Delay(wait.WaitTimeSeconds * 1000, cancellationToken);
             }
             else if (wait.Type == WaitType.AppRunning)
             {
                 var app = _applications.FirstOrDefault(a => a.Name == wait.AppId);
                 if (app != null)
                 {
                     int checkIntervalMs = 1000;
                     int totalWaitedMs = 0;
                     int timeoutMs = wait.AppStartTimeoutSeconds * 1000;

                     while (true)
                     {
                         cancellationToken.ThrowIfCancellationRequested();

                         // Check if process is running
                         bool isRunning = IsAppRunning(app);
                         if (isRunning)
                         {
                             break;
                         }

                         if (timeoutMs > 0 && totalWaitedMs >= timeoutMs)
                         {
                             throw new TimeoutException($"Timed out waiting for app {wait.AppId} to run.");
                         }

                         await Task.Delay(checkIntervalMs, cancellationToken);
                         totalWaitedMs += checkIntervalMs;
                     }
                 }
             }
        }

        private bool IsAppRunning(ApplicationDetails app)
        {
            return ProcessTerminator.IsRunning(app);
        }

        private async Task ExecuteActionAsync(RoutineAction action, CancellationToken cancellationToken)
        {
            if (action.TargetType == TargetType.App)
            {
                 var app = _applications.FirstOrDefault(a => a.Name == action.TargetId);
                 if (app != null)
                 {
                     switch (action.Type)
                     {
                         case ActionType.Start:
                             if (!string.IsNullOrWhiteSpace(app.RestartPath))
                             {
                                 var startInfo = new System.Diagnostics.ProcessStartInfo
                                 {
                                     FileName = app.RestartPath,
                                     WorkingDirectory = System.IO.Path.GetDirectoryName(app.RestartPath),
                                     UseShellExecute = true,
                                 };
                                 if (app.StartMinimized)
                                     startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                                 System.Diagnostics.Process.Start(startInfo);
                             }
                             break;
                         case ActionType.Stop:
                             await ProcessTerminator.StopAsync(app, log: null);
                             break;
                         case ActionType.Restart:
                             await ProcessTerminator.StopAsync(app, log: null);
                             await Task.Delay(2000, cancellationToken);
                             if (!string.IsNullOrWhiteSpace(app.RestartPath))
                             {
                                 var startInfo = new System.Diagnostics.ProcessStartInfo
                                 {
                                     FileName = app.RestartPath,
                                     WorkingDirectory = System.IO.Path.GetDirectoryName(app.RestartPath),
                                     UseShellExecute = true,
                                 };
                                 if (app.StartMinimized)
                                     startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                                 System.Diagnostics.Process.Start(startInfo);
                             }
                             break;
                         case ActionType.KeyboardShortcut:
                             if (!string.IsNullOrWhiteSpace(action.Keys))
                             {
                                 if (IsRemoteApp(app))
                                 {
                                     await SendRemoteRoutineActionAsync(app, "KeyboardShortcut", keys: action.Keys);
                                 }
                                 else
                                 {
                                     await ExecuteKeyboardShortcutAsync(app, action.Keys, cancellationToken);
                                 }
                             }
                             break;
                         case ActionType.ClickArea:
                             if (action.ClickX >= 0 && action.ClickY >= 0)
                             {
                                 if (IsRemoteApp(app))
                                 {
                                     await SendRemoteRoutineActionAsync(app, "ClickArea", clickX: action.ClickX, clickY: action.ClickY);
                                 }
                                 else
                                 {
                                     await ExecuteClickAsync(app, action.ClickX, action.ClickY, cancellationToken);
                                 }
                             }
                             break;
                                          case ActionType.Minimize:
                                              {
                                                  if (IsRemoteApp(app))
                                                  {
                                                      await SendRemoteRoutineActionAsync(app, "Minimize");
                                                  }
                                                  else
                                                  {
                                                      await ExecuteMinimizeAsync(app, cancellationToken);
                                                  }
                                              }
                                              break;
                                      }
                                  }
                             }
                             // TODO: Implementation for PC and Group targets
        }

        private bool ActivateWindow(IntPtr hwnd)
        {
            try
            {
                // Check if window is visible
                if (!IsWindowVisible(hwnd))
                {
                    _log?.Invoke("Window is not visible, showing it");
                    ShowWindow(hwnd, SW_SHOW);
                }

                // Restore if minimized
                ShowWindow(hwnd, SW_RESTORE);
                Thread.Sleep(100);

                // Get current foreground window thread
                IntPtr currentForeground = GetForegroundWindow();
                uint currentThreadId = GetCurrentThreadId();
                uint targetThreadId = GetWindowThreadProcessId(hwnd, out _);

                // Attach to the target window's thread to allow SetForegroundWindow to work
                if (currentThreadId != targetThreadId)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, true);
                    BringWindowToTop(hwnd);
                    SetForegroundWindow(hwnd);
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                }
                else
                {
                    BringWindowToTop(hwnd);
                    SetForegroundWindow(hwnd);
                }

                Thread.Sleep(100);

                // Verify the window is now in foreground
                IntPtr newForeground = GetForegroundWindow();
                bool success = newForeground == hwnd;
                _log?.Invoke($"Window activation result: {success} (Handle: {hwnd}, Foreground: {newForeground})");

                return success;
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Error activating window: {ex.Message}");
                return false;
            }
        }

        private IntPtr FindAppWindow(ApplicationDetails app)
        {
            if (app == null)
            {
                _log?.Invoke("FindAppWindow: app is null");
                return IntPtr.Zero;
            }

            try
            {
                _log?.Invoke($"FindAppWindow: Looking for process for app '{app.Name}'");

                // Use the same process selection logic as start/stop operations
                var targets = ProcessTerminator.SelectTargets(app, _log);

                if (targets.Count == 0)
                {
                    _log?.Invoke($"FindAppWindow: No running processes found for '{app.Name}'");
                    return IntPtr.Zero;
                }

                _log?.Invoke($"FindAppWindow: Found {targets.Count} matching process(es)");

                // Try each target process to find a window
                foreach (var process in targets)
                {
                    try
                    {
                        var hwnd = FindWindowForProcess(process);
                        if (hwnd != IntPtr.Zero)
                        {
                            _log?.Invoke($"FindAppWindow: Found window handle {hwnd} for process {process.Id}");

                            // Clean up other processes
                            foreach (var p in targets)
                            {
                                if (p != process)
                                {
                                    try { p.Dispose(); } catch { }
                                }
                            }

                            return hwnd;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log?.Invoke($"FindAppWindow: Error accessing process {process.Id}: {ex.Message}");
                    }
                }

                // Clean up all processes if no window found
                foreach (var p in targets)
                {
                    try { p.Dispose(); } catch { }
                }

                _log?.Invoke("FindAppWindow: No window handle found for any matching process");
            }
            catch (Exception ex)
            {
                _log?.Invoke($"FindAppWindow: Exception: {ex.Message}");
            }

            return IntPtr.Zero;
        }

        private IntPtr FindWindowForProcess(System.Diagnostics.Process process)
        {
            try
            {
                // First try MainWindowHandle
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    _log?.Invoke($"Process {process.Id} has MainWindowHandle: {process.MainWindowHandle}");
                    return process.MainWindowHandle;
                }

                // If no MainWindowHandle, enumerate all windows for this process
                _log?.Invoke($"Process {process.Id} has no MainWindowHandle, enumerating all windows...");
                var windows = new List<IntPtr>();

                EnumWindows((hwnd, lParam) =>
                {
                    GetWindowThreadProcessId(hwnd, out uint processId);
                    if (processId == process.Id && IsWindowVisible(hwnd))
                    {
                        windows.Add(hwnd);
                        _log?.Invoke($"Found visible window {hwnd} for process {process.Id}");
                    }
                    return true;
                }, IntPtr.Zero);

                if (windows.Count > 0)
                {
                    _log?.Invoke($"Found {windows.Count} visible window(s) for process {process.Id}, using first one");
                    return windows[0];
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke($"FindWindowForProcess: Error for process {process.Id}: {ex.Message}");
            }

            return IntPtr.Zero;
        }

        // Helper methods for remote execution
        private bool IsRemoteApp(ApplicationDetails app)
        {
            return !string.IsNullOrWhiteSpace(app.ClientIP);
        }

        private async Task SendRemoteRoutineActionAsync(ApplicationDetails app, string actionType, string keys = null, int clickX = 0, int clickY = 0)
        {
            try
            {
                _log?.Invoke($"Sending remote routine action '{actionType}' to {app.ClientIP}");

                var request = new RemoteRoutineActionRequest
                {
                    ActionType = RemoteActionType.RoutineAction,
                    AppName = app.Name,
                    ProcessName = app.ProcessName,
                    RestartPath = app.RestartPath,
                    RoutineActionType = actionType,
                    Keys = keys,
                    ClickX = clickX,
                    ClickY = clickY
                };

                using var client = new System.Net.Sockets.TcpClient(app.ClientIP, 2024); // TODO: Get port from settings
                client.SendTimeout = 6000;
                using var stream = client.GetStream();

                var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(RemoteRoutineActionRequest));
                serializer.WriteObject(stream, request);
                stream.Flush();

                _log?.Invoke($"Remote routine action sent successfully");
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Error sending remote routine action: {ex.Message}");
            }
        }

        // Public helper methods for individual action execution (used by remote handler)
        public async Task ExecuteKeyboardShortcutAsync(ApplicationDetails app, string keys, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keys))
                return;

            _log?.Invoke($"Executing keyboard shortcut '{keys}' on app '{app.Name}'");
            var hwnd = FindAppWindow(app);
            if (hwnd != IntPtr.Zero)
            {
                _log?.Invoke($"Found window handle: {hwnd}");

                if (!ActivateWindow(hwnd))
                {
                    _log?.Invoke("Warning: Failed to fully activate window");
                }

                await Task.Delay(500, cancellationToken);

                _log?.Invoke($"Sending keys: {keys}");
                System.Windows.Forms.SendKeys.SendWait(keys);
                _log?.Invoke("Keys sent successfully");
            }
            else
            {
                _log?.Invoke($"Error: Could not find window for app '{app.Name}'");
            }
        }

        public async Task ExecuteClickAsync(ApplicationDetails app, int clickX, int clickY, CancellationToken cancellationToken = default)
        {
            if (clickX < 0 || clickY < 0)
                return;

            _log?.Invoke($"Executing click at ({clickX}, {clickY}) on app '{app.Name}'");
            var hwnd = FindAppWindow(app);
            if (hwnd != IntPtr.Zero)
            {
                _log?.Invoke($"Found window handle: {hwnd}");

                if (!ActivateWindow(hwnd))
                {
                    _log?.Invoke("Warning: Failed to fully activate window");
                }

                await Task.Delay(500, cancellationToken);

                _log?.Invoke($"Moving cursor to screen position ({clickX}, {clickY})");
                SetCursorPos(clickX, clickY);
                await Task.Delay(100, cancellationToken);

                _log?.Invoke("Performing mouse click");
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                await Task.Delay(50, cancellationToken);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                _log?.Invoke("Click completed");
            }
            else
            {
                _log?.Invoke($"Error: Could not find window for app '{app.Name}'");
            }
        }

        public async Task ExecuteMinimizeAsync(ApplicationDetails app, CancellationToken cancellationToken = default)
        {
            _log?.Invoke($"Minimizing app '{app.Name}'");
            var hwnd = FindAppWindow(app);
            if (hwnd != IntPtr.Zero)
            {
                _log?.Invoke($"Found window handle: {hwnd}");

                const int SW_MINIMIZE = 6;
                if (ShowWindow(hwnd, SW_MINIMIZE))
                {
                    _log?.Invoke($"Successfully minimized '{app.Name}'");
                }
                else
                {
                    _log?.Invoke($"Warning: ShowWindow returned false for '{app.Name}'");
                }
            }
            else
            {
                _log?.Invoke($"Error: Could not find window for app '{app.Name}'");
            }

            await Task.CompletedTask;
        }
    }
}
