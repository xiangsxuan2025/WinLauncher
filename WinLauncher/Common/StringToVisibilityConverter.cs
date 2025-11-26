using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WinLauncher.Common.Converters
{
    /// <summary>
    /// 字符串到可见性转换器
    /// 用于在 XAML 中根据字符串是否为空来控制 UI 元素的可见性
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法：字符串为空时隐藏，否则显示
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 反向转换（未实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
