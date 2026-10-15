using AppManager2.AMUtils;
using AppManager2.Commands;
using AppManager2.Config;
using AppManager2.Models;
using AppManager2.Services;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace AppManager2.ViewModels {
    [AddINotifyPropertyChangedInterface]
    public class AppFormViewModel : BaseViewModel {
        public short OperateType { get; set; }
        public string Section { get; set; }

        // 基本信息
        public ObservableCollection<string> AppTypes { get; set; }
        public ObservableCollection<string> Groups { get; set; }
        public string AppType { get; set; } = Configs.AppTypeJava;
        public string AppTitle { get; set; }
        public string Group { get; set; }

        // 路径配置
        public string AppPath { get; set; }
        public string StopPath { get; set; }
        public string JdkPath { get; set; }

        // 运行配置
        public string Port { get; set; }
        public string HeartBeatUrl { get; set; }
        public string ActuatorUrl { get; set; }

        // 参数配置
        public string ExtScript { get; set; }
        public string JvmParams { get; set; }
        public string EnvParams { get; set; }
        public string Profiles { get; set; }

        // 其他配置
        public bool IsWarn { get; set; } = true;
        public string WarnUrl { get; set; }
        public bool AutoRun { get; set; }
        public bool IsPrintLog { get; set; } = true;
        public string Remark { get; set; }

        // UI状态
        public string WindowTitle { get; set; } = "添加应用";
        public bool IsEditMode => OperateType == Configs.OperateTypeAdd || OperateType == Configs.OperateTypeUpdate;
        public bool IsDetailMode => OperateType == Configs.OperateTypeDetail;

        // 命令
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }
        public RelayCommand BrowseAppPathCommand { get; set; }
        public RelayCommand BrowseStopPathCommand { get; set; }
        public RelayCommand BrowseJdkPathCommand { get; set; }
        public RelayCommand DefaultJvmCommand { get; set; }
        public RelayCommand DefaultEnvCommand { get; set; }

        private readonly AppService _appService;
        private Window _window;

        // 默认参数
        private readonly Dictionary<string, string> _defaultJvmParams = new Dictionary<string, string> {
            ["-Xms"] = "-Xms128m",
            ["-Xmx"] = "-Xmx1024m",
            ["-XX:MetaspaceSize"] = "-XX:MetaspaceSize=32m",
            ["-Dfile.encoding"] = "-Dfile.encoding=utf-8"
        };

        private readonly Dictionary<string, string> _defaultEnvParams = new Dictionary<string, string> {
            ["--spring.profiles.active"] = "--spring.profiles.active=prod"
        };

        public AppFormViewModel() {
            _appService = AppService.Instance;

            // 初始化集合
            AppTypes = new ObservableCollection<string> { Configs.AppTypeJava, Configs.AppTypeBat, Configs.AppTypeExe };
            Groups = new ObservableCollection<string>();

            // 初始化命令
            SaveCommand = new RelayCommand(OnSave, CanSave);
            CloseCommand = new RelayCommand(OnClose);
            BrowseAppPathCommand = new RelayCommand(OnBrowseAppPath);
            BrowseStopPathCommand = new RelayCommand(OnBrowseStopPath);
            BrowseJdkPathCommand = new RelayCommand(OnBrowseJdkPath);
            DefaultJvmCommand = new RelayCommand(OnDefaultJvm);
            DefaultEnvCommand = new RelayCommand(OnDefaultEnv);

            LoadGroups();
        }

        public void SetWindow(Window window) {
            _window = window;
        }

        public void SetOperateType(short operateType, string section = null) {
            OperateType = operateType;
            RelayCommand.Raise(SaveCommand);
            switch (operateType) {
                case Configs.OperateTypeAdd:
                    WindowTitle = "添加应用";
                    break;
                case Configs.OperateTypeUpdate:
                    WindowTitle = "修改应用";
                    break;
                case Configs.OperateTypeDetail:
                    WindowTitle = "查看应用";
                    break;
            }

            if (operateType == Configs.OperateTypeUpdate || operateType == Configs.OperateTypeDetail) {
                if (!string.IsNullOrEmpty(section)) {
                    Section = section;
                    LoadApp(section);
                }
            }
        }

        private void LoadGroups() {
            Groups.Clear();
            var groupList = _appService.GroupList;
            if (groupList != null) {
                foreach (var group in groupList) {
                    Groups.Add(group);
                }
            }
        }

        private void LoadApp(string section) {
            var app = _appService.GetAppBySection(section);
            if (app != null) {
                Section = app.Section;
                AppType = app.Type;
                AppTitle = app.Title;
                AppPath = app.App;
                StopPath = app.StopApp;
                JdkPath = app.Jdk;
                Port = app.Port;
                HeartBeatUrl = app.HeartBeat;
                ActuatorUrl = app.Actuator;
                ExtScript = app.Ext;
                JvmParams = app.Param;
                EnvParams = app.Env;
                Profiles = app.Profiles;
                Group = app.Group;
                IsWarn = app.IsWarn;
                WarnUrl = app.WarnUrl;
                AutoRun = app.AutoRun;
                Remark = app.Remark;
                IsPrintLog = app.IsPrintLog;
            }
        }

        private void OnSave(object obj) {
            var errorMsg = ValidateInput();
            if (!string.IsNullOrEmpty(errorMsg)) {
                MessageBox.Show(errorMsg, "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try {
                var section = OperateType == Configs.OperateTypeAdd
                    ? Guid.NewGuid().ToString()
                    : Section;

                var appSection = new AppSection {
                    Section = section,
                    Type = AppType,
                    Title = AppTitle?.Trim(),
                    App = AppPath?.Trim(),
                    StopApp = StopPath?.Trim(),
                    Jdk = JdkPath?.Trim(),
                    Port = Port?.Trim(),
                    HeartBeat = HeartBeatUrl?.Trim(),
                    Actuator = ActuatorUrl?.Trim(),
                    Ext = ExtScript?.Trim(),
                    Param = JvmParams?.Trim(),
                    Env = EnvParams?.Trim(),
                    Profiles = Profiles?.Trim(),
                    Group = Group?.Trim(),
                    IsWarn = IsWarn,
                    WarnUrl = WarnUrl?.Trim(),
                    AutoRun = AutoRun,
                    Remark = Remark?.Trim(),
                    IsPrintLog = IsPrintLog,
                    RunStat = Configs.AppRunStatUnrun,
                    IsRunning = false
                };

                SaveAppSection(appSection);
                MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                try { _window.DialogResult = true; } catch { /* 非模态打开时忽略 */ }
                _window?.Close();
            } catch (Exception ex) {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSave(object obj) {
            return IsEditMode;
        }

        private string ValidateInput() {
            var errors = new List<string>();

            // 名称必填
            if (string.IsNullOrWhiteSpace(AppTitle)) {
                errors.Add("名称不能为空");
            }

            // 应用路径必填
            if (string.IsNullOrWhiteSpace(AppPath)) {
                errors.Add("应用路径不能为空");
            }

            // 根据类型验证
            switch (AppType) {
                case Configs.AppTypeJava:
                    // Java类型需要jar文件
                    if (!string.IsNullOrEmpty(AppPath) && !AppPath.ToLower().EndsWith(".jar")) {
                        errors.Add("Java类型必须选择.jar文件");
                    }
                    // 端口必填
                    if (string.IsNullOrWhiteSpace(Port)) {
                        errors.Add("端口不能为空");
                    } else if (!Regex.IsMatch(Port, @"^\d+$")) {
                        errors.Add("端口必须是数字");
                    } else if (int.Parse(Port) > 65535) {
                        errors.Add("端口不能大于65535");
                    }
                    break;

                case Configs.AppTypeBat:
                    // Bat类型需要.bat或.cmd文件
                    if (!string.IsNullOrEmpty(AppPath) &&
                        !AppPath.ToLower().EndsWith(".bat") &&
                        !AppPath.ToLower().EndsWith(".cmd")) {
                        errors.Add("Bat类型必须选择.bat或.cmd文件");
                    }
                    // 终止脚本必填
                    if (string.IsNullOrWhiteSpace(StopPath)) {
                        errors.Add("终止脚本路径不能为空");
                    }
                    // 端口或心跳必填其一
                    if (string.IsNullOrWhiteSpace(Port) && string.IsNullOrWhiteSpace(HeartBeatUrl)) {
                        errors.Add("端口和心跳必须填写其中一个");
                    }
                    break;

                case Configs.AppTypeExe:
                    // Exe类型需要.exe文件
                    if (!string.IsNullOrEmpty(AppPath) && !AppPath.ToLower().EndsWith(".exe")) {
                        errors.Add("Exe类型必须选择.exe文件");
                    }
                    break;
            }

            return string.Join("\n", errors);
        }

        private void SaveAppSection(AppSection appSection) {
            // 保存到 SQLite（AppManager.db 的 t_app 表），并更新缓存、通知界面刷新
            _appService.SaveToDb(appSection);

            // java项目根据jar包地址生成对应的启动/停止bat脚本
            if (Configs.AppTypeJava.Equals(appSection.Type)) {
                JavaAppUtils.LoadSettings();

                var result = JavaAppUtils.CreateStartBat(appSection, Configs.LogFileInfo, Configs.LogFileError);
                if (!bool.TrueString.Equals(result)) {
                    // 应用信息已保存，脚本生成失败单独提示
                    MessageBox.Show(result, "脚本生成失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                JavaAppUtils.CreateStopBat(appSection);
            }
        }

        private void OnClose(object obj) {
            _window?.Close();
        }

        private void OnBrowseAppPath(object obj) {
            var filter = AppType switch {
                Configs.AppTypeJava => "Jar Files (*.jar)|*.jar|All Files (*.*)|*.*",
                Configs.AppTypeBat => "Bat Files (*.bat)|*.bat|Cmd Files (*.cmd)|*.cmd|All Files (*.*)|*.*",
                Configs.AppTypeExe => "Exe Files (*.exe)|*.exe|All Files (*.*)|*.*",
                _ => "All Files (*.*)|*.*"
            };

            var dialog = new Microsoft.Win32.OpenFileDialog {
                Filter = filter,
                Title = "选择应用文件"
            };

            if (!string.IsNullOrEmpty(AppPath)) {
                dialog.InitialDirectory = Path.GetDirectoryName(AppPath);
            }

            if (dialog.ShowDialog() == true) {
                AppPath = dialog.FileName;
                // 如果名称为空，自动填充名称
                if (string.IsNullOrEmpty(AppTitle)) {
                    var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    AppTitle = fileName;
                }
            }
        }

        private void OnBrowseStopPath(object obj) {
            var filter = "Bat Files (*.bat)|*.bat|Cmd Files (*.cmd)|*.cmd|All Files (*.*)|*.*";

            var dialog = new Microsoft.Win32.OpenFileDialog {
                Filter = filter,
                Title = "选择停止脚本"
            };

            if (!string.IsNullOrEmpty(StopPath)) {
                dialog.InitialDirectory = Path.GetDirectoryName(StopPath);
            }

            if (dialog.ShowDialog() == true) {
                StopPath = dialog.FileName;
            }
        }

        private void OnBrowseJdkPath(object obj) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog {
                Description = "选择JDK目录",
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrEmpty(JdkPath) && Directory.Exists(JdkPath)) {
                dialog.SelectedPath = JdkPath;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                JdkPath = dialog.SelectedPath;
            }
        }

        private void OnDefaultJvm(object obj) {
            var current = JvmParams ?? string.Empty;
            foreach (var param in _defaultJvmParams) {
                if (!current.Contains(param.Key)) {
                    current = string.IsNullOrEmpty(current)
                        ? param.Value
                        : $"{current} {param.Value}";
                }
            }
            JvmParams = current;
        }

        private void OnDefaultEnv(object obj) {
            var current = EnvParams ?? string.Empty;
            foreach (var param in _defaultEnvParams) {
                if (!current.Contains(param.Key)) {
                    current = string.IsNullOrEmpty(current)
                        ? param.Value
                        : $"{current} {param.Value}";
                }
            }
            EnvParams = current;
        }

        public AppSection ToAppSection() {
            return new AppSection {
                Section = Section,
                Type = AppType,
                Title = AppTitle,
                App = AppPath,
                StopApp = StopPath,
                Jdk = JdkPath,
                Port = Port,
                HeartBeat = HeartBeatUrl,
                Actuator = ActuatorUrl,
                Ext = ExtScript,
                Param = JvmParams,
                Env = EnvParams,
                Profiles = Profiles,
                Group = Group,
                IsWarn = IsWarn,
                WarnUrl = WarnUrl,
                AutoRun = AutoRun,
                Remark = Remark,
                IsPrintLog = IsPrintLog,
                RunStat = Configs.AppRunStatUnrun,
                IsRunning = false
            };
        }

        public void FromAppSection(AppSection app) {
            Section = app.Section;
            AppType = app.Type;
            AppTitle = app.Title;
            AppPath = app.App;
            StopPath = app.StopApp;
            JdkPath = app.Jdk;
            Port = app.Port;
            HeartBeatUrl = app.HeartBeat;
            ActuatorUrl = app.Actuator;
            ExtScript = app.Ext;
            JvmParams = app.Param;
            EnvParams = app.Env;
            Profiles = app.Profiles;
            Group = app.Group;
            IsWarn = app.IsWarn;
            WarnUrl = app.WarnUrl;
            AutoRun = app.AutoRun;
            Remark = app.Remark;
            IsPrintLog = app.IsPrintLog;
        }
    }
}
