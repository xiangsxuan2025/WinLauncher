using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace WinLauncher.Core.Models
{
    /// <summary>
    /// 应用信息类
    /// 表示一个应用程序的基本信息
    /// </summary>
    public class AppInfo : INotifyPropertyChanged
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
}
