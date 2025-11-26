using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace WinLauncher.Models
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

    /// <summary>
    /// 文件夹信息类
    /// 表示一个包含多个应用的文件夹
    /// </summary>
    public class FolderInfo : INotifyPropertyChanged
    {
        public string Id { get; set; } // 文件夹唯一标识符
        public string Name { get; set; } // 文件夹名称
        public ObservableCollection<AppInfo> Apps { get; set; } // 文件夹中的应用集合
        public BitmapImage FolderIcon { get; set; } // 文件夹图标

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// 启动台项目类
    /// 表示启动台中的一个项目，可以是应用、文件夹、空位或丢失的应用
    /// 支持多种类型，统一管理
    /// </summary>
    public class LaunchpadItem : INotifyPropertyChanged
    {
        private ItemType _type;
        private AppInfo _app;
        private FolderInfo _folder;
        private string _emptyToken;
        private string _customDisplayName;
        private MissingAppPlaceholder _missingApp;

        /// <summary>
        /// 项目类型
        /// </summary>
        public ItemType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(Icon));
            }
        }

        /// <summary>
        /// 应用信息（当 Type 为 App 时有效）
        /// </summary>
        public AppInfo App
        {
            get => _app;
            set
            {
                _app = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(Icon));
            }
        }

        /// <summary>
        /// 文件夹信息（当 Type 为 Folder 时有效）
        /// </summary>
        public FolderInfo Folder
        {
            get => _folder;
            set
            {
                _folder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(Icon));
            }
        }

        /// <summary>
        /// 空位标识（当 Type 为 Empty 时有效）
        /// </summary>
        public string EmptyToken
        {
            get => _emptyToken;
            set
            {
                _emptyToken = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// 丢失的应用占位符（当 Type 为 MissingApp 时有效）
        /// </summary>
        public MissingAppPlaceholder MissingApp
        {
            get => _missingApp;
            set
            {
                _missingApp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(Icon));
            }
        }

        /// <summary>
        /// 自定义显示名称（主要用于 Empty 类型）
        /// </summary>
        public string CustomDisplayName
        {
            get => _customDisplayName;
            set
            {
                _customDisplayName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string Id
        {
            get
            {
                return Type switch
                {
                    ItemType.App => $"app_{App?.Id}",
                    ItemType.Folder => $"folder_{Folder?.Id}",
                    ItemType.Empty => $"empty_{EmptyToken}",
                    ItemType.MissingApp => $"missing_{MissingApp?.Id}",
                    _ => "unknown"
                };
            }
        }

        /// <summary>
        /// 显示名称
        /// 根据类型返回相应的显示名称
        /// </summary>
        public string DisplayName
        {
            get
            {
                // 如果设置了自定义显示名称，优先使用它
                if (!string.IsNullOrEmpty(_customDisplayName))
                    return _customDisplayName;

                return Type switch
                {
                    ItemType.App => App?.DisplayName ?? "未知应用",
                    ItemType.Folder => Folder?.Name ?? "未命名文件夹",
                    ItemType.Empty => string.Empty,
                    ItemType.MissingApp => MissingApp?.DisplayName ?? "丢失的应用",
                    _ => "未知项目"
                };
            }
        }

        /// <summary>
        /// 项目图标
        /// 根据类型返回相应的图标
        /// </summary>
        public BitmapImage Icon
        {
            get
            {
                return Type switch
                {
                    ItemType.App => App?.Icon,
                    ItemType.Folder => Folder?.FolderIcon,
                    ItemType.Empty => CreateTransparentIcon(),
                    ItemType.MissingApp => MissingApp?.Icon,
                    _ => CreateDefaultIcon()
                };
            }
        }

        public AppInfo AppInfoIfApp => Type == ItemType.App ? App : null;
        public FolderInfo FolderInfoIfFolder => Type == ItemType.Folder ? Folder : null;

        // 静态创建方法，添加对自定义显示名称的支持
        public static LaunchpadItem CreateAppItem(AppInfo app)
        {
            return new LaunchpadItem { Type = ItemType.App, App = app };
        }

        public static LaunchpadItem CreateFolderItem(FolderInfo folder)
        {
            return new LaunchpadItem { Type = ItemType.Folder, Folder = folder };
        }

        public static LaunchpadItem CreateEmptyItem(string token = "", string customDisplayName = "")
        {
            return new LaunchpadItem
            {
                Type = ItemType.Empty,
                EmptyToken = token,
                CustomDisplayName = customDisplayName
            };
        }

        /// <summary>
        /// 创建带有自定义消息的空项目
        /// </summary>
        public static LaunchpadItem CreateMessageItem(string message)
        {
            return new LaunchpadItem
            {
                Type = ItemType.Empty,
                CustomDisplayName = message
            };
        }

        /// <summary>
        /// 创建透明图标
        /// </summary>
        private BitmapImage CreateTransparentIcon()
        {
            try
            {
                // 创建 1x1 透明 PNG
                byte[] transparentPng = new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
                0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
                0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
                0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
                0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
            };

                using (var stream = new MemoryStream(transparentPng))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建透明图标失败: {ex.Message}");
                return CreateDefaultIcon();
            }
        }

        /// <summary>
        /// 创建默认图标
        /// </summary>
        private BitmapImage CreateDefaultIcon()
        {
            try
            {
                // 创建简单的默认图标
                var bitmap = new System.Drawing.Bitmap(64, 64);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.LightGray);
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建默认图标失败: {ex.Message}");

                // 最后的回退
                try
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    // 使用内置的小图标数据
                    bitmapImage.StreamSource = new MemoryStream(new byte[] { 0x00 });
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
                catch
                {
                    return null;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 项目类型枚举
    /// </summary>
    public enum ItemType
    {
        App, // 应用程序
        Folder, // 文件夹
        Empty, // 空位
        MissingApp // 丢失的应用
    }

    /// <summary>
    /// 丢失的应用占位符
    /// 当应用被卸载或移动时使用
    /// </summary>
    public class MissingAppPlaceholder : INotifyPropertyChanged, IEquatable<MissingAppPlaceholder>
    {
        private string _bundlePath;
        private string _displayName;
        private string _removableSource;
        private BitmapImage _icon;

        public string BundlePath
        {
            get => _bundlePath;
            set
            {
                _bundlePath = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        public string RemovableSource
        {
            get => _removableSource;
            set
            {
                _removableSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 唯一标识符（使用 BundlePath）
        /// </summary>
        public string Id => BundlePath;

        /// <summary>
        /// 图标，如果为空则创建默认图标
        /// </summary>
        public BitmapImage Icon
        {
            get => _icon ?? CreateDefaultIcon();
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public MissingAppPlaceholder(string bundlePath, string displayName, string removableSource = null)
        {
            BundlePath = bundlePath ?? throw new ArgumentNullException(nameof(bundlePath));
            DisplayName = displayName ?? "丢失的应用";
            RemovableSource = removableSource;
        }

        /// <summary>
        /// 创建默认的丢失应用图标
        /// 显示问号和虚线边框
        /// </summary>
        public static BitmapImage CreateDefaultIcon()
        {
            try
            {
                // 创建一个表示"应用丢失"的图标
                var size = new System.Drawing.Size(256, 256);
                var bitmap = new System.Drawing.Bitmap(size.Width, size.Height);

                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // 背景
                    var backgroundRect = new System.Drawing.Rectangle(0, 0, size.Width, size.Height);
                    var backgroundPath = CreateRoundedRectanglePath(backgroundRect, (int)(size.Width * 0.18));
                    graphics.FillPath(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(235, 235, 235)), backgroundPath);

                    // 边框（虚线）
                    var strokeRect = new System.Drawing.Rectangle(
                        (int)(size.Width * 0.12),
                        (int)(size.Height * 0.12),
                        (int)(size.Width * 0.76),
                        (int)(size.Height * 0.76)
                    );
                    var dashPath = CreateRoundedRectanglePath(strokeRect, (int)(strokeRect.Width * 0.18));

                    using (var dashPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(180, 180, 180), Math.Max(1, size.Width * 0.05f)))
                    {
                        dashPen.DashPattern = new float[] { size.Width * 0.16f, size.Width * 0.10f };
                        graphics.DrawPath(dashPen, dashPath);
                    }

                    // 问号图标
                    using (var font = new System.Drawing.Font("Arial", size.Height * 0.3f, System.Drawing.FontStyle.Bold))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(150, 150, 150)))
                    {
                        var text = "?";
                        var textSize = graphics.MeasureString(text, font);
                        graphics.DrawString(text, font, brush,
                            (size.Width - textSize.Width) / 2,
                            (size.Height - textSize.Height) / 2);
                    }
                }

                // 转换为 BitmapImage
                var stream = new System.IO.MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建丢失应用图标失败: {ex.Message}");

                // 回退：创建简单的默认图标
                var fallback = new BitmapImage();
                fallback.BeginInit();
                fallback.EndInit();
                fallback.Freeze();
                return fallback;
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(System.Drawing.Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var diameter = radius * 2;
            var arc = new System.Drawing.Rectangle(bounds.Location, new System.Drawing.Size(diameter, diameter));

            // 左上角
            path.AddArc(arc, 180, 90);

            // 右上角
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // 右下角
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // 左下角
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        #region Equatable and HashCode

        public bool Equals(MissingAppPlaceholder other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return BundlePath == other.BundlePath;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MissingAppPlaceholder);
        }

        public override int GetHashCode()
        {
            return BundlePath?.GetHashCode() ?? 0;
        }

        public static bool operator ==(MissingAppPlaceholder left, MissingAppPlaceholder right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(MissingAppPlaceholder left, MissingAppPlaceholder right)
        {
            return !(left == right);
        }

        #endregion Equatable and HashCode

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}