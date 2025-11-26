using System;
using System.Diagnostics;

namespace WinLauncher.Infrastructure.Services
{
    public class UsageAnalytics
    {
        public void TrackAppLaunch(string appName, TimeSpan loadTime)
        {
            Debug.WriteLine($"应用启动: {appName}, 加载时间: {loadTime.TotalMilliseconds}ms");
        }

        public void TrackSearchUsage(string searchTerm, int resultCount)
        {
            Debug.WriteLine($"搜索使用: '{searchTerm}', 结果数: {resultCount}");
        }
    }
}
