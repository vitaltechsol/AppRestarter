using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

public static class StartupHelper
{
    private const string DefaultTaskName = "AppRestarter";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Create or update a Scheduled Task that runs the current EXE at user logon with highest privileges.
    /// Also removes any old registry-based autostart entry.
    /// </summary>
    public static void AddOrUpdateAppStartup(Action<string> AddToLog, string taskName = DefaultTaskName, string appName = "AppRestarter")
    {
        try
        {
            string exePath = Application.ExecutablePath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                AddToLog?.Invoke("Startup: could not determine EXE path.");
                return;
            }

            // Remove old registry autostart if present
            RemoveOldRegistryEntry(AddToLog, appName);

            // Check if task already exists and if its path matches
            string queryArgs = $"/Query /TN \"{taskName}\" /FO LIST /V";
            int queryCode = RunSchtasks(queryArgs, requireElevation: false, out string stdout, out string stderr);

            bool needsUpdate = true;
            if (queryCode == 0 && stdout.Contains("TaskName:"))
            {
                // Look for the "Task To Run" line
                foreach (var line in stdout.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.TrimStart().StartsWith("Task To Run", StringComparison.OrdinalIgnoreCase) ||
                        line.TrimStart().StartsWith("Action", StringComparison.OrdinalIgnoreCase)) // different locales/versions
                    {
                        if (line.Contains(exePath, StringComparison.OrdinalIgnoreCase))
                        {
                            needsUpdate = false;
                        }
                        break;
                    }
                }
            }

            if (!needsUpdate)
            {
                //AddToLog?.Invoke($"Startup: scheduled task '{taskName}' already points to {exePath}, no update needed.");
                return;
            }

            // Create/update the task
            string createArgs = $"/Create /TN \"{taskName}\" /SC ONLOGON /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F";
            int code = RunSchtasks(createArgs, requireElevation: true, out string so, out string se);
            if (code == 0)
            {
                AddToLog?.Invoke($"Startup: scheduled task '{taskName}' created/updated to run: {exePath}");
            }
            else
            {
                AddToLog?.Invoke($"Startup: failed to create/update task '{taskName}'. ExitCode={code}\nStdOut: {so}\nStdErr: {se}");
            }
        }
        catch (Exception ex)
        {
            AddToLog?.Invoke($"Startup: exception creating/updating scheduled task. {ex.Message}");
        }
    }


    /// <summary>
    /// Remove the Scheduled Task autostart.
    /// </summary>
    public static void RemoveAppStartup(Action<string> AddToLog, string taskName = DefaultTaskName)
    {
        try
        {
            string argsDelete = $"/Delete /TN \"{taskName}\" /F";
            int code = RunSchtasks(argsDelete, requireElevation: true, out string so, out string se);
            if (code == 0)
            {
                AddToLog?.Invoke($"Startup: scheduled task '{taskName}' removed.");
            }
            else
            {
                AddToLog?.Invoke($"Startup: failed to remove task '{taskName}'. ExitCode={code}\nStdOut: {so}\nStdErr: {se}");
            }
        }
        catch (Exception ex)
        {
            AddToLog?.Invoke($"Startup: exception removing scheduled task. {ex.Message}");
        }
    }

    /// <summary>
    /// Helper: remove old registry Run entry if it exists.
    /// </summary>
    private static void RemoveOldRegistryEntry(Action<string> AddToLog, string appName)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key?.GetValue(appName) != null)
            {
                key.DeleteValue(appName, false);
                AddToLog?.Invoke($"Startup: removed old registry autostart entry for {appName}.");
            }
        }
        catch (Exception ex)
        {
            AddToLog?.Invoke($"Startup: failed to remove old registry entry. {ex.Message}");
        }
    }

    /// <summary>
    /// Small helper to run schtasks with or without elevation.
    /// </summary>
    private static int RunSchtasks(string args, bool requireElevation, out string stdOut, out string stdErr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = args,
            CreateNoWindow = true
        };

        if (requireElevation)
        {
            psi.UseShellExecute = true;
            psi.Verb = "runas"; // prompt for elevation
            stdOut = "";
            stdErr = "";
        }
        else
        {
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            stdOut = "";
            stdErr = "";
        }

        try
        {
            using var p = Process.Start(psi);
            if (!requireElevation)
            {
                stdOut = p.StandardOutput.ReadToEnd();
                stdErr = p.StandardError.ReadToEnd();
            }
            p.WaitForExit();
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            stdOut = "";
            stdErr = ex.Message;
            return -1;
        }
    }
}
public class WinApiHelper
{
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    public const int SW_MINIMIZE = 6;
}
