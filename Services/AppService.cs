using AppManager2.Config;
using AppManager2.Models;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
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
        private LinkedHashMap<string, AppSection> _dictionary = new LinkedHashMap<string, AppSection>();

        public List<string> GetAllSections() => _sections;

        public LinkedHashMap<string, AppSection> GetAllSectionDic() => _dictionary;

        public AppSection GetAppBySection(string section) {
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
            AppDbService.Instance.DeleteBySection(section);
            RemoveAppBySection(section);
            AppListChanged?.Invoke();
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

        public void UpdateAppSection(string section, AppSection appSection) {
            GroupList ??= new List<string>();
            _dictionary ??= new LinkedHashMap<string, AppSection>();
            _sections ??= new List<string>();

            if (!string.IsNullOrEmpty(appSection.Group) && !GroupList.Contains(appSection.Group)) {
                GroupList.Add(appSection.Group);
            }

            bool isExist = _dictionary.ContainsKey(section);
            if (isExist) {
                _dictionary[section] = appSection;
            } else {
                _dictionary.Add(section, appSection);
                _sections.Add(section);
            }
        }

        /// <summary>
        /// 将应用保存到 SQLite（t_app 表），并更新内存缓存、触发变更事件
        /// </summary>
        public void SaveToDb(AppSection appSection) {
            var record = ToRecord(appSection);
            AppDbService.Instance.SaveApp(record);
            UpdateAppSection(appSection.Section, appSection);
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

            if (records.Count == 0 && File.Exists(Configs.AppPath + "\\" + Configs.ProjectsIniPath)) {
                // 首次使用：将 ini 中的历史数据迁移进 SQLite
                LoadFromIni();
                foreach (var section in _sections.ToList()) {
                    AppDbService.Instance.SaveApp(ToRecord(_dictionary[section]));
                }
                return;
            }

            foreach (var record in records) {
                var appSection = ToSection(record);
                if (!string.IsNullOrEmpty(appSection.Group) && !GroupList.Contains(appSection.Group)) {
                    GroupList.Add(appSection.Group);
                }
                _dictionary.Add(appSection.Section, appSection);
                _sections.Add(appSection.Section);
            }
        }

        private AppRecord ToRecord(AppSection app) {
            return new AppRecord {
                Section = app.Section,
                Type = app.Type,
                Title = app.Title,
                App = app.App,
                StopApp = app.StopApp,
                Jdk = app.Jdk,
                Port = app.Port,
                HeartBeat = app.HeartBeat,
                Actuator = app.Actuator,
                Ext = app.Ext,
                Param = app.Param,
                Env = app.Env,
                Profiles = app.Profiles,
                Group = app.Group,
                IsWarn = app.IsWarn,
                WarnUrl = app.WarnUrl,
                AutoRun = app.AutoRun,
                IsPrintLog = app.IsPrintLog,
                Remark = app.Remark,
                Sort = app.Sort
            };
        }

        private AppSection ToSection(AppRecord record) {
            return new AppSection {
                Section = record.Section,
                Type = record.Type,
                Title = record.Title,
                App = record.App,
                StopApp = record.StopApp,
                Jdk = record.Jdk,
                Port = record.Port,
                HeartBeat = record.HeartBeat,
                Actuator = record.Actuator,
                Ext = record.Ext,
                Param = record.Param,
                Env = record.Env,
                Profiles = record.Profiles,
                Group = record.Group,
                IsWarn = record.IsWarn,
                WarnUrl = record.WarnUrl,
                AutoRun = record.AutoRun,
                IsPrintLog = record.IsPrintLog,
                Remark = record.Remark,
                Sort = record.Sort,
                RunStat = Configs.AppRunStatUnrun,
                IsRunning = false
            };
        }

        public void LoadFromIni() {
            var iniData = IniParserUtils.GetIniData(Configs.AppPath + "\\" + Configs.ProjectsIniPath);

            _dictionary.Clear();
            _sections.Clear();
            GroupList = new List<string>();

            foreach (var section in iniData.Sections) {
                var appSection = new AppSection {
                    Section = section.SectionName,
                    Type = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppType) ? iniData[section.SectionName][Configs.IniKeyAppType] : "",
                    Title = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppTitle) ? iniData[section.SectionName][Configs.IniKeyAppTitle] : "",
                    App = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppPath) ? iniData[section.SectionName][Configs.IniKeyAppPath] : "",
                    StopApp = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppStoppath) ? iniData[section.SectionName][Configs.IniKeyAppStoppath] : "",
                    Jdk = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppJdk) ? iniData[section.SectionName][Configs.IniKeyAppJdk] : "",
                    Port = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppPort) ? iniData[section.SectionName][Configs.IniKeyAppPort] : "",
                    HeartBeat = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppHeartBeat) ? iniData[section.SectionName][Configs.IniKeyAppHeartBeat] : "",
                    Actuator = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppActuator) ? iniData[section.SectionName][Configs.IniKeyAppActuator] : "",
                    Ext = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppExt) ? iniData[section.SectionName][Configs.IniKeyAppExt] : "",
                    Param = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppParam) ? iniData[section.SectionName][Configs.IniKeyAppParam] : "",
                    Env = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppEnv) ? iniData[section.SectionName][Configs.IniKeyAppEnv] : "",
                    Profiles = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppProfiles) ? iniData[section.SectionName][Configs.IniKeyAppProfiles] : "",
                    Group = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppGroup) ? iniData[section.SectionName][Configs.IniKeyAppGroup] : "",
                    WarnUrl = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppWarnUrl) ? iniData[section.SectionName][Configs.IniKeyAppWarnUrl] : "",
                    Remark = iniData[section.SectionName].ContainsKey(Configs.IniKeyAppRemark) ? iniData[section.SectionName][Configs.IniKeyAppRemark] : "",
                    IsPrintLog = bool.Parse(iniData[section.SectionName].ContainsKey(Configs.IniKeyAppPrintLog) ? iniData[section.SectionName][Configs.IniKeyAppPrintLog] : "false"),
                    IsWarn = bool.Parse(iniData[section.SectionName].ContainsKey(Configs.IniKeyAppIsWarn) ? iniData[section.SectionName][Configs.IniKeyAppIsWarn] : "false"),
                    AutoRun = bool.Parse(iniData[section.SectionName].ContainsKey(Configs.IniKeyAppAutoRun) ? iniData[section.SectionName][Configs.IniKeyAppAutoRun] : "false"),
                    RunStat = Configs.AppRunStatUnrun,
                    IsRunning = false
                };

                if (!string.IsNullOrEmpty(appSection.Group) && !GroupList.Contains(appSection.Group)) {
                    GroupList.Add(appSection.Group);
                }

                _dictionary.Add(section.SectionName, appSection);
                _sections.Add(section.SectionName);
            }
        }

        public void SaveToIni() {
            var iniData = new IniData();

            foreach (var kvp in _dictionary) {
                string section = kvp.Key;
                var app = kvp.Value;

                iniData[section][Configs.IniKeyAppType] = app.Type ?? "";
                iniData[section][Configs.IniKeyAppTitle] = app.Title ?? "";
                iniData[section][Configs.IniKeyAppPath] = app.App ?? "";
                iniData[section][Configs.IniKeyAppStoppath] = app.StopApp ?? "";
                iniData[section][Configs.IniKeyAppJdk] = app.Jdk ?? "";
                iniData[section][Configs.IniKeyAppPort] = app.Port ?? "";
                iniData[section][Configs.IniKeyAppHeartBeat] = app.HeartBeat ?? "";
                iniData[section][Configs.IniKeyAppActuator] = app.Actuator ?? "";
                iniData[section][Configs.IniKeyAppExt] = app.Ext ?? "";
                iniData[section][Configs.IniKeyAppParam] = app.Param ?? "";
                iniData[section][Configs.IniKeyAppEnv] = app.Env ?? "";
                iniData[section][Configs.IniKeyAppProfiles] = app.Profiles ?? "";
                iniData[section][Configs.IniKeyAppGroup] = app.Group ?? "";
                iniData[section][Configs.IniKeyAppWarnUrl] = app.WarnUrl ?? "";
                iniData[section][Configs.IniKeyAppRemark] = app.Remark ?? "";
                iniData[section][Configs.IniKeyAppPrintLog] = app.IsPrintLog.ToString();
                iniData[section][Configs.IniKeyAppIsWarn] = app.IsWarn.ToString();
                iniData[section][Configs.IniKeyAppAutoRun] = app.AutoRun.ToString();
            }

            string iniPath = Configs.AppPath + "\\" + Configs.ProjectsIniPath;
            IniParserUtils.SaveIniData(iniPath, iniData);
        }
    }
}
