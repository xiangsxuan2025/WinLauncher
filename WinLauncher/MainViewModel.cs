using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WinLauncher.Core.Interfaces;
using WinLauncher.Core.Models;
using WinLauncher.Models;

namespace WinLauncher
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAppScannerService _appScanner;
        private readonly IDataService _dataService;
        private readonly UsageAnalytics _usageAnalytics;
        private readonly DispatcherTimer _searchTimer;
        private List<LaunchpadItem> _allItems = new List<LaunchpadItem>();

        public ObservableCollection<LaunchpadItem> Items { get; set; }
        public ObservableCollection<LaunchpadItem> SkeletonItems { get; set; }
        public ObservableCollection<FolderInfo> Folders { get; set; }

        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();

                // 搜索防抖
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }

        private int _currentPage;

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
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

        public ICommand LaunchAppCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ItemClickCommand { get; }

        public MainViewModel(IAppScannerService appScanner, IDataService dataService)
        {
            _appScanner = appScanner;
            _dataService = dataService;
            _usageAnalytics = new UsageAnalytics();
            // 搜索防抖定时器
            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                ExecuteSearch();
            };

            Items = new ObservableCollection<LaunchpadItem>();
            SkeletonItems = new ObservableCollection<LaunchpadItem>();
            Folders = new ObservableCollection<FolderInfo>();

            // 初始化骨架屏项目
            InitializeSkeletonItems();

            // 命令初始化
            LaunchAppCommand = new RelayCommand<AppInfo>(LaunchApp);
            OpenFolderCommand = new RelayCommand<FolderInfo>(OpenFolder);
            SearchCommand = new RelayCommand(ExecuteSearch);
            ItemClickCommand = new RelayCommand<LaunchpadItem>(OnItemClicked);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            OpenSettingsCommand = new RelayCommand(OpenSettings);

            LoadData();
        }

        private void InitializeSkeletonItems()
        {
            for (int i = 0; i < 20; i++)
            {
                SkeletonItems.Add(LaunchpadItem.CreateEmptyItem($"skeleton_{i}"));
            }
        }

        private async void LoadData()
        {
            try
            {
                IsLoading = true;
                Items.Clear();

                System.Diagnostics.Debug.WriteLine("=== 开始扫描所有应用 ===");

                var apps = await _appScanner.ScanInstalledAppsAsync();
                _allItems.Clear();

                if (apps.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"=== 扫描完成，共找到 {apps.Count} 个应用 ===");

                    // 将应用转换为 LaunchpadItem
                    foreach (var app in apps)
                    {
                        var item = LaunchpadItem.CreateAppItem(app);
                        _allItems.Add(item);
                    }

                    // 初始显示所有项目
                    UpdateItemsCollection(_allItems);
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

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                UpdateItemsCollection(_allItems);
            }
            else
            {
                FilterItems();
                _usageAnalytics.TrackSearchUsage(SearchText, Items.Count);
            }
        }

        /// <summary>
        /// 智能搜索过滤
        /// </summary>
        private void FilterItems()
        {
            var searchTerms = SearchText.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredItems = new List<LaunchpadItem>();

            foreach (var item in _allItems)
            {
                var score = CalculateSearchScore(item, searchTerms);
                if (score > 0.3) // 阈值可调整
                {
                    filteredItems.Add(item);
                }
            }

            // 按搜索分数排序
            var sortedItems = filteredItems
                .OrderByDescending(item => CalculateSearchScore(item, searchTerms))
                .ThenBy(item => item.DisplayName);

            UpdateItemsCollection(sortedItems);
        }

        /// <summary>
        /// 计算搜索分数
        /// </summary>
        private double CalculateSearchScore(LaunchpadItem item, string[] searchTerms)
        {
            if (item.Type != ItemType.App) return 0;

            var displayName = item.DisplayName?.ToLower() ?? "";
            double score = 0;

            foreach (var term in searchTerms)
            {
                if (displayName.Contains(term))
                {
                    // 完全匹配得分更高
                    if (displayName == term)
                        score += 1.0;
                    // 开头匹配得分较高
                    else if (displayName.StartsWith(term))
                        score += 0.8;
                    // 包含匹配得分一般
                    else
                        score += 0.5;
                }
            }

            return score / searchTerms.Length;
        }

        private void UpdateItemsCollection(IEnumerable<LaunchpadItem> items)
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            UpdateItemsCollection(_allItems);
        }

        private void OpenSettings()
        {
            // 打开设置窗口
            MessageBox.Show("设置功能开发中...", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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
                    break;

                case ItemType.MissingApp:
                    System.Diagnostics.Debug.WriteLine($"应用已丢失: {item.DisplayName}");
                    break;
            }
        }

        private void LaunchApp(AppInfo app)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = app.ExecutablePath,
                    UseShellExecute = true
                });

                stopwatch.Stop();
                _usageAnalytics.TrackAppLaunch(app.DisplayName, stopwatch.Elapsed);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动应用失败: {ex.Message}");
            }
        }

        private void OpenFolder(FolderInfo folder)
        {
            System.Diagnostics.Debug.WriteLine($"打开文件夹: {folder.Name}");
        }

        private void MergeLayoutWithScannedApps(List<LaunchpadItem> savedItems, List<AppInfo> scannedApps)
        {
            // 实现布局合并逻辑
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
