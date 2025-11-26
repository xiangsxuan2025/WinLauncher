using System.Configuration;
using System.Data;
using System.Windows;

namespace WinLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    // App.xaml.cs
    // App.xaml.cs
    using System.Windows;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public partial class App : Application
    {
        private IHost _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 简单的手动依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetService<MainViewModel>();
            mainWindow.Show();
        }

        public static IServiceProvider ServiceProvider { get; private set; }

        private void ConfigureServices(IServiceCollection services)
        {
            // 服务
            services.AddSingleton<IconExtractorService>();
            services.AddSingleton<IAppScannerService, WindowsAppScannerService>();
            services.AddSingleton<IDataService, JsonDataService>();

            // ViewModels 和 View
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }

}