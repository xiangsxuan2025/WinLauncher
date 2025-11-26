using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WinLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 更安全的方式设置 DataContext

            if (App.ServiceProvider != null)
            {
                this.DataContext = App.ServiceProvider.GetService<MainViewModel>();
            }
            else
            {
                // 备用方案：直接创建 ViewModel
                this.DataContext = new MainViewModel(
                    new WindowsAppScannerService(new IconExtractorService()),
                    new JsonDataService()
                );
            }
        }

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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}