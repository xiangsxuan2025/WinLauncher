using System.IO;

namespace WinLauncher.Infrastructure.Helpers
{
    internal static class FileFilter
    {
        /// <summary>
        /// 判断文件是否为系统文件或安装程序
        /// 用于过滤掉不需要显示的可执行文件
        /// </summary>
        public static bool IsSystemOrInstallerFile(string filePath)
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
    }
}
