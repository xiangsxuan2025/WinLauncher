namespace WinLauncher.Core.ValueObjects
{
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
