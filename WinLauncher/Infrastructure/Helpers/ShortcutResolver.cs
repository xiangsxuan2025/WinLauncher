using System.IO;

namespace WinLauncher.Infrastructure.Helpers
{
    internal static class ShortcutResolver
    {
        /// <summary>
        /// 解析快捷方式文件的目标路径
        /// 使用多种方法确保兼容性
        /// </summary>
        public static async Task<string> ResolveShortcutTarget(string lnkPath)
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
                return ResolveUsingCom(lnkPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析快捷方式目标失败 {lnkPath}: {ex.Message}");
                return lnkPath;
            }
        }// 在 WindowsAppScannerService 类中添加更好的图标处理

        private static string ResolveUsingCom(string lnkPath)
        {
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
                // todo: 处理异常或记录日志
            }

            return string.Empty;
        }
    }
}
