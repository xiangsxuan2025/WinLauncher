using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinLauncher.Core.Entities;
using WinLauncher.Infrastructure.Services;

namespace WinLauncher.Infrastructure.Helpers
{
    // 应用信息
    internal class AppInfoCreator
    {
        private readonly IconExtractorService _iconExtractorService;

        public AppInfoCreator(IconExtractorService iconExtractorService)
        {
            this._iconExtractorService = iconExtractorService;
        }

        /// <summary>
        /// 从安装位置创建应用信息
        /// </summary>
        public async Task<AppInfo> CreateAppInfoFromInstallLocation(string installLocation, string displayName, string displayIcon)
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
                        .Where(f => !FileFilter.IsSystemOrInstallerFile(f))
                        .OrderBy(f => f.Length) // 通常主程序文件较小
                        .FirstOrDefault();

                    exePath = exeFiles;
                }

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var icon = await _iconExtractorService.GetAppIconAsync(exePath);

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
    }
}
