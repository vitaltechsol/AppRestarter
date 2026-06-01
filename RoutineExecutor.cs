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

        private const int SW_RESTORE = 9;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public RoutineExecutor(List<ApplicationDetails> applications)
        {
            _applications = applications;
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
                                 var hwnd = FindAppWindow(app);
                                 if (hwnd != IntPtr.Zero)
                                 {
                                     // Restore and bring window to foreground
                                     ShowWindow(hwnd, SW_RESTORE);
                                     SetForegroundWindow(hwnd);
                                     await Task.Delay(200, cancellationToken); // Wait for window to be ready

                                     // Send the keys using SendKeys
                                     System.Windows.Forms.SendKeys.SendWait(action.Keys);
                                 }
                             }
                             break;
                         case ActionType.ClickArea:
                             if (action.ClickX >= 0 && action.ClickY >= 0)
                             {
                                 var hwnd = FindAppWindow(app);
                                 if (hwnd != IntPtr.Zero)
                                 {
                                     // Restore and bring window to foreground
                                     ShowWindow(hwnd, SW_RESTORE);
                                     SetForegroundWindow(hwnd);
                                     await Task.Delay(200, cancellationToken); // Wait for window to be ready

                                     // Get window position and convert client coordinates to screen coordinates
                                     var point = new POINT { X = action.ClickX, Y = action.ClickY };
                                     ClientToScreen(hwnd, ref point);

                                     // Move cursor and click
                                     SetCursorPos(point.X, point.Y);
                                     await Task.Delay(50, cancellationToken);
                                     mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                     await Task.Delay(50, cancellationToken);
                                     mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                 }
                             }
                             break;
                     }
                 }
            }
            // TODO: Implementation for PC and Group targets
        }

        private IntPtr FindAppWindow(ApplicationDetails app)
        {
            if (app == null || string.IsNullOrWhiteSpace(app.ProcessName))
                return IntPtr.Zero;

            try
            {
                // Get all processes with the app's process name
                var processes = System.Diagnostics.Process.GetProcessesByName(app.ProcessName);
                if (processes.Length == 0)
                    return IntPtr.Zero;

                // If a RestartPath is specified, match by full path
                if (!string.IsNullOrWhiteSpace(app.RestartPath))
                {
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (process.MainModule != null &&
                                string.Equals(process.MainModule.FileName, app.RestartPath, StringComparison.OrdinalIgnoreCase))
                            {
                                if (process.MainWindowHandle != IntPtr.Zero)
                                    return process.MainWindowHandle;
                            }
                        }
                        catch { }
                    }
                }

                // Fallback: return the first process with a main window
                foreach (var process in processes)
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                        return process.MainWindowHandle;
                }
            }
            catch { }

            return IntPtr.Zero;
        }
    }
}
