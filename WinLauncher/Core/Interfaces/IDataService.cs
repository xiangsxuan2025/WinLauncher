using WinLauncher.Models;

namespace WinLauncher.Core.Interfaces
{
    /// <summary>
    /// 数据服务接口定义
    /// 负责应用布局和设置的持久化存储
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// 保存启动台布局（应用项和文件夹）
        /// </summary>
        Task SaveLayoutAsync(List<LaunchpadItem> items, List<FolderInfo> folders);

        /// <summary>
        /// 加载启动台布局
        /// </summary>
        Task<(List<LaunchpadItem> items, List<FolderInfo> folders)> LoadLayoutAsync();

        /// <summary>
        /// 保存应用设置
        /// </summary>
        Task SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// 加载应用设置
        /// </summary>
        Task<AppSettings> LoadSettingsAsync();
    }
}
