using System;
using System.Diagnostics;

namespace WinLauncher
{
    public class PerformanceMonitor
    {
        public void TrackScanPerformance(TimeSpan duration, int appCount)
        {
            Debug.WriteLine($"扫描性能: {appCount} 个应用, 耗时: {duration.TotalSeconds:F2}秒");

            if (duration.TotalSeconds > 10)
                Debug.WriteLine("警告: 应用扫描时间过长，建议优化");
        }

        public void TrackMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            Debug.WriteLine($"内存使用: {memoryMB} MB");
        }
    }
}
