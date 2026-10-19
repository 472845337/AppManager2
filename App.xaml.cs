using AppManager2.Config;
using SqliteUtils;
using System;
using System.Windows;

namespace AppManager2 {
    public partial class App : Application {

        static App() {
            // 初始化通用 SQLite 配置（数据库文件名与密码），必须在任何数据库操作之前执行。
            // 数据库文件由 SqliteUtils 首次连接时自动创建，这里不再需要手工 CreateFile。
            SqliteConfig.Init(Configs.DbName, Configs.DbPassword);
        }

        protected override void OnStartup(StartupEventArgs e) {
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
    }
}
