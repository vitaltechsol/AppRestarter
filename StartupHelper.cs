using Microsoft.Win32;
using System;
using System.Windows.Forms;

public static class StartupHelper
{
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AppRestarter";

    public static void EnsureStartup(bool enable)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
        {
            string existingValue = key.GetValue(AppName) as string;
            string appPath = "\"" + Application.ExecutablePath + "\"";

            if (enable)
            {
                if (!string.Equals(existingValue, appPath, StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue(AppName, appPath);
                }
            }
            else
            {
                if (existingValue != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
    }
}
