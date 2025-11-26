using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Media.Imaging;
using WinLauncher.Core.Interfaces;
using WinLauncher.Core.Models;

namespace WinLauncher
{
    /// <summary>
    /// Windows 应用扫描服务实现
    /// 负责从多个来源扫描和发现已安装的应用程序
    /// </summary>
    public class WindowsAppScannerService : IAppScannerService
    {
        private readonly IconExtractorService _iconExtractor;
        private readonly PerformanceMonitor _performanceMonitor;

        public WindowsAppScannerService(IconExtractorService iconExtractor)
        {
            _iconExtractor = iconExtractor;
            _performanceMonitor = new PerformanceMonitor();
        }

        /// <summary>
        /// 并行扫描已安装的应用
        /// </summary>
        public async Task<List<AppInfo>> ScanInstalledAppsAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var tasks = new[]
                {
                    Task.Run(() => ScanSafeDirectories()),
                    Task.Run(() => ScanUwpAppsAsync()),
                    Task.Run(() => ScanStoreAppsAsync())
                };

                var results = await Task.WhenAll(tasks);
                var allApps = results.SelectMany(x => x).ToList();

                var distinctApps = allApps.Distinct(new AppInfoComparer()).ToList();

                stopwatch.Stop();
                _performanceMonitor.TrackScanPerformance(stopwatch.Elapsed, distinctApps.Count);

