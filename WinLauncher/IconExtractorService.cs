using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinLauncher
{// Services/IconExtractorService.cs
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    public class IconExtractorService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out IntPtr lpiIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1;

        public async Task<BitmapImage> GetAppIconAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"文件不存在: {filePath}");
                        return CreateDefaultIcon();
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
                    return CreateDefaultIcon();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"图标提取失败 {filePath}: {ex.Message}");
                    return CreateDefaultIcon();
                }
            });
        }

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
                    DestroyIcon(hIcon);
            }
            return null;
        }

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
                    DestroyIcon(shFileInfo.hIcon);
            }
            return null;
        }

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
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"转换图标到 BitmapImage 失败: {ex.Message}");
                return null;
            }
        }

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

    // Graphics 扩展方法用于绘制圆角矩形
    public static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(this System.Drawing.Graphics graphics, System.Drawing.Pen pen, System.Drawing.Rectangle bounds, int cornerRadius)
        {
            using (var path = CreateRoundedRectanglePath(bounds, cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void FillRoundedRectangle(this System.Drawing.Graphics graphics, System.Drawing.Brush brush, System.Drawing.Rectangle bounds, int cornerRadius)
        {
            using (var path = CreateRoundedRectanglePath(bounds, cornerRadius))
            {
                graphics.FillPath(brush, path);
            }
        }

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
    }
}