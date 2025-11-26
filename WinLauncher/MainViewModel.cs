using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WinLauncher.Models;

namespace WinLauncher
{
    // ViewModels/MainViewModel.cs// ViewModels/MainViewModel.cs
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Input;

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAppScannerService _appScanner;
        private readonly IDataService _dataService;

        public ObservableCollection<LaunchpadItem> Items { get; set; }
        public ObservableCollection<FolderInfo> Folders { get; set; }

        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterItems();
                OnPropertyChanged();
            }
        }

        private int _currentPage;

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public ICommand LaunchAppCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand SearchCommand { get; }

        // ViewModels/MainViewModel.cs (新增命令)
        public ICommand ClearSearchCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand ItemClickCommand { get; } // 新增：统一的项目点击命令

        public MainViewModel(IAppScannerService appScanner, IDataService dataService)
        {
            _appScanner = appScanner;
            _dataService = dataService;

            Items = new ObservableCollection<LaunchpadItem>();
            Folders = new ObservableCollection<FolderInfo>();

            // 修正：正确初始化命令
            LaunchAppCommand = new RelayCommand<AppInfo>(LaunchApp);
            OpenFolderCommand = new RelayCommand<FolderInfo>(OpenFolder);
            SearchCommand = new RelayCommand(ExecuteSearch);
            ItemClickCommand = new RelayCommand<LaunchpadItem>(OnItemClicked); // 新增
            // 在构造函数中添加
            ClearSearchCommand = new RelayCommand(ClearSearch);
            OpenSettingsCommand = new RelayCommand(OpenSettings);

            LoadData();
        }

        // 新增：统一处理项目点击
        private void OnItemClicked(LaunchpadItem item)
        {
            if (item == null) return;

            switch (item.Type)
            {
                case ItemType.App:
                    LaunchApp(item.App);
                    break;

                case ItemType.Folder:
                    OpenFolder(item.Folder);
                    break;

                case ItemType.Empty:
                    // 空槽位，不执行任何操作
                    break;

                case ItemType.MissingApp:
                    // 缺失的应用，可以显示提示信息
                    System.Diagnostics.Debug.WriteLine($"应用已丢失: {item.DisplayName}");
                    break;
            }
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void OpenSettings()
        {
            // 打开设置窗口
            MessageBox.Show("设置功能开发中...", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void LoadData()
        {
            try
            {
                IsLoading = true;

                // 先显示加载状态
                Items.Clear();
                Items.Add(LaunchpadItem.CreateMessageItem("正在扫描应用..."));

                System.Diagnostics.Debug.WriteLine("=== 开始扫描所有应用 ===");

                var apps = await _appScanner.ScanInstalledAppsAsync();

                // 清除现有项目
                Items.Clear();

                if (apps.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"=== 扫描完成，共找到 {apps.Count} 个应用 ===");

                    // 统计不同类型应用的数量
                    var traditionalApps = apps.Count(a => !a.Id.StartsWith("UWP_") && !a.Id.StartsWith("Store_"));
                    var uwpApps = apps.Count(a => a.Id.StartsWith("UWP_"));
                    var storeApps = apps.Count(a => a.Id.StartsWith("Store_"));

                    System.Diagnostics.Debug.WriteLine($"传统桌面应用: {traditionalApps} 个");
                    System.Diagnostics.Debug.WriteLine($"UWP 应用: {uwpApps} 个");
                    System.Diagnostics.Debug.WriteLine($"应用商店应用: {storeApps} 个");

                    // 将应用转换为 LaunchpadItem
                    foreach (var app in apps)
                    {
                        var item = LaunchpadItem.CreateAppItem(app);
                        Items.Add(item);

                        // 调试信息
                        var appType = app.Id.StartsWith("UWP_") ? "UWP" :
                                     app.Id.StartsWith("Store_") ? "Store" : "Desktop";
                        System.Diagnostics.Debug.WriteLine($"{appType}应用: {app.DisplayName}, 路径: {app.ExecutablePath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== 扫描完成，未找到任何应用 ===");
                    Items.Add(LaunchpadItem.CreateMessageItem("未找到应用"));
                }

                // 加载保存的布局
                try
                {
                    var (savedItems, savedFolders) = await _dataService.LoadLayoutAsync();
                    if (savedItems.Count > 0)
                    {
                        MergeLayoutWithScannedApps(savedItems, apps);
                    }

                    foreach (var folder in savedFolders)
                        Folders.Add(folder);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载保存的布局时出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载数据时出错: {ex.Message}");
                Items.Clear();
                Items.Add(LaunchpadItem.CreateMessageItem($"加载失败: {ex.Message}"));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLoadingIndicator));
            }
        }

        public bool ShowLoadingIndicator => IsLoading;

        private void MergeLayoutWithScannedApps(List<LaunchpadItem> savedItems, List<AppInfo> scannedApps)
        {
            // 实现布局合并逻辑
            // 这里可以确保保存的布局项与扫描到的应用匹配
        }

        private void LaunchApp(AppInfo app)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = app.ExecutablePath,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                // 处理启动失败
                System.Diagnostics.Debug.WriteLine($"启动应用失败: {ex.Message}");
            }
        }

        private void OpenFolder(FolderInfo folder)
        {
            // 打开文件夹逻辑
            System.Diagnostics.Debug.WriteLine($"打开文件夹: {folder.Name}");
        }

        private void ExecuteSearch()
        {
            // 执行搜索逻辑
            FilterItems();
        }

        private void FilterItems()
        {
            // 实现搜索过滤逻辑
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // 显示所有项目
            }
            else
            {
                // 根据搜索文本过滤
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}