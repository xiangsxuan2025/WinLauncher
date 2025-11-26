using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WinLauncher.Infrastructure.Helpers
{
    internal class UwpIconCreator
    {
        /// <summary>
        /// 创建 UWP 应用默认图标
        /// </summary>
        public static async Task<BitmapImage> CreateUwpDefaultIcon()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var bitmap = new System.Drawing.Bitmap(64, 64);
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    using (var stream = new MemoryStream())
                    {
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        graphics.Clear(System.Drawing.Color.FromArgb(0, 120, 215)); // UWP 主题蓝色

                        // 绘制 UWP 风格的方块
                        var rect = new System.Drawing.Rectangle(8, 8, 48, 48);
                        graphics.FillRectangle(System.Drawing.Brushes.White, rect);

                        // 绘制 UWP 字样
                        using (var font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold))
                        using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215)))
                        {
                            var text = "UWP";
                            var textSize = graphics.MeasureString(text, font);
                            graphics.DrawString(text, font, brush,
                                (64 - textSize.Width) / 2,
                                (64 - textSize.Height) / 2);
                        }

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
                    System.Diagnostics.Debug.WriteLine($"创建 UWP 默认图标失败: {ex.Message}");
                    return null;
                }
            });
        }
    }
}
