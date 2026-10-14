using AppManager2.Config;
using System;
using System.Windows;

namespace AppManager2
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 校验库文件：不存在时先创建 AppManager.db，避免后续连接异常
                var dbFile = System.IO.Path.Combine(Configs.AppPath, "AppManager.db");
                if (!System.IO.File.Exists(dbFile))
                {
                    System.Data.SQLite.SQLiteConnection.CreateFile(dbFile);
                }

                // 初始化数据库和配置（应用信息从 SQLite 加载，首次自动迁移 ini 历史数据）
                var appService = Services.AppService.Instance;
                appService.LoadFromDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库初始化失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
