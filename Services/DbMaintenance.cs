using AppManager2.Config;
using SqliteUtils;

namespace AppManager2.Services {
    /// <summary>
    /// 数据库维护（原 TableService.CountDbDelete 的职责，迁出到独立类，
    /// 因为它与具体某张表无关，是整库级别的操作）。
    ///
    /// 删除数据后计数，超过阈值时执行 VACUUM 收缩数据库文件并清零重计；
    /// 只计数不写 ini——主窗口关闭时与启动时加载的 PriDelCount 比对，值变化才持久化。
    /// </summary>
    public static class DbMaintenance {

        /// <summary>
        /// 复用 SqliteUtils 按库路径缓存的数据库连接
        /// </summary>
        private static SqLiteHelper Helper => SqLiteFactory.GetSqLiteHelper(SqliteConfig.DbFilePath, SqliteConfig.DbPassword);

        /// <summary>
        /// 删除数据后调用：累计删除次数，超过阈值时执行 VACUUM 并清零重计
        /// </summary>
        public static void CountDbDelete() {
            if (Configs.DelCount > Configs.DelCountLimit) {
                Configs.DelCount = 0;
                Helper.Vacuum();
            } else {
                Configs.DelCount += 1;
            }
        }
    }
}
