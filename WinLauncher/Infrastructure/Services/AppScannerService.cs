using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Media.Imaging;
using WinLauncher.Core.Entities;
using WinLauncher.Core.Interfaces;
using WinLauncher.Infrastructure.Helpers;
using WinLauncher.Infrastructure.Strategies;

namespace WinLauncher.Infrastructure.Services
{
    /// <summary>
    /// Windows 应用扫描服务实现
    /// 负责从多个来源扫描和发现已安装的应用程序
    /// </summary>
    public class WindowsAppScannerService : IAppScannerService
    {
        private readonly IconExtractorService _iconExtractor;
        private readonly PerformanceMonitor _performanceMonitor;

        private readonly List<IScanStrategy> _scanStrategies;

        public WindowsAppScannerService(IconExtractorService iconExtractor,
        PerformanceMonitor performanceMonitor)
        {
            _iconExtractor = iconExtractor;
            _performanceMonitor = performanceMonitor;
            _scanStrategies = new List<IScanStrategy>
            {
                new DesktopScanStrategy(iconExtractor),
                new UwpScanStrategy(iconExtractor),
                new StoreAppScanStrategy(iconExtractor),
            };
        }

        /// <summary>
        /// 并行扫描已安装的应用
        /// </summary>
        public async Task<List<AppInfo>> ScanInstalledAppsAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var scanTasks = _scanStrategies.Select(strategy => strategy.ScanAsync()).ToArray();
                var results = await Task.WhenAll(scanTasks);
                var allApps = results.SelectMany(x => x).ToList();

                var distinctApps = allApps.Distinct(new AppInfoEqualityComparer()).ToList();

                stopwatch.Stop();
                _performanceMonitor.TrackScanPerformance(stopwatch.Elapsed, distinctApps.Count);

                return distinctApps;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"扫描应用失败: {ex.Message}");
                return new List<AppInfo>();
            }
        }
    }
}
