using AppManager2.Models;
using SqliteUtils;
using System;
using System.Collections.Generic;

namespace AppManager2.Services {
    /// <summary>
    /// t_app 表的 SQLite 访问服务（AppManager.db）。
    ///
    /// 通用增删改查（自动建表/补列、Insert/Update/Delete/Clear/Vacuum、按查询对象查询）
    /// 全部由 SqliteUtils.TableService&lt;T&gt; 提供，这里只保留 t_app 专有的查询封装：
    /// 查询条件由 new AppRecord { 字段 = 值 } 表达，非空值自动拼进 where（详见 SqliteUtils.TableService）。
    /// </summary>
    public class AppDbService : TableService<AppRecord> {

        /// <summary>
        /// 显式的静态构造函数用来告诉C#编译器在其内容实例化之前不要标记其类型
        /// </summary>
        static AppDbService() { }

        private AppDbService() { }

        public static AppDbService Instance { get; } = new AppDbService();

        /// <summary>
        /// 查询全部应用（按 Sort 升序）
        /// </summary>
        public List<AppRecord> GetAll() {
            // 只设置排序，不设置任何查询字段，即 SELECT * FROM t_app ORDER BY Sort ASC
            return SelectList(new AppRecord { OrderBy = "Sort ASC" });
        }

        /// <summary>
        /// 按 section 查询单个应用（不存在返回 null）
        /// </summary>
        public AppRecord GetBySection(string section) {
            if (string.IsNullOrEmpty(section)) return null;
            return SelectFirst(new AppRecord { Section = section });
        }

        /// <summary>
        /// 新增或更新（按 section 判断）
        /// </summary>
        public bool SaveApp(AppRecord record) {
            if (record == null || string.IsNullOrEmpty(record.Section))
                throw new ArgumentException("Section 不能为空");

            var exist = GetBySection(record.Section);
            if (exist != null) {
                record.Id = exist.Id;
                return Update(record) > 0;
            }
            return Insert(record) > 0;
        }

        /// <summary>
        /// 按 section 删除
        /// </summary>
        public bool DeleteBySection(string section) {
            if (string.IsNullOrEmpty(section)) return false;
            return Delete(new AppRecord { Section = section }) > 0;
        }
    }
}
