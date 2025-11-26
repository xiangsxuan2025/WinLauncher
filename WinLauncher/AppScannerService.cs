using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Media.Imaging;
using WinLauncher.Models;

namespace WinLauncher
{
    /// <summary>
    /// 应用扫描服务接口定义
    /// </summary>
    public interface IAppScannerService
    {
        /// <summary>
        /// 异步扫描系统中已安装的应用
        /// </summary>
        Task<List<AppInfo>> ScanInstalledAppsAsync();

        /// <summary>
        /// 异步获取应用的图标
        /// </summary>
        Task<BitmapImage> GetAppIconAsync(string executablePath);
    }

    /// <summary>
    /// Windows 应用扫描服务实现
    /// 负责从多个来源扫描和发现已安装的应用程序
    /// </summary>
    public class WindowsAppScannerService : IAppScannerService
    {
        private readonly IconExtractorService _iconExtractor;

        public WindowsAppScannerService(IconExtractorService iconExtractor)
        {
            _iconExtractor = iconExtractor;
        }

        /// <summary>
        /// 扫描已安装的应用，并从多个来源收集应用信息
        /// </summary>
        public async Task<List<AppInfo>> ScanInstalledAppsAsync()
        {
            var apps = new List<AppInfo>();

            // 扫描安全的目录（避免权限问题）
            await ScanSafeDirectories(apps);

            // 使用比较器去重后返回
            return apps.Distinct(new AppInfoComparer()).ToList();
        }

        /// <summary>
        /// 扫描安全的目录来发现应用
        /// 包括：开始菜单、桌面快捷方式、程序文件目录、注册表
        /// </summary>
        private async Task ScanSafeDirectories(List<AppInfo> apps)
        {
            // 1. 扫描当前用户的开始菜单（通常有权限）
            await ScanCurrentUserStartMenu(apps);

            // 2. 扫描桌面快捷方式
            await ScanDesktopShortcuts(apps);

            // 3. 扫描程序文件目录（只扫描常见应用，避免权限问题）
            await ScanCommonProgramFiles(apps);

            // 4. 使用注册表获取已安装应用信息
            await ScanFromRegistry(apps);
        }

        /// <summary>
        /// 扫描当前用户的开始菜单目录
        /// </summary>
        private async Task ScanCurrentUserStartMenu(List<AppInfo> apps)
        {
            try
            {
                var userStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                if (!string.IsNullOrEmpty(userStartMenu) && Directory.Exists(userStartMenu))
                {
                    await ScanDirectoryForShortcuts(userStartMenu, apps);
                }
            }
            catch (SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"安全异常 - 用户开始菜单: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"访问被拒绝 - 用户开始菜单: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描用户开始菜单时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 扫描桌面快捷方式
        /// </summary>
        private async Task ScanDesktopShortcuts(List<AppInfo> apps)
        {
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
        }

        /// <summary>
        /// 扫描程序文件目录（Program Files）
        /// </summary>
        private async Task ScanCommonProgramFiles(List<AppInfo> apps)
        {
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
                catch (SecurityException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"安全异常 - 程序文件目录 {programFilesPath}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"访问被拒绝 - 程序文件目录 {programFilesPath}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"扫描程序文件目录 {programFilesPath} 时出错: {ex.Message}");
                }
            }
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
        private async Task ScanFromRegistry(List<AppInfo> apps)
        {
            try
            {
                // 使用注册表获取已安装的应用信息（更安全的方式）
                var installedApps = await GetInstalledAppsFromRegistry();
                apps.AddRange(installedApps);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从注册表扫描应用时出错: {ex.Message}");
            }
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
    }
}