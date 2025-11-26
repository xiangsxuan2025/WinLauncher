using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using WinLauncher.Core.Interfaces;

namespace WinLauncher
{
    public partial class App : Application
    {
        private IHost _host;

        /// <summary>
        /// 检查是否以管理员权限运行
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 以管理员权限重新启动应用
        /// </summary>
        private void RestartAsAdministrator()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    Verb = "runas" // 请求管理员权限
                };

                Process.Start(startInfo);
                Current.Shutdown();
            }
            catch (Exception ex)
            {
                // 用户可能拒绝了UAC提示
                System.Diagnostics.Debug.WriteLine($"请求管理员权限失败: {ex.Message}");
                MessageBox.Show("需要管理员权限来扫描所有应用程序。应用将继续运行，但可能无法显示所有应用的图标。",
                    "权限提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 设置全局异常处理
            SetupExceptionHandling();

            // 检查管理员权限，如果不是管理员则尝试重启
            if (!IsRunningAsAdministrator())
            {
                System.Diagnostics.Debug.WriteLine("当前不是管理员权限运行，尝试请求管理员权限...");
                RestartAsAdministrator();
                return; // 等待重启，当前实例退出
            }

            System.Diagnostics.Debug.WriteLine("以管理员权限运行，开始初始化应用...");

            try
            {
                // 配置依赖注入
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();

                // 创建并显示主窗口
                var mainWindow = new MainWindow();
                mainWindow.DataContext = ServiceProvider.GetService<MainViewModel>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用启动失败: {ex.Message}");
                MessageBox.Show($"应用启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                HandleException(ex, "AppDomain.UnhandledException");
            };

            DispatcherUnhandledException += (s, e) =>
            {
                HandleException(e.Exception, "Application.DispatcherUnhandledException");
                e.Handled = true; // 防止应用崩溃
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved(); // 标记为已观察
            };
        }

        private void HandleException(Exception ex, string source = null)
        {
            var message = $"发生未处理的异常{(source != null ? $" ({source})" : "")}:\n{ex}";

            Debug.WriteLine(message);

            // 记录到文件
            LogToFile(message);

            // 用户友好的错误消息
            MessageBox.Show("应用程序遇到问题，部分功能可能无法正常使用。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LogToFile(string message)
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLauncher", "Logs");
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");

                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n\n");
            }
            catch
            {
                // 忽略日志记录错误
            }
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

            // 工具服务
            services.AddSingleton<PerformanceMonitor>();
            services.AddSingleton<UsageAnalytics>();

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
