// Services/WindowsAppScannerService.cs
using WinLauncher.Models;

// 应用信息比较器（去重）
public class AppInfoComparer : IEqualityComparer<AppInfo>
{
    public bool Equals(AppInfo x, AppInfo y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id;
    }

    public int GetHashCode(AppInfo obj)
    {
        return obj.Id?.GetHashCode() ?? 0;
    }
}