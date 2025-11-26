using System.Collections.Generic;
using System.Threading.Tasks;
using WinLauncher.Core.Entities;

namespace WinLauncher.Core.Interfaces
{
    public interface IScanStrategy
    {
        string StrategyName { get; }

        Task<List<AppInfo>> ScanAsync();
    }
}
