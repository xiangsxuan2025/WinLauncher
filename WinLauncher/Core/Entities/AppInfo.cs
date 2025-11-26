using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace WinLauncher.Core.Entities
{
    /// <summary>
    /// 应用信息类
    /// 表示一个应用程序的基本信息
    /// </summary>
    public class AppInfo : ObservableObject
    {
        private string _id;
        private string _name;
        private string _displayName;
        private string _executablePath;
        private BitmapImage _icon;
        private bool _isSystemApp;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(); }
        }

        public BitmapImage Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public bool IsSystemApp
        {
            get => _isSystemApp;
            set { _isSystemApp = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 安全地更新图标属性（从外部调用）
        /// </summary>
        public void UpdateIcon(BitmapImage newIcon)
        {
            _icon = newIcon;
            OnPropertyChanged(nameof(Icon));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 应用信息比较器，用于去重操作
    /// 通过应用的 Id 属性来判断两个 AppInfo 对象是否相等
    /// </summary>
    public class AppInfoEqualityComparer : IEqualityComparer<AppInfo>
    {
        /// <summary>
        /// 比较两个 AppInfo 对象是否相等
        /// </summary>
        public bool Equals(AppInfo x, AppInfo y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Id == y.Id; // 基于应用路径判断是否相同应用
        }

        /// <summary>
        /// 获取 AppInfo 对象的哈希码
        /// </summary>
        public int GetHashCode(AppInfo obj)
        {
            return obj.Id?.GetHashCode() ?? 0;
        }
    }
}
