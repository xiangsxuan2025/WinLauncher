using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace WinLauncher
{
    /// <summary>
    /// 主窗口类
    /// 负责 UI 显示和用户交互
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 设置数据上下文
            this.DataContext = App.ServiceProvider.GetService<MainViewModel>();
        }

        /// <summary>
        /// 窗口源初始化完成后调用
        /// 用于启用窗口阴影等效果
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 启用窗口阴影
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    NativeMethods.EnableShadow(helper.Handle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启用窗口阴影失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口鼠标左键按下事件，支持窗口拖动
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove(); // 拖动窗口
        }

        /// <summary>
        /// 最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
