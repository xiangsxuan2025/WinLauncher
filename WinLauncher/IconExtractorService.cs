using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace WinLauncher
{
    /// <summary>
    /// 图标提取服务
    /// 负责从可执行文件中提取应用程序图标
    /// 使用多种方法确保兼容性和可靠性
    /// </summary>
    public class IconExtractorService
    {
        // Windows API 函数声明

        /// <summary>
        /// 从可执行文件中提取关联的图标
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out IntPtr lpiIcon);

        /// <summary>
        /// 销毁图标句柄，释放资源
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// 获取文件信息，包括图标
        /// </summary>
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        /// <summary>
        /// 文件信息结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon; // 图标句柄
            public int iIcon; // 图标索引
            public uint dwAttributes; // 文件属性
            public string szDisplayName; // 显示名称
            public string szTypeName; // 类型名称
        }

        // Windows API 常量
        private const uint SHGFI_ICON = 0x100; // 获取图标
        private const uint SHGFI_LARGEICON = 0x0; // 大图标
        private const uint SHGFI_SMALLICON = 0x1; // 小图标

        /// <summary>
        /// 异步获取应用图标
        /// 使用多种方法确保成功提取图标
        /// </summary>
        public async Task<BitmapImage> GetAppIconAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"文件不存在: {filePath}");
                        return CreateDefaultIcon(); // 返回默认图标
                    }

                    // 方法1: 使用 ExtractAssociatedIcon (最可靠)
                    var iconFromExtract = ExtractIconUsingAPI(filePath);
                    if (iconFromExtract != null)
                        return iconFromExtract;

                    // 方法2: 使用 SHGetFileInfo (备用方法)
                    var iconFromShell = ExtractIconUsingShell(filePath);
                    if (iconFromShell != null)
                        return iconFromShell;

                    // 方法3: 使用 System.Drawing.Icon (最后备选)
                    var iconFromDrawing = ExtractIconUsingDrawing(filePath);
                    if (iconFromDrawing != null)
                        return iconFromDrawing;

                    System.Diagnostics.Debug.WriteLine($"所有图标提取方法都失败了: {filePath}");
                    return CreateDefaultIcon(); // 所有方法都失败时返回默认图标
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"图标提取失败 {filePath}: {ex.Message}");
                    return CreateDefaultIcon();
                }
            });
        }

        /// <summary>
        /// 使用 ExtractAssociatedIcon API 提取图标
        /// </summary>
        private BitmapImage ExtractIconUsingAPI(string filePath)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                // 使用 ExtractAssociatedIcon API
                hIcon = ExtractAssociatedIcon(IntPtr.Zero, filePath, out _);
                if (hIcon != IntPtr.Zero)
                {
                    using (var icon = Icon.FromHandle(hIcon))
                    {
                        return ConvertIconToBitmapImage(icon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExtractAssociatedIcon 失败: {ex.Message}");
            }
            finally
            {
                if (hIcon != IntPtr.Zero)
                    DestroyIcon(hIcon); // 清理资源
            }
            return null;
        }

        /// <summary>
        /// 使用 SHGetFileInfo API 提取图标
        /// </summary>
        private BitmapImage ExtractIconUsingShell(string filePath)
        {
            SHFILEINFO shFileInfo = new SHFILEINFO();
            try
            {
                IntPtr hIcon = SHGetFileInfo(filePath, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), SHGFI_ICON | SHGFI_LARGEICON);

                if (shFileInfo.hIcon != IntPtr.Zero)
                {
                    using (var icon = Icon.FromHandle(shFileInfo.hIcon))
                    {
                        return ConvertIconToBitmapImage(icon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SHGetFileInfo 失败: {ex.Message}");
            }
            finally
            {
                if (shFileInfo.hIcon != IntPtr.Zero)
                    DestroyIcon(shFileInfo.hIcon); // 清理资源
            }
            return null;
        }

        /// <summary>
        /// 使用 System.Drawing.Icon 提取图标
        /// </summary>
        private BitmapImage ExtractIconUsingDrawing(string filePath)
        {
            try
            {
                // 使用 System.Drawing.Icon.ExtractAssociatedIcon
                var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon != null)
                {
                    using (icon)
                    {
                        return ConvertIconToBitmapImage(icon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Icon.ExtractAssociatedIcon 失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 将 System.Drawing.Icon 转换为 WPF 的 BitmapImage
        /// </summary>
        private BitmapImage ConvertIconToBitmapImage(Icon icon)
        {
            try
            {
                using (var bitmap = icon.ToBitmap())
                {
                    var stream = new MemoryStream();

                    // 使用 PNG 格式以获得更好的质量
                    bitmap.Save(stream, ImageFormat.Png);
                    stream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 立即加载
                    bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 忽略缓存
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // 冻结对象，使其可以在其他线程使用

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"转换图标到 BitmapImage 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建默认应用图标
        /// 当无法提取图标时使用
        /// </summary>
        private BitmapImage CreateDefaultIcon()
        {
            try
            {
                // 创建一个美观的默认应用图标
                var bitmap = new System.Drawing.Bitmap(64, 64);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.Clear(System.Drawing.Color.FromArgb(240, 240, 240));

                    // 绘制圆角矩形背景
                    var backgroundBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 200, 200));
                    var backgroundRect = new System.Drawing.Rectangle(4, 4, 56, 56);
                    graphics.FillRoundedRectangle(backgroundBrush, backgroundRect, 12);

                    // 绘制应用图标轮廓
                    var outlinePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(120, 120, 120), 2);
                    graphics.DrawRoundedRectangle(outlinePen, backgroundRect, 12);

                    // 绘制"A"字母
                    using (var font = new System.Drawing.Font("Arial", 20, System.Drawing.FontStyle.Bold))
                    using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80)))
                    {
                        var textSize = graphics.MeasureString("A", font);
                        graphics.DrawString("A", font, textBrush,
                            (64 - textSize.Width) / 2,
                            (64 - textSize.Height) / 2);
                    }
                }

                var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建默认图标失败: {ex.Message}");

                // 最后的回退：创建空的位图
                var emptyBitmap = new BitmapImage();
                emptyBitmap.BeginInit();
                emptyBitmap.EndInit();
                emptyBitmap.Freeze();
                return emptyBitmap;
            }
        }
    }

    /// <summary>
    /// Graphics 扩展方法
    /// 用于绘制圆角矩形等高级图形操作
    /// </summary>
    public static class GraphicsExtensions
    {
        /// <summary>
        /// 绘制圆角矩形边框
        /// </summary>
        public static void DrawRoundedRectangle(this System.Drawing.Graphics graphics, System.Drawing.Pen pen, System.Drawing.Rectangle bounds, int cornerRadius)
        {
            using (var path = CreateRoundedRectanglePath(bounds, cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// 填充圆角矩形
        /// </summary>
        public static void FillRoundedRectangle(this System.Drawing.Graphics graphics, System.Drawing.Brush brush, System.Drawing.Rectangle bounds, int cornerRadius)
        {
            using (var path = CreateRoundedRectanglePath(bounds, cornerRadius))
            {
                graphics.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(System.Drawing.Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var size = new System.Drawing.Size(diameter, diameter);
            var arc = new System.Drawing.Rectangle(bounds.Location, size);
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // 左上角圆弧
            path.AddArc(arc, 180, 90);

            // 右上角圆弧
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // 右下角圆弧
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // 左下角圆弧
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}