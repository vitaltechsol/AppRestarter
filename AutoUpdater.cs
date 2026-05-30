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
    public class AutoUpdater
    {
        private readonly string _repoUrl;
        private readonly string _appName;
        private readonly string _executableName;
        private readonly string _currentVersionStr;
        private readonly string[] _filesToKeep;

        public AutoUpdater(string repoUrl, string appName, string executableName, string currentVersionStr, string[] filesToKeep = null)
        {
            _repoUrl = repoUrl;
            _appName = appName;
            _executableName = executableName;
            _currentVersionStr = currentVersionStr;
            _filesToKeep = filesToKeep ?? Array.Empty<string>();
        }

        public async Task CheckForUpdatesAsync(bool manualCheck = false)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(_appName, "1.0"));

                var response = await client.GetStringAsync(_repoUrl);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString();
                if (string.IsNullOrEmpty(tagName)) return;

                if (tagName.StartsWith("v")) tagName = tagName.Substring(1);

                // Ignore git commit hash in ProductVersion if present
                var cleanCurrent = _currentVersionStr.Split('+')[0];

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

                            var releaseUrl = root.GetProperty("html_url").GetString();
                            var message = $"A new version ({tagName}) is available.\n\nWould you like to update now?\n\nClick 'No' to view release notes first.";
                            var result = MessageBox.Show(message, "Update Available", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);

                            if (result == DialogResult.Yes)
                            {
                                await PerformUpdate(downloadUrl);
                            }
                            else if (result == DialogResult.No)
                            {
                                if (!string.IsNullOrEmpty(releaseUrl))
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = releaseUrl,
                                        UseShellExecute = true
                                    });
                                }

                                var updateResult = MessageBox.Show($"Would you like to update to version {tagName} now?", "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (updateResult == DialogResult.Yes)
                                {
                                    await PerformUpdate(downloadUrl);
                                }
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

        private async Task PerformUpdate(string downloadUrl)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), _appName + "_Update");
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
                var exePath = Path.Combine(exeDir, _executableName);

                var batContentBuilder = new System.Text.StringBuilder();
                batContentBuilder.AppendLine("@echo off");
                batContentBuilder.AppendLine("timeout /t 2 /nobreak > nul");

                // Backup files
                foreach (var file in _filesToKeep)
                {
                    batContentBuilder.AppendLine($@"if exist ""{exeDir}\{file}"" (");
                    batContentBuilder.AppendLine($@"    copy /Y ""{exeDir}\{file}"" ""{tempPath}\{file}""");
                    batContentBuilder.AppendLine(")");
                }

                // Copy new files
                batContentBuilder.AppendLine($@"xcopy /Y /E /Q ""{extractedAppDir}\*"" ""{exeDir}\""");

                // Restore files
                foreach (var file in _filesToKeep)
                {
                    batContentBuilder.AppendLine($@"if exist ""{tempPath}\{file}"" (");
                    batContentBuilder.AppendLine($@"    copy /Y ""{tempPath}\{file}"" ""{exeDir}\{file}""");
                    batContentBuilder.AppendLine(")");
                }

                batContentBuilder.AppendLine($@"start """" ""{exePath}""");
                batContentBuilder.AppendLine(@"del ""%~f0""");

                var batContent = batContentBuilder.ToString();
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
