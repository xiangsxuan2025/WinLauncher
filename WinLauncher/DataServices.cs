using Newtonsoft.Json;
using System.IO;
using WinLauncher.Models;

namespace WinLauncher
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

    /// <summary>
    /// JSON 数据服务实现
    /// 使用 JSON 文件存储应用数据和设置
    /// </summary>
    public class JsonDataService : IDataService
    {
        private readonly string _dataDirectory;
        private readonly string _layoutFile;
        private readonly string _settingsFile;

        public JsonDataService()
        {
            // 使用 AppData 目录存储应用数据
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LaunchNext");

            _layoutFile = Path.Combine(_dataDirectory, "layout.json");
            _settingsFile = Path.Combine(_dataDirectory, "settings.json");

            // 确保数据目录存在
            Directory.CreateDirectory(_dataDirectory);
        }

        /// <summary>
        /// 保存布局到 JSON 文件
        /// </summary>
        public async Task SaveLayoutAsync(List<LaunchpadItem> items, List<FolderInfo> folders)
        {
            var layout = new { Items = items, Folders = folders };
            var json = JsonConvert.SerializeObject(layout, Formatting.Indented);
            await File.WriteAllTextAsync(_layoutFile, json);
        }

        /// <summary>
        /// 从 JSON 文件加载布局
        /// </summary>
        public async Task<(List<LaunchpadItem> items, List<FolderInfo> folders)> LoadLayoutAsync()
        {
            if (!File.Exists(_layoutFile))
                return (new List<LaunchpadItem>(), new List<FolderInfo>());

            var json = await File.ReadAllTextAsync(_layoutFile);
            var layout = JsonConvert.DeserializeObject<LayoutData>(json);

            return (layout?.Items ?? new List<LaunchpadItem>(),
                    layout?.Folders ?? new List<FolderInfo>());
        }

        /// <summary>
        /// 保存设置到 JSON 文件
        /// </summary>
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsFile, json);
        }

        /// <summary>
        /// 从 JSON 文件加载设置
        /// </summary>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFile))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(_settingsFile);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }

        /// <summary>
        /// 布局数据内部类，用于 JSON 序列化
        /// </summary>
        private class LayoutData
        {
            public List<LaunchpadItem> Items { get; set; }
            public List<FolderInfo> Folders { get; set; }
        }
    }

    /// <summary>
    /// 应用设置类
    /// 包含用户可配置的各种选项
    /// </summary>
    public class AppSettings
    {
        public bool ShowLabels { get; set; } = true; // 是否显示应用标签
        public double IconSize { get; set; } = 72; // 图标大小
        public bool EnableAnimations { get; set; } = true; // 是否启用动画
        public string Theme { get; set; } = "System"; // 主题设置
        public bool StartWithWindows { get; set; } = false; // 是否开机启动
    }
}