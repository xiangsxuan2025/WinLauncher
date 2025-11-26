using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace WinLauncher
{
    public partial class App : Application
    {
        private IHost _host;

        /// <summary>
        /// 应用程序启动时调用，配置依赖注入并创建主窗口
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 配置依赖注入容器
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetService<MainViewModel>();
            mainWindow.Show();
        }

        /// <summary>
        /// 全局服务提供者，用于在应用内获取依赖注入的服务
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // 注册服务
            services.AddSingleton<IconExtractorService>(); // 图标提取服务
            services.AddSingleton<IAppScannerService, WindowsAppScannerService>(); // 应用扫描服务
            services.AddSingleton<IDataService, JsonDataService>(); // 数据存储服务

            // 注册 ViewModels 和 Views
            services.AddTransient<MainViewModel>(); // 主视图模型
            services.AddTransient<MainWindow>(); // 主窗口
        }

        /// <summary>
        /// 应用程序退出时调用，清理资源
        /// </summary>
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