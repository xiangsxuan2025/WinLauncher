using WinLauncher.Core.Models;

/// <summary>
/// 应用信息比较器，用于去重操作
/// 通过应用的 Id 属性来判断两个 AppInfo 对象是否相等
/// </summary>
public class AppInfoComparer : IEqualityComparer<AppInfo>
{
    /// <summary>
    /// 比较两个 AppInfo 对象是否相等
    /// </summary>
    public bool Equals(AppInfo x, AppInfo y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id; // 基于应用路径判断是否相同应用
    }

    /// <summary>
    /// 获取 AppInfo 对象的哈希码
    /// </summary>
    public int GetHashCode(AppInfo obj)
    {
        return obj.Id?.GetHashCode() ?? 0;
    }
}
