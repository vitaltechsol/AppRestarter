using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

public static class StartupHelper
{
    public static void AddOrUpdateAppStartup(Action<string> AddToLog, string appName = "AppRestarter")
    {
        Debug.WriteLine("AddOrUpdateAppStartup");
        try
        {
            // Get full path of the running executable
            string exePath = Application.ExecutablePath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                AddToLog("Executable path could not be determine");
                throw new FileNotFoundException("Executable path could not be determined.");
            }

            string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

            using RegistryKey key = Registry.CurrentUser.OpenSubKey(runKey, writable: true);
            if (key == null)
            {
                // Can't open registry key for some reason, maybe permissions
                AddToLog("Failed to open registry key for startup.");
                return;
            }

            string currentValue = key.GetValue(appName) as string;
            string desiredValue = $"\"{exePath}\"";

            if (currentValue == null)
            {
                // Key not present, create it
                key.SetValue(appName, desiredValue);
                AddToLog($"Startup entry created for {appName}. {desiredValue}");
            }
            else if (!string.Equals(currentValue, desiredValue, StringComparison.OrdinalIgnoreCase))
            {
                // Path changed, update to new one
                key.SetValue(appName, desiredValue);
                AddToLog($"Startup entry updated for {appName}. {desiredValue}");
            }
            else
            {
                // Path is already correct
             //   AddToLog($"Startup entry already exists and is correct for {appName}. with {currentValue}");
            }
        }
        catch (Exception ex)
        {
            AddToLog($"Error setting startup registry key: {ex.Message}");
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
