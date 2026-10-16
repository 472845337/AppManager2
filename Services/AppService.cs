using AppManager2.Config;
using AppManager2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace AppManager2.Services {
    public class AppService {
        private static AppService _instance;
        public static AppService Instance => _instance ??= new AppService();

        /// <summary>
        /// 应用列表发生变化（新增/修改/删除）时触发，用于刷新界面树
        /// </summary>
        public event Action AppListChanged;

        public List<string> GroupList { get; set; }
        private List<string> _sections = new List<string>();
        private LinkedHashMap<string, AppRecord> _dictionary = new LinkedHashMap<string, AppRecord>();

        public List<string> GetAllSections() => _sections;

        public LinkedHashMap<string, AppRecord> GetAllSectionDic() => _dictionary;

        public AppRecord GetAppBySection(string section) {
            if (string.IsNullOrEmpty(section)) return null;
            return _dictionary.TryGetValue(section, out var app) ? app : null;
        }

        public void RemoveAppBySection(string section) {
            if (_dictionary.ContainsKey(section)) {
                _dictionary.Remove(section);
            }
            if (_sections.Contains(section)) {
                _sections.Remove(section);
            }
        }

        /// <summary>
        /// 从 SQLite 中删除应用，并更新内存缓存、触发变更事件
        /// </summary>
        public void DeleteFromDb(string section) {
            bool deleted = AppDbService.Instance.DeleteBySection(section);
            RemoveAppBySection(section);
            AppListChanged?.Invoke();
            if (deleted) {
                // 删除计数（超限执行VACUUM，见TableService.CountDbDelete）
                AppDbService.CountDbDelete();
            }
        }

        /// <summary>
        /// 清空 SQLite 中的全部应用，并触发变更事件
        /// </summary>
        public void ClearDb() {
            AppDbService.Instance.Clear();
            _dictionary.Clear();
            _sections.Clear();
            GroupList = new List<string>();
            AppListChanged?.Invoke();
        }

        public void UpdateApp(string section, AppRecord app) {
            GroupList ??= new List<string>();
            _dictionary ??= new LinkedHashMap<string, AppRecord>();
            _sections ??= new List<string>();

            if (!string.IsNullOrEmpty(app.Group) && !GroupList.Contains(app.Group)) {
                GroupList.Add(app.Group);
            }

            bool isExist = _dictionary.ContainsKey(section);
            if (isExist) {
                _dictionary[section] = app;
            } else {
                _dictionary.Add(section, app);
                _sections.Add(section);
            }
        }

        /// <summary>
        /// 将应用保存到 SQLite（t_app 表），并更新内存缓存、触发变更事件
        /// </summary>
        public void SaveToDb(AppRecord app) {
            AppDbService.Instance.SaveApp(app);
            UpdateApp(app.Section, app);
            AppListChanged?.Invoke();
        }

        /// <summary>
        /// 从 SQLite（t_app 表）加载全部应用；首次（表为空）时自动从 ini 迁移历史数据
        /// </summary>
        public void LoadFromDb() {
            _dictionary.Clear();
            _sections.Clear();
            GroupList = new List<string>();

            var records = AppDbService.Instance.GetAll();

            foreach (var record in records) {
                // 老数据无log_wrap列值时按“系统”处理（跟随设置里的系统配置）
                if (string.IsNullOrEmpty(record.LogWrap)) {
                    record.LogWrap = Configs.LogWrapSystem;
                }
                if (!string.IsNullOrEmpty(record.Group) && !GroupList.Contains(record.Group)) {
                    GroupList.Add(record.Group);
                }
                _dictionary.Add(record.Section, record);
                _sections.Add(record.Section);
            }
        }
    }
}
