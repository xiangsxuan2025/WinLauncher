using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinLauncher.Core.Models;
using WinLauncher.Infrastructure.Helpers;

namespace WinLauncher.Infrastructure.Strategies
{
    internal class UwpScanStrategy : BaseScanStrategy
    {
        public override string StrategyName { get; } = "UwpScanStrategy";
        private IconExtractorService _iconExtractor;

        public UwpScanStrategy(IconExtractorService iconExtractor) : base(iconExtractor)
        {
            this._iconExtractor = iconExtractor;
        }

        public override async Task<List<AppInfo>> ScanAsync()
        {
            return await ScanUwpAppsAsync();
        }

        /// <summary>
        /// 扫描 UWP 应用
        /// </summary>
        public async Task<List<AppInfo>> ScanUwpAppsAsync()
        {
            var uwpApps = new List<AppInfo>();

            try
            {
                System.Diagnostics.Debug.WriteLine("开始扫描 UWP 应用...");

                // 方法1: 通过开始菜单扫描 UWP 应用
                await ScanUwpAppsFromStartMenu(uwpApps);

                // 方法2: 通过 PowerShell 命令获取 UWP 应用
                await ScanUwpAppsFromPowerShell(uwpApps);

                // 方法3: 通过注册表获取 UWP 应用
                await ScanUwpAppsFromRegistry(uwpApps);

                System.Diagnostics.Debug.WriteLine($"扫描到 {uwpApps.Count} 个 UWP 应用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描 UWP 应用时出错: {ex.Message}");
            }

            return uwpApps;
        }

        #region ScanUwpAppsFromPowerShell

