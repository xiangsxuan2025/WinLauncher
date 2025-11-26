using System.Collections.Generic;
using System.Threading.Tasks;
using WinLauncher.Core.Models;

namespace WinLauncher.Core.Interfaces
{
    public interface IScanStrategy
    {
        string StrategyName { get; }

        Task<List<AppInfo>> ScanAsync();
    }
}
