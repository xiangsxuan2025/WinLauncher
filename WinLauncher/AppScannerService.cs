using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Media.Imaging;
using WinLauncher.Core.Interfaces;
using WinLauncher.Core.Models;
using WinLauncher.Infrastructure.Helpers;
using WinLauncher.Infrastructure.Strategies;

namespace WinLauncher
{
    /// <summary>
    /// Windows 应用扫描服务实现
    /// 负责从多个来源扫描和发现已安装的应用程序
    /// </summary>
    public class WindowsAppScannerService : IAppScannerService
    {
        private readonly IconExtractorService _iconExtractor;
        private readonly PerformanceMonitor _performanceMonitor;

        public WindowsAppScannerService(IconExtractorService iconExtractor)
        {
            _iconExtractor = iconExtractor;
            _performanceMonitor = new PerformanceMonitor();
        }

        /// <summary>
        /// 并行扫描已安装的应用
        /// </summary>
        public async Task<List<AppInfo>> ScanInstalledAppsAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var tasks = new[]
                {
                    Task.Run(() => new DesktopScanStrategy(_iconExtractor).ScanAsync()),
                    Task.Run(() => new UwpScanStrategy(_iconExtractor).ScanAsync()),
                    Task.Run(() =>  new StoreAppScanStrategy(_iconExtractor).ScanAsync())
                };

                var results = await Task.WhenAll(tasks);
                var allApps = results.SelectMany(x => x).ToList();

                var distinctApps = allApps.Distinct(new AppInfoComparer()).ToList();

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
