using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinLauncher.Core.Entities;
using WinLauncher.Infrastructure.Helpers;
using WinLauncher.Infrastructure.Services;

namespace WinLauncher.Infrastructure.Strategies
{
    internal class StoreAppScanStrategy : BaseScanStrategy
    {
        public override string StrategyName { get; } = "StoreAppScanStrategy";
        private IconExtractorService _iconExtractor;

        public StoreAppScanStrategy(IconExtractorService iconExtractor) : base(iconExtractor)
        {
            this._iconExtractor = iconExtractor;
        }

        public override async Task<List<AppInfo>> ScanAsync()
        {
            return await ScanStoreAppsAsync();
        }

        /// <summary>
        /// 扫描应用商店应用
        /// </summary>
        public async Task<List<AppInfo>> ScanStoreAppsAsync()
        {
            var storeApps = new List<AppInfo>();

            try
            {
                System.Diagnostics.Debug.WriteLine("开始扫描应用商店应用...");

                // 方法1: 扫描 WindowsApps 目录
                await ScanStoreAppsFromWindowsApps(storeApps);

                // 方法2: 通过注册表获取应用商店应用
                await ScanStoreAppsFromRegistry(storeApps);

                System.Diagnostics.Debug.WriteLine($"扫描到 {storeApps.Count} 个应用商店应用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描应用商店应用时出错: {ex.Message}");
            }

            return storeApps;
        }

        #region ScanStoreAppsFromWindowsApps

        /// <summary>
        /// 从 WindowsApps 目录扫描应用商店应用
        /// </summary>
        private async Task ScanStoreAppsFromWindowsApps(List<AppInfo> apps)
        {
            try
            {
                var windowsAppsPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages"),
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files", "WindowsApps")
                };

                foreach (var windowsAppsPath in windowsAppsPaths)
                {
                    if (Directory.Exists(windowsAppsPath))
                    {
                        await ScanStoreAppDirectories(windowsAppsPath, apps);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从 WindowsApps 目录扫描应用商店应用时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 扫描应用商店应用目录
        /// </summary>
        private async Task ScanStoreAppDirectories(string windowsAppsPath, List<AppInfo> apps)
        {
            try
            {
                var searchOptions = new EnumerationOptions
                {
                    MaxRecursionDepth = 2,
                    IgnoreInaccessible = true
                };

                var appDirectories = Directory.GetDirectories(windowsAppsPath, "*", searchOptions);

                foreach (var appDirectory in appDirectories)
                {
                    try
                    {
                        var appInfo = await CreateAppInfoFromStoreApp(appDirectory);
                        if (appInfo != null)
                        {
                            apps.Add(appInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理应用商店应用目录 {appDirectory} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描应用商店应用目录时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从应用商店应用目录创建应用信息
        /// </summary>
        private async Task<AppInfo> CreateAppInfoFromStoreApp(string appDirectory)
        {
            try
            {
                var directoryName = Path.GetFileName(appDirectory);
                if (string.IsNullOrEmpty(directoryName))
                    return null;

                // 查找应用可执行文件
                var exeFiles = Directory.GetFiles(appDirectory, "*.exe", SearchOption.AllDirectories)
                    .Where(f => !FileFilter.IsSystemOrInstallerFile(f))
                    .ToList();

                if (exeFiles.Count == 0)
                    return null;

                var mainExe = exeFiles.First();
                var displayName = CleanStoreAppName(directoryName);
                var icon = await _iconExtractor.GetAppIconAsync(mainExe);

                var appInfo = new AppInfo
                {
                    Id = $"Store_{appDirectory}",
                    Name = displayName,
                    DisplayName = displayName,
                    ExecutablePath = mainExe,
                    Icon = icon
                };

                return appInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从应用商店应用目录创建应用信息失败 {appDirectory}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清理应用商店应用名称
        /// </summary>
        private string CleanStoreAppName(string directoryName)
        {
            try
            {
                // 移除版本号和其他技术信息
                var cleanName = directoryName;

                // 移除类似 "_1.0.0.0_x64__8wekyb3d8bbwe" 这样的后缀
                var underscoreIndex = cleanName.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    cleanName = cleanName.Substring(0, underscoreIndex);
                }

                // 将 PascalCase 转换为可读的名称
                cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, "([a-z])([A-Z])", "$1 $2");

                return cleanName.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理应用商店应用名称失败: {ex.Message}");
                return directoryName;
            }
        }

        #endregion ScanStoreAppsFromWindowsApps

        #region ScanStoreAppsFromRegistry

        /// <summary>
        /// 从注册表扫描应用商店应用
        /// </summary>
        private async Task ScanStoreAppsFromRegistry(List<AppInfo> apps)
        {
            try
            {
                var registryPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
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
                                        var installLocation = subKey?.GetValue("InstallLocation") as string;
                                        var publisher = subKey?.GetValue("Publisher") as string;

                                        // 检查是否是应用商店应用（通常有特定的发布者或特征）
                                        if (!string.IsNullOrEmpty(displayName) &&
                                            !string.IsNullOrEmpty(installLocation) &&
                                            IsStoreApp(displayName, publisher, installLocation))
                                        {
                                            var appInfo = await new AppInfoCreator(_iconExtractor).CreateAppInfoFromInstallLocation(installLocation, displayName, null);
                                            if (appInfo != null)
                                            {
                                                apps.Add(appInfo);
                                            }
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
                System.Diagnostics.Debug.WriteLine($"从注册表扫描应用商店应用时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否是应用商店应用
        /// </summary>
        private bool IsStoreApp(string displayName, string publisher, string installLocation)
        {
            try
            {
                // 应用商店应用通常有这些特征
                var storeIndicators = new[]
                {
                    "Microsoft Corporation",
                    "WindowsStore",
                    "WindowsApps",
                    "Appx",
                    "MSIX"
                };

                if (publisher != null && storeIndicators.Any(indicator => publisher.Contains(indicator)))
                    return true;

                if (installLocation != null && storeIndicators.Any(indicator => installLocation.Contains(indicator)))
                    return true;

                // 检查是否在 WindowsApps 目录中
                if (installLocation != null && installLocation.Contains("WindowsApps"))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"判断应用商店应用失败: {ex.Message}");
                return false;
            }
        }

        #endregion ScanStoreAppsFromRegistry
    }
}
