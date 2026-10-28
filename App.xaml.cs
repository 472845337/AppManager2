using AppManager2.Config;
using SqliteUtils;
using System;
using System.IO;
using System.Text;
using System.Windows;
using AppManager2.AMUtils;

namespace AppManager2 {
    public partial class App : Application {

        static App() {
            // 初始化通用 SQLite 配置（数据库文件名与密码），必须在任何数据库操作之前执行。
            // 数据库文件由 SqliteUtils 首次连接时自动创建，这里不再需要手工 CreateFile。
            SqliteConfig.Init(Configs.DbName, Configs.DbPassword);

            // 全局兜底：任何导致进程终止的未处理异常（含App.xaml解析失败等Run之前的异常）写入日志。
            // WER崩溃报告只显示 System.Windows.Markup.XamlParse，真正的内部异常（如SQLite缺DLL、
            // 目录无写权限）只能靠这里落到文件里查看
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                var ex = (Exception)e.ExceptionObject;
                LogCrash(ex);
                try { MessageBox.Show(ex.ToString(), "启动异常", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
            };
        }

        protected override void OnStartup(StartupEventArgs e) {
            // UI线程兜底：主窗口XAML解析/构造期间抛出的异常（StartupUri窗口在BAML加载中实例化，
            // 构造函数里的任何异常都会被包装成XamlParseException）在此收口，展示真实异常后退出
            DispatcherUnhandledException += (s, e) => {
                LogCrash(e.Exception);
                e.Handled = true;
                MessageBox.Show(e.Exception.ToString(), "启动异常", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            };

            // 界面字体/颜色主题加载并应用（必须在主窗口创建前，首帧即生效）
            AMUtils.ThemeUtils.Init();

            base.OnStartup(e);

            try {
                // 初始化数据库和配置
                var appService = Services.AppService.Instance;
                appService.LoadFromDb();
            } catch (Exception ex) {
                MessageBox.Show($"数据库初始化失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 异常完整链写入exe目录startup_error.log（ToString已含全部InnerException与堆栈），
        /// 目录不可写时退到%TEMP%；只写文件不抛出，避免日志失败掩盖原异常
        /// </summary>
        private static void LogCrash(Exception ex) {
            if (ex == null) return;
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}";
            try {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log"), text, Encoding.UTF8);
            } catch {
                try {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "AppManager2_startup_error.log"), text, Encoding.UTF8);
                } catch { }
            }
        }
    }
}
