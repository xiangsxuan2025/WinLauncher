using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public string Id { get; set; } // 唯一标识符（通常为可执行文件路径）
        public string Name { get; set; } // 应用名称
        public string DisplayName { get; set; } // 显示名称
        public string ExecutablePath { get; set; } // 可执行文件路径
        public BitmapImage Icon { get; set; } // 应用图标
        public bool IsSystemApp { get; set; } // 是否为系统应用

        public event PropertyChangedEventHandler PropertyChanged;
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
                OnPropertyChanged(nameof(DisplayName)); // 类型改变时更新显示名称
                OnPropertyChanged(nameof(Icon)); // 类型改变时更新图标
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
        /// 项目唯一标识符
        /// 根据类型生成不同的标识符
        /// </summary>
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

        // 方便方法：若为 .app 返回 AppInfo，否则为 null
        public AppInfo AppInfoIfApp => Type == ItemType.App ? App : null;

        // 方便方法：若为 .folder 返回 FolderInfo，否则为 null
        public FolderInfo FolderInfoIfFolder => Type == ItemType.Folder ? Folder : null;

        // 静态创建方法，更安全
        public static LaunchpadItem CreateAppItem(AppInfo app)
        {
            return new LaunchpadItem { Type = ItemType.App, App = app };
        }

        public static LaunchpadItem CreateFolderItem(FolderInfo folder)
        {
            return new LaunchpadItem { Type = ItemType.Folder, Folder = folder };
        }

        public static LaunchpadItem CreateEmptyItem(string token = "")
        {
            return new LaunchpadItem { Type = ItemType.Empty, EmptyToken = token };
        }

        /// <summary>
        /// 创建透明图标（用于空位）
        /// </summary>
        private BitmapImage CreateTransparentIcon()
        {
            // 创建透明图标
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// 创建默认图标
        /// </summary>
        private BitmapImage CreateDefaultIcon()
        {
            // 创建默认图标
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
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