using AppManager2.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace AppManager2.Services {
    /// <summary>
    /// t_app 表的 SQLite 访问服务（AppManager.db）
    /// </summary>
    public class AppDbService : TableService<AppRecord> {
        private static AppDbService _instance;
        public static AppDbService Instance => _instance ??= new AppDbService();

        private AppDbService() {
            EnsureTable();
        }

        public List<AppRecord> GetAll() {
            return SelectList(null, null, "Sort", "ASC");
        }

        public AppRecord GetBySection(string section) {
            if (string.IsNullOrEmpty(section)) return null;
            var whereParams = new[] { new SQLiteParameter("section", section) };
            var list = SelectList("section=@section", whereParams);
            return list.FirstOrDefault();
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
                return Update(record);
            }
            return Insert(record) > 0;
        }

        public bool DeleteBySection(string section) {
            if (string.IsNullOrEmpty(section)) return false;
            var whereParams = new[] { new SQLiteParameter("section", section) };
            return _dbHelper.Delete("t_app", "section=@section", whereParams) > 0;
        }
    }
}