                return distinctApps;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"扫描应用失败: {ex.Message}");
                return new List<AppInfo>();
            }
        }

        /// <summary>
        /// 并行扫描安全目录
        /// </summary>
        private async Task<List<AppInfo>> ScanSafeDirectories()
        {
            var tasks = new[]
            {
                ScanCurrentUserStartMenu(),
                ScanDesktopShortcuts(),
                ScanCommonProgramFiles(),
                ScanFromRegistry()
            };

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(x => x).ToList();
        }

        /// <summary>
        /// 扫描当前用户的开始菜单目录
        /// </summary>
        private async Task<List<AppInfo>> ScanCurrentUserStartMenu()
        {
            var apps = new List<AppInfo>();
            try
            {
                var userStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                if (!string.IsNullOrEmpty(userStartMenu) && Directory.Exists(userStartMenu))
                {
                    await ScanDirectoryForShortcuts(userStartMenu, apps);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描用户开始菜单时出错: {ex.Message}");
            }
            return apps;
        }

        /// <summary>
        /// 扫描桌面快捷方式
        /// </summary>
        private async Task<List<AppInfo>> ScanDesktopShortcuts()
        {
            var apps = new List<AppInfo>();
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (!string.IsNullOrEmpty(desktopPath) && Directory.Exists(desktopPath))
                {
                    await ScanDirectoryForShortcuts(desktopPath, apps);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描桌面快捷方式时出错: {ex.Message}");
            }
            return apps;
        }

        /// <summary>
        /// 扫描程序文件目录（Program Files）
        /// </summary>
        private async Task<List<AppInfo>> ScanCommonProgramFiles()
        {
            var apps = new List<AppInfo>();
            var programFilesPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (var programFilesPath in programFilesPaths)
            {
                try
                {
                    if (Directory.Exists(programFilesPath))
                    {
                        await ScanCommonApplications(programFilesPath, apps);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"扫描程序文件目录 {programFilesPath} 时出错: {ex.Message}");
                }
            }
            return apps;
        }

        /// <summary>
        /// 扫描指定目录中的快捷方式文件 (.lnk)
        /// </summary>
        private async Task ScanDirectoryForShortcuts(string directory, List<AppInfo> apps)
        {
            try
            {
                // 递归搜索所有 .lnk 文件
                var lnkFiles = Directory.GetFiles(directory, "*.lnk", SearchOption.AllDirectories);

                foreach (var lnkFile in lnkFiles)
                {
                    try
                    {
                        var appInfo = await CreateAppInfoFromShortcut(lnkFile);
                        if (appInfo != null)
                            apps.Add(appInfo);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理快捷方式 {lnkFile} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描目录 {directory} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 扫描常见的应用程序目录
        /// 避免深度扫描整个 Program Files 目录，提高性能
        /// </summary>
        private async Task ScanCommonApplications(string programFilesPath, List<AppInfo> apps)
        {
            // 只扫描常见的应用程序目录，避免深度扫描整个程序文件目录
            var commonAppDirs = new[]
            {
                "Microsoft Office", "Google", "Mozilla Firefox", "Adobe", "VideoLAN",
                "Notepad++", "7-Zip", "WinRAR", "VLC", "Spotify", "Discord", "Slack",
                "Microsoft Edge", "Windows Media Player"
            };

            foreach (var appDirName in commonAppDirs)
            {
                var appDir = Path.Combine(programFilesPath, appDirName);
                if (Directory.Exists(appDir))
                {
                    await ScanApplicationDirectory(appDir, apps);
                }
            }
        }

        /// <summary>
        /// 扫描应用程序目录中的可执行文件
        /// </summary>
        private async Task ScanApplicationDirectory(string appDir, List<AppInfo> apps)
        {
            try
            {
                // 限制扫描深度，只扫描顶级目录和一级子目录
                var searchOptions = new EnumerationOptions
                {
                    MaxRecursionDepth = 1, // 最大递归深度为1
                    IgnoreInaccessible = true // 忽略无法访问的目录
                };

                // 获取所有 .exe 文件，并过滤掉系统文件和安装程序
                var exeFiles = Directory.GetFiles(appDir, "*.exe", searchOptions)
                    .Where(f => !IsSystemOrInstallerFile(f));

                foreach (var exeFile in exeFiles)
                {
                    try
                    {
                        var appInfo = await CreateAppInfoFromExecutable(exeFile);
                        if (appInfo != null)
                            apps.Add(appInfo);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理可执行文件 {exeFile} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描应用程序目录 {appDir} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断文件是否为系统文件或安装程序
        /// 用于过滤掉不需要显示的可执行文件
        /// </summary>
        private bool IsSystemOrInstallerFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();

            // 排除安装程序、卸载程序、系统文件等
            var excludedKeywords = new[]
            {
                "uninstall", "install", "setup", "update", "patch",
                "helper", "service", "runtime", "launcher", "crash",
                "debug", "diagnostics", "repair", "unins"
            };

            return excludedKeywords.Any(keyword => fileName.Contains(keyword));
        }

        /// <summary>
        /// 从 Windows 注册表中扫描已安装的应用信息
        /// </summary>
        private async Task<List<AppInfo>> ScanFromRegistry()
        {
            var apps = new List<AppInfo>();
            try
            {
                var installedApps = await GetInstalledAppsFromRegistry();
                apps.AddRange(installedApps);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从注册表扫描应用时出错: {ex.Message}");
            }
            return apps;
        }

        /// <summary>
        /// 从注册表的卸载信息中获取已安装应用
        /// </summary>
        private async Task<List<AppInfo>> GetInstalledAppsFromRegistry()
        {
            var apps = new List<AppInfo>();

            try
            {
                // 读取 64 位系统的注册表项
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName") as string;
                                var installLocation = subKey?.GetValue("InstallLocation") as string;
                                var displayIcon = subKey?.GetValue("DisplayIcon") as string;

                                // 验证必要的应用信息
                                if (!string.IsNullOrEmpty(displayName) &&
                                    !string.IsNullOrEmpty(installLocation) &&
                                    Directory.Exists(installLocation))
                                {
                                    var appInfo = await CreateAppInfoFromInstallLocation(installLocation, displayName, displayIcon);
                                    if (appInfo != null)
                                        apps.Add(appInfo);
                                }
                            }
                        }
                    }
                }

                // 读取 32 位系统上的注册表项（在 64 位系统上）
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName") as string;
                                var installLocation = subKey?.GetValue("InstallLocation") as string;
                                var displayIcon = subKey?.GetValue("DisplayIcon") as string;

                                if (!string.IsNullOrEmpty(displayName) &&
                                    !string.IsNullOrEmpty(installLocation) &&
                                    Directory.Exists(installLocation))
                                {
                                    var appInfo = await CreateAppInfoFromInstallLocation(installLocation, displayName, displayIcon);
                                    if (appInfo != null)
                                        apps.Add(appInfo);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取注册表时出错: {ex.Message}");
            }

            return apps;
        }

        /// <summary>
        /// 从安装位置创建应用信息
        /// </summary>
        private async Task<AppInfo> CreateAppInfoFromInstallLocation(string installLocation, string displayName, string displayIcon)
        {
            try
            {
                string exePath = null;

                // 优先使用注册表中指定的图标路径
                if (!string.IsNullOrEmpty(displayIcon) && File.Exists(displayIcon))
                {
                    exePath = displayIcon;
                }
                else
                {
                    // 否则在安装目录中查找主要的可执行文件
                    var exeFiles = Directory.GetFiles(installLocation, "*.exe")
                        .Where(f => !IsSystemOrInstallerFile(f))
                        .OrderBy(f => f.Length) // 通常主程序文件较小
                        .FirstOrDefault();

                    exePath = exeFiles;
                }

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var icon = await GetAppIconAsync(exePath);

                    return new AppInfo
                    {
                        Id = exePath, // 使用可执行文件路径作为唯一标识
                        Name = displayName,
                        DisplayName = displayName,
                        ExecutablePath = exePath,
                        Icon = icon
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从安装位置创建应用信息失败 {installLocation}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 从快捷方式文件 (.lnk) 创建应用信息
        /// </summary>
        private async Task<AppInfo> CreateAppInfoFromShortcut(string lnkPath)
        {
            try
            {
                var targetPath = await ResolveShortcutTarget(lnkPath);

                if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(lnkPath);
                    var icon = await GetAppIconAsync(targetPath);

                    return new AppInfo
                    {
                        Id = targetPath,
                        Name = fileName,
                        DisplayName = fileName,
                        ExecutablePath = targetPath,
                        Icon = icon
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理快捷方式失败 {lnkPath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取应用的图标
        /// </summary>
        public async Task<BitmapImage> GetAppIconAsync(string executablePath)
        {
            return await _iconExtractor.GetAppIconAsync(executablePath);
        }

        /// <summary>
        /// 解析快捷方式文件的目标路径
        /// 使用多种方法确保兼容性
        /// </summary>
        private async Task<string> ResolveShortcutTarget(string lnkPath)
        {
            try
            {
                // 方法1: 尝试从 .lnk 文件所在目录查找同名的 .exe 文件
                var directory = Path.GetDirectoryName(lnkPath);
                var fileName = Path.GetFileNameWithoutExtension(lnkPath);
                var possibleExePath = Path.Combine(directory, fileName + ".exe");

                if (File.Exists(possibleExePath))
                    return possibleExePath;

                // 方法2: 使用 Windows Script Host (COM)
                try
                {
                    var shellType = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(lnkPath);
                    string targetPath = shortcut.TargetPath;

                    if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                        return targetPath;
                }
                catch
                {
                    // 如果 COM 方法失败，回退到其他方法
                }

                return lnkPath; // 如果所有方法都失败，返回原始路径
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析快捷方式目标失败 {lnkPath}: {ex.Message}");
                return lnkPath;
            }
        }// 在 WindowsAppScannerService 类中添加更好的图标处理

        private async Task<AppInfo> CreateAppInfoFromExecutable(string exePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(exePath);

                // 先创建基本的 AppInfo，设置一个临时的加载图标
                var appInfo = new AppInfo
                {
                    Id = exePath,
                    Name = fileName,
                    DisplayName = fileName,
                    ExecutablePath = exePath
                };

                // 先设置一个加载图标
                var loadingIcon = CreateLoadingIcon();
                if (loadingIcon != null)
                {
                    appInfo.Icon = loadingIcon;
                }

                // 异步加载真实图标
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var icon = await GetAppIconAsync(exePath);
                        if (icon != null)
                        {
                            // 在 UI 线程更新图标
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                appInfo.UpdateIcon(icon); // 使用新的方法来更新图标
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"异步加载图标失败 {exePath}: {ex.Message}");
                    }
                });

                return appInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理可执行文件失败 {exePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 创建加载中的占位图标
        /// </summary>
        private BitmapImage CreateLoadingIcon()
        {
            try
            {
                var bitmap = new System.Drawing.Bitmap(64, 64);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                using (var stream = new MemoryStream())
                {
                    graphics.Clear(System.Drawing.Color.LightGray);

                    // 绘制加载文本
                    using (var font = new System.Drawing.Font("Arial", 8))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                    {
                        var text = "Loading...";
                        var textSize = graphics.MeasureString(text, font);
                        graphics.DrawString(text, font, brush,
                            (64 - textSize.Width) / 2,
                            (64 - textSize.Height) / 2);
                    }

                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建加载图标失败: {ex.Message}");

                // 回退方案 - 创建简单的灰色图标
                try
                {
                    var fallback = new BitmapImage();
                    fallback.BeginInit();
                    // 创建一个简单的 1x1 灰色图像
                    byte[] grayPixel = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
                0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00,
                0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0x99, 0x63, 0xF8, 0xCF,
                0xC0, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
                    fallback.StreamSource = new MemoryStream(grayPixel);
                    fallback.CacheOption = BitmapCacheOption.OnLoad;
                    fallback.EndInit();
                    fallback.Freeze();
                    return fallback;
                }
                catch
                {
                    return null;
                }
            }
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
                var icon = await GetAppIconAsync(appExecutable);

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
                                    Icon = await CreateUwpDefaultIcon()
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
                                                Icon = await CreateUwpDefaultIcon()
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

        /// <summary>
        /// 创建 UWP 应用默认图标
        /// </summary>
        private async Task<BitmapImage> CreateUwpDefaultIcon()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var bitmap = new System.Drawing.Bitmap(64, 64);
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    using (var stream = new MemoryStream())
                    {
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        graphics.Clear(System.Drawing.Color.FromArgb(0, 120, 215)); // UWP 主题蓝色

                        // 绘制 UWP 风格的方块
                        var rect = new System.Drawing.Rectangle(8, 8, 48, 48);
                        graphics.FillRectangle(System.Drawing.Brushes.White, rect);

                        // 绘制 UWP 字样
                        using (var font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold))
                        using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215)))
                        {
                            var text = "UWP";
                            var textSize = graphics.MeasureString(text, font);
                            graphics.DrawString(text, font, brush,
                                (64 - textSize.Width) / 2,
                                (64 - textSize.Height) / 2);
                        }

                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        stream.Position = 0;

                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = stream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        return bitmapImage;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"创建 UWP 默认图标失败: {ex.Message}");
                    return null;
                }
            });
        }

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
                    .Where(f => !IsSystemOrInstallerFile(f))
                    .ToList();

                if (exeFiles.Count == 0)
                    return null;

                var mainExe = exeFiles.First();
                var displayName = CleanStoreAppName(directoryName);
                var icon = await GetAppIconAsync(mainExe);

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
                                            var appInfo = await CreateAppInfoFromInstallLocation(installLocation, displayName, null);
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
    }
}
