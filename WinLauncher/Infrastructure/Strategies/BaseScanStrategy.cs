using System.Collections.Generic;
using System.Threading.Tasks;
using WinLauncher.Core.Interfaces;
using WinLauncher.Core.Models;

namespace WinLauncher.Infrastructure.Strategies
{
    public abstract class BaseScanStrategy : IScanStrategy
    {
        public abstract string StrategyName { get; }

        protected readonly IconExtractorService IconExtractor;

        protected BaseScanStrategy(IconExtractorService iconExtractor)
        {
            IconExtractor = iconExtractor;
        }

        public abstract Task<List<AppInfo>> ScanAsync();

        protected void LogInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{StrategyName}] {message}");
        }

        protected void LogError(string message, Exception ex = null)
        {
            var errorMessage = $"[{StrategyName}] {message}";
            if (ex != null) errorMessage += $": {ex.Message}";
            System.Diagnostics.Debug.WriteLine(errorMessage);
        }
    }
}
