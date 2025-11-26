using System.Windows.Media.Imaging;
using WinLauncher.Core.Entities;

namespace WinLauncher.Core.Interfaces
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
    }
}
