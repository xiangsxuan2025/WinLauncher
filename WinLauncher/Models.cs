using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace WinLauncher.Models
{
    // Models/AppInfo.cs
    public class AppInfo : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ExecutablePath { get; set; }
        public BitmapImage Icon { get; set; }
        public bool IsSystemApp { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // Models/FolderInfo.cs
    public class FolderInfo : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ObservableCollection<AppInfo> Apps { get; set; }
        public BitmapImage FolderIcon { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // Models/LaunchpadItem.cs
    public class LaunchpadItem : INotifyPropertyChanged
    {
        private ItemType _type;
        private AppInfo _app;
        private FolderInfo _folder;
        private string _emptyToken;
        private MissingAppPlaceholder _missingApp;

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

        private BitmapImage CreateTransparentIcon()
        {
            // 创建透明图标
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

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

    public enum ItemType
    {
        App,
        Folder,
        Empty,
        MissingApp
    }

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

        public string Id => BundlePath;

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