        /// <summary>
        /// 使用 PowerShell 扫描 UWP 应用
        /// </summary>
        private async Task ScanUwpAppsFromPowerShell(List<AppInfo> apps)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "Get-StartApps | ConvertTo-Json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();

                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            await ParsePowerShellUwpApps(output, apps);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"PowerShell 命令执行失败: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"使用 PowerShell 扫描 UWP 应用时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析 PowerShell 输出的 UWP 应用信息
        /// </summary>
        private async Task ParsePowerShellUwpApps(string jsonOutput, List<AppInfo> apps)
        {
            try
            {
                // 简单的 JSON 解析（实际项目中可以使用 Newtonsoft.Json）
                // 这里简化处理，实际需要根据 Get-StartApps 的实际输出格式进行解析
                var lines = jsonOutput.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Contains("AppID") && line.Contains("Name"))
                    {
                        // 简化解析逻辑
                        // 实际应该使用 JSON 解析器
                        try
                        {
                            var appId = ExtractValue(line, "AppID");
                            var name = ExtractValue(line, "Name");

                            if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(name))
                            {
                                var appInfo = new AppInfo
                                {
                                    Id = $"UWP_PS_{appId}",
                                    Name = name,
                                    DisplayName = name,
                                    ExecutablePath = appId, // UWP 应用使用 AppID 作为标识
                                    Icon = await UwpIcoCreater.CreateUwpDefaultIcon()
                                };

                                apps.Add(appInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"解析 PowerShell 输出行失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析 PowerShell UWP 应用输出时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从字符串中提取值
        /// </summary>
        private string ExtractValue(string line, string key)
        {
            try
            {
                var keyIndex = line.IndexOf($"\"{key}\"");
                if (keyIndex >= 0)
                {
                    var valueStart = line.IndexOf(':', keyIndex) + 1;
                    var valueEnd = line.IndexOf(',', valueStart);
                    if (valueEnd < 0) valueEnd = line.IndexOf('}', valueStart);

                    if (valueEnd > valueStart)
                    {
                        var value = line.Substring(valueStart, valueEnd - valueStart).Trim();
                        return value.Trim('"', ' ', '\t', '\r', '\n');
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"提取值失败: {ex.Message}");
                return null;
            }
        }

        #endregion ScanUwpAppsFromPowerShell

        #region ScanUwpAppsFromStartMenu

        /// <summary>
        /// 从开始菜单扫描 UWP 应用
        /// </summary>
        private async Task ScanUwpAppsFromStartMenu(List<AppInfo> apps)
        {
            try
            {
                // UWP 应用通常位于这些目录
                var startMenuPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs")
                };

                foreach (var startMenuPath in startMenuPaths)
                {
                    if (Directory.Exists(startMenuPath))
                    {
                        await ScanForAppxManifests(startMenuPath, apps);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从开始菜单扫描 UWP 应用时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 扫描 AppxManifest 文件来发现 UWP 应用
        /// </summary>
        private async Task ScanForAppxManifests(string directory, List<AppInfo> apps)
        {
            try
            {
                var appxManifestFiles = Directory.GetFiles(directory, "AppxManifest.xml", SearchOption.AllDirectories);

                foreach (var manifestFile in appxManifestFiles)
                {
                    try
                    {
                        var appInfo = await CreateAppInfoFromAppxManifest(manifestFile);
                        if (appInfo != null)
                        {
                            apps.Add(appInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理 AppxManifest 文件 {manifestFile} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描 AppxManifest 文件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从 AppxManifest 文件创建应用信息
        /// </summary>
        private async Task<AppInfo> CreateAppInfoFromAppxManifest(string manifestPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(manifestPath);
                var appExecutable = await FindUwpAppExecutable(directory);

                if (string.IsNullOrEmpty(appExecutable))
                    return null;

                var displayName = Path.GetFileName(directory) ?? "UWP 应用";
                var icon = await _iconExtractor.GetAppIconAsync(appExecutable);

                var appInfo = new AppInfo
                {
                    Id = $"UWP_{directory}",
                    Name = displayName,
                    DisplayName = displayName,
                    ExecutablePath = appExecutable,
                    Icon = icon
                };

                return appInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从 AppxManifest 创建应用信息失败 {manifestPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查找 UWP 应用的可执行文件
        /// </summary>
        private async Task<string> FindUwpAppExecutable(string appDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(appDirectory) || !Directory.Exists(appDirectory))
                    return null;

                // 查找常见的 UWP 应用可执行文件
                var possibleExecutables = new[]
                {
                    "ApplicationFrameHost.exe", // UWP 应用宿主
                    "App.exe",
                    "AppHost.exe",
                    "*.exe" // 任何可执行文件
                };

                foreach (var pattern in possibleExecutables)
                {
                    var files = Directory.GetFiles(appDirectory, pattern, SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        return files[0]; // 返回第一个找到的可执行文件
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找 UWP 应用可执行文件失败 {appDirectory}: {ex.Message}");
                return null;
            }
        }

        #endregion ScanUwpAppsFromStartMenu

        #region ScanUwpAppsFromRegistry

        /// <summary>
        /// 从注册表扫描 UWP 应用
        /// </summary>
        private async Task ScanUwpAppsFromRegistry(List<AppInfo> apps)
        {
            try
            {
                var registryPaths = new[]
                {
                    @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications"
                };

                foreach (var registryPath in registryPaths)
                {
                    try
                    {
                        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                        {
                            if (key != null)
                            {
                                foreach (var subKeyName in key.GetSubKeyNames())
                                {
                                    using (var subKey = key.OpenSubKey(subKeyName))
                                    {
                                        var displayName = subKey?.GetValue("DisplayName") as string;
                                        if (!string.IsNullOrEmpty(displayName))
                                        {
                                            var appInfo = new AppInfo
                                            {
                                                Id = $"UWP_REG_{subKeyName}",
                                                Name = displayName,
                                                DisplayName = displayName,
                                                ExecutablePath = $"UWP:{subKeyName}",
                                                Icon = await UwpIcoCreater.CreateUwpDefaultIcon()
                                            };

                                            apps.Add(appInfo);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"扫描注册表路径 {registryPath} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从注册表扫描 UWP 应用时出错: {ex.Message}");
            }
        }

        #endregion ScanUwpAppsFromRegistry
    }
}
