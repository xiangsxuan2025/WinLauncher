using Newtonsoft.Json;
using System.IO;
using WinLauncher.Models;

namespace WinLauncher
{
    public interface IDataService
    {
        Task SaveLayoutAsync(List<LaunchpadItem> items, List<FolderInfo> folders);

        Task<(List<LaunchpadItem> items, List<FolderInfo> folders)> LoadLayoutAsync();

        Task SaveSettingsAsync(AppSettings settings);

        Task<AppSettings> LoadSettingsAsync();
    }

    public class JsonDataService : IDataService
    {
        private readonly string _dataDirectory;
        private readonly string _layoutFile;
        private readonly string _settingsFile;

        public JsonDataService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LaunchNext");

            _layoutFile = Path.Combine(_dataDirectory, "layout.json");
            _settingsFile = Path.Combine(_dataDirectory, "settings.json");

            // 确保目录存在
            Directory.CreateDirectory(_dataDirectory);
        }

        public async Task SaveLayoutAsync(List<LaunchpadItem> items, List<FolderInfo> folders)
        {
            var layout = new { Items = items, Folders = folders };
            var json = JsonConvert.SerializeObject(layout, Formatting.Indented);
            await File.WriteAllTextAsync(_layoutFile, json);
        }

        public async Task<(List<LaunchpadItem> items, List<FolderInfo> folders)> LoadLayoutAsync()
        {
            if (!File.Exists(_layoutFile))
                return (new List<LaunchpadItem>(), new List<FolderInfo>());

            var json = await File.ReadAllTextAsync(_layoutFile);
            var layout = JsonConvert.DeserializeObject<LayoutData>(json);

            return (layout?.Items ?? new List<LaunchpadItem>(),
                    layout?.Folders ?? new List<FolderInfo>());
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsFile, json);
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFile))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(_settingsFile);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }

        private class LayoutData
        {
            public List<LaunchpadItem> Items { get; set; }
            public List<FolderInfo> Folders { get; set; }
        }
    }

    // Models/AppSettings.cs
    public class AppSettings
    {
        public bool ShowLabels { get; set; } = true;
        public double IconSize { get; set; } = 72;
        public bool EnableAnimations { get; set; } = true;
        public string Theme { get; set; } = "System";
        public bool StartWithWindows { get; set; } = false;
    }
}