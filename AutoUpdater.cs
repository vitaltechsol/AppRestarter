using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppRestarter
{
    public static class AutoUpdater
    {
        private const string RepoUrl = "https://api.github.com/repos/vitaltechsol/AppRestarter/releases/latest";
        private const string AppName = "AppRestarter";
        private const string ExecutableName = "AppRestarter.exe";

        public static async Task CheckForUpdatesAsync(string currentVersionStr, bool manualCheck = false)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(AppName, "1.0"));

                var response = await client.GetStringAsync(RepoUrl);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString();
                if (string.IsNullOrEmpty(tagName)) return;

                if (tagName.StartsWith("v")) tagName = tagName.Substring(1);

                // Ignore git commit hash in ProductVersion if present
                var cleanCurrent = currentVersionStr.Split('+')[0];

                if (Version.TryParse(tagName, out var latestVersion) && Version.TryParse(cleanCurrent, out var currentVersion))
                {
                    if (latestVersion > currentVersion)
                    {
                        var assets = root.GetProperty("assets");
                        if (assets.GetArrayLength() > 0)
                        {
                            var asset = assets[0];
                            var downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            
                            if (string.IsNullOrEmpty(downloadUrl)) return;

                            if (MessageBox.Show($"A new version ({tagName}) is available. Would you like to update now?", "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            {
                                await PerformUpdate(downloadUrl);
                            }
                        }
                    }
                    else if (manualCheck)
                    {
                        MessageBox.Show("You are up to date.", "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else if (manualCheck)
                {
                    MessageBox.Show("Could not parse version information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                if (manualCheck)
                {
                    MessageBox.Show("Update check failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Debug.WriteLine("Update check failed: " + ex.Message);
            }
        }

        private static async Task PerformUpdate(string downloadUrl)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), AppName + "_Update");
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);

                var zipPath = Path.Combine(tempPath, "update.zip");

                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(zipPath, data);
                }

                ZipFile.ExtractToDirectory(zipPath, tempPath);

                var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                if (string.IsNullOrEmpty(exeDir)) exeDir = AppDomain.CurrentDomain.BaseDirectory;

                var extractedDirs = Directory.GetDirectories(tempPath);
                var extractedAppDir = extractedDirs.FirstOrDefault() ?? tempPath; 

                var batPath = Path.Combine(tempPath, "update.bat");
                var exePath = Path.Combine(exeDir, ExecutableName);

                var batContent = $@"@echo off
timeout /t 2 /nobreak > nul
xcopy /Y /E /Q ""{extractedAppDir}\*"" ""{exeDir}\""
start """" ""{exePath}""
del ""%~f0""
";
                File.WriteAllText(batPath, batContent);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(processStartInfo);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to download or apply update: " + ex.Message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
