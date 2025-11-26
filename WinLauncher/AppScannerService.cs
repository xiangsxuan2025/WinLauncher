// Services/WindowsAppScannerService.cs
using System.IO;
using WinLauncher.Models;

namespace WinLauncher
{
    // Services/IAppScannerService.cs
    using System.Collections.Generic;
    using System.Security;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    public interface IAppScannerService
    {
        Task<List<AppInfo>> ScanInstalledAppsAsync();

        Task<BitmapImage> GetAppIconAsync(string executablePath);
    }

    public class WindowsAppScannerService : IAppScannerService
    {
        private readonly IconExtractorService _iconExtractor;

        public WindowsAppScannerService(IconExtractorService iconExtractor)
        {
            _iconExtractor = iconExtractor;
        }

        public async Task<List<AppInfo>> ScanInstalledAppsAsync()
        {
            var apps = new List<AppInfo>();

            // 只扫描当前用户有权限访问的目录
            await ScanSafeDirectories(apps);

            return apps.Distinct(new AppInfoComparer()).ToList();
        }

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

        private async Task ScanDirectoryForShortcuts(string directory, List<AppInfo> apps)
        {
            try
            {
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

        private async Task ScanCommonApplications(string programFilesPath, List<AppInfo> apps)
        {
            // 只扫描常见的应用程序目录，避免深度扫描整个程序文件目录
            var commonAppDirs = new[]
            {
            "Microsoft Office",
            "Google",
            "Mozilla Firefox",
            "Adobe",
            "VideoLAN",
            "Notepad++",
            "7-Zip",
            "WinRAR",
            "VLC",
            "Spotify",
            "Discord",
            "Slack",
            "Microsoft Edge",
            "Windows Media Player"
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

        private async Task ScanApplicationDirectory(string appDir, List<AppInfo> apps)
        {
            try
            {
                // 只扫描顶级目录和一级子目录，避免深度扫描
                var searchOptions = new EnumerationOptions
                {
                    MaxRecursionDepth = 1,
                    IgnoreInaccessible = true
                };

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

        private async Task<List<AppInfo>> GetInstalledAppsFromRegistry()
        {
            var apps = new List<AppInfo>();

            try
            {
                // 读取注册表中的已安装应用
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

                // 同样检查32位系统上的注册表
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

        private async Task<AppInfo> CreateAppInfoFromInstallLocation(string installLocation, string displayName, string displayIcon)
        {
            try
            {
                string exePath = null;

                // 如果有指定的图标路径，使用它
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
                        Id = exePath,
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

        // 原有的方法保持不变，但添加异常处理
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

        private async Task<AppInfo> CreateAppInfoFromExecutable(string exePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(exePath);
                var icon = await GetAppIconAsync(exePath);

                return new AppInfo
                {
                    Id = exePath,
                    Name = fileName,
                    DisplayName = fileName,
                    ExecutablePath = exePath,
                    Icon = icon
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理可执行文件失败 {exePath}: {ex.Message}");
            }

            return null;
        }

        // 原有的 ResolveShortcutTarget 和 GetAppIconAsync 方法保持不变
        public async Task<BitmapImage> GetAppIconAsync(string executablePath)
        {
            return await _iconExtractor.GetAppIconAsync(executablePath);
        }

        private async Task<string> ResolveShortcutTarget(string lnkPath)
        {
            // 原有的实现
            try
            {
                // 方法1: 尝试从 .lnk 文件所在目录查找同名的 .exe 文件
                var directory = Path.GetDirectoryName(lnkPath);
                var fileName = Path.GetFileNameWithoutExtension(lnkPath);
                var possibleExePath = Path.Combine(directory, fileName + ".exe");

                if (File.Exists(possibleExePath))
                    return possibleExePath;

                // 方法2: 使用 Windows Script Host
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

                return lnkPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析快捷方式目标失败 {lnkPath}: {ex.Message}");
                return lnkPath;
            }
        }
    }
}