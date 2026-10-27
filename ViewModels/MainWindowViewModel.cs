using AMUtils;
using AppManager2.AMUtils;
using AppManager2.Commands;
using AppManager2.Config;
using AppManager2.Excep;
using AppManager2.Models;
using AppManager2.Services;
using IniParser.Model;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Utils;

namespace AppManager2.ViewModels {
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel : BaseViewModel {
        public ObservableCollection<TreeItemModel> AppTree { get; set; }
        public ObservableCollection<AppRunTabItemModel> RunningApps { get; set; }

        [OnChangedMethod(nameof(OnSelectedAppChanged))]
        public TreeItemModel SelectedApp { get; set; }
        public AppRunTabItemModel SelectedTab { get; set; }

        public int SelectedTabIndex { get; set; }

        // 状态栏瞬时提示（对应老项目OperateMessageStatusLabel + TimerUtils.ShowOperateLabelMessage）
        public string OperateMessage { get; set; } = string.Empty;
        public Brush OperateMessageBrush { get; set; } = Brushes.Black;

        // 状态栏右侧固定信息（对应老项目IpToolStripStatusLabel/AuthorToolStripStatusLabel）
        public string IpInfo { get; set; }
        public string AuthorInfo { get; set; }

        // 分割比例（0-1之间）
        [OnChangedMethod(nameof(OnTreeSplitRatioChanged))]
        public double TreeSplitRatio { get; set; } = 0.25; // 默认25%
        public double RunSplitRatio { get; set; } = 0.75;
        // 环境设置（对应老项目EnvGroupBox：Jdk/配置dev|test|prod/日志）
        public string JdkPath { get; set; }
        public string LogPath { get; set; }
        public string SelectedProfile { get; set; }
        public ObservableCollection<string> ProfilesOptions { get; set; }

        // 监听设置（对应老项目MonitorSetGroupBox：超时/s、频次/s、日志换行）
        public string MonitorTimeoutText { get; set; } = "15";
        public string MonitorIntervalText { get; set; } = "5";
        // 系统级日志换行（项目换行为“系统”时按此生效）
        public ObservableCollection<LogWrapOption> LogWrapOptions { get; set; }
        public string SelectedLogWrap { get; set; } = Configs.LogWrapNone;

        // 运行Tab表头行数（1单行/2双行/0不限，超出部分收进右侧下拉，参考IntelliJ）：
        // SelectedRunTabRows为设置页下拉选中Code，RunTabRows为控件绑定值；
        // IsRunTabMultiRow为选项卡选项菜单的"多行显示"勾选态（=自适应行数）
        public ObservableCollection<LogWrapOption> RunTabRowsOptions { get; set; }
        [OnChangedMethod(nameof(OnSelectedRunTabRowsChanged))]
        public string SelectedRunTabRows { get; set; } = "1";
        public int RunTabRows { get; set; } = 1;
        public bool IsRunTabMultiRow { get; set; }
        // 多行显示取消后恢复的单/双行配置
        private int _runTabRowsRestore = 1;
        public ICommand ToggleRunTabMultiRowCommand { get; set; }

        private void OnSelectedRunTabRowsChanged() {
            // 非法值回落单行
            RunTabRows = int.TryParse(SelectedRunTabRows, out var rows) && rows >= 0 ? rows : 1;
            IsRunTabMultiRow = RunTabRows == 0;
        }

        /// <summary>
        /// 选项卡选项菜单-多行显示选项卡开关（参考VS2022）：勾选=自适应多行（0），
        /// 取消=恢复上次的单/双行配置；即时更新界面，落盘在主窗口关闭时统一处理
        /// </summary>
        private void OnToggleRunTabMultiRow(object obj) {
            if (RunTabRows == 0) {
                SelectedRunTabRows = _runTabRowsRestore.ToString();
            } else {
                _runTabRowsRestore = RunTabRows;
                SelectedRunTabRows = "0";
            }
        }

        // 应用设置（对应老项目AppSetGroup：心跳字符/通知+预警地址/机器名）
        public string HeartBeatMsg { get; set; }
        public string ServerName { get; set; }
        public string WarnUrl { get; set; }
        public bool IsWarnEnabled { get; set; }

        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public int SettingTabControlSelectedIndex { get; set; }
        
        public void RaiseResetSettingCommand() {
            RelayCommand.Raise(new RelayCommand[] { ResetSoftwareSettingsCommand });
        }
        // 关闭时设置（对应老项目ClosedTypeGroup：退出/最小化）
        private bool _isClosedExit = true;
        private bool _isClosedMin;
        public bool IsClosedExit {
            get => _isClosedExit;
            set {
                _isClosedExit = value;
                if (value) _isClosedMin = false;
            }
        }
        public bool IsClosedMin {
            get => _isClosedMin;
            set {
                _isClosedMin = value;
                if (value) _isClosedExit = false;
            }
        }

        // 自启动设置（对应老项目AutoRunGroupBox：全选+项目自启动联动）
        public ObservableCollection<AutoRunItemModel> AutoRunItems { get; set; }

        // 重建设置页列表/单项勾选刷新全选状态时，不触发批量应用
        private bool _buildingAutoRun;

        [OnChangedMethod(nameof(OnAutoRunAllChanged))]
        public bool AutoRunAll { get; set; }

        private void OnAutoRunAllChanged() {
            if (_buildingAutoRun) return;
            // 目标值必须先存局部变量：每勾一项，HandleAutoRunItemChanged会重算并回写
            // AutoRunAll（“尚未全选”时改回false），若循环内直接读属性，第一项之后就全是false了
            var checkAll = AutoRunAll;
            foreach (var item in AutoRunItems) {
                item.IsChecked = checkAll; // 勾选回调负责同步项目AutoRun并入库
            }
        }

        // 设置页命令
        public ICommand ApplySettingsCommand { get; set; }
        public RelayCommand ResetSoftwareSettingsCommand { get; set; }
        public ICommand BrowseJdkCommand { get; set; }
        public ICommand BrowseLogCommand { get; set; }

        public ICommand AddAppCommand { get; set; }
        // 启停相关命令声明为具体类型，状态变化时统一Raise刷新可用性
        public RelayCommand EditAppCommand { get; set; }
        public RelayCommand DeleteAppCommand { get; set; }
        public RelayCommand StartAppCommand { get; set; }
        public RelayCommand StopAppCommand { get; set; }
        public ICommand ResetRunStatCommand { get; set; }
        public ICommand AllStartCommand { get; set; }
        public ICommand AllStopCommand { get; set; }
        public ICommand ClearAllCommand { get; set; }
        public ICommand ExportConfigCommand { get; set; }
        public ICommand ImportConfigCommand { get; set; }
        public ICommand CloseTabCommand { get; set; }
        public ICommand CloseStoppedTabsCommand { get; set; }
        public ICommand SendInputCommand { get; set; }
        public ICommand ExpandAllCommand { get; set; }
        public ICommand CollapseAllCommand { get; set; }

        // 应用节点右键菜单命令
        public ICommand DetailAppCommand { get; set; }
        public ICommand WarnSwitchAppCommand { get; set; }
        public ICommand LocateFileCommand { get; set; }
        // 组节点右键菜单命令
        public ICommand GroupStartCommand { get; set; }
        public ICommand GroupStopCommand { get; set; }
        public ICommand GroupDeleteCommand { get; set; }
        public ICommand GroupWarnOnCommand { get; set; }
        public ICommand GroupWarnOffCommand { get; set; }

        // 事件：通知View展开/收缩TreeView
        public event Action ExpandAllRequested;
        public event Action CollapseAllRequested;

        private readonly AppService _appService;
        private readonly AppRunService _runService;
        private readonly AppMonitorService _monitorService;
        private readonly DispatcherTimer _messageTimer = new DispatcherTimer();

        // 节点图标（对应老项目TreeViewImageList）
        private static readonly ImageSource GroupIcon = LoadIcon("Resources/icon/AppForm/group.ico");
        private static readonly ImageSource JavaIcon = LoadIcon("Resources/icon/AppForm/java.ico");
        private static readonly ImageSource BatIcon = LoadIcon("Resources/icon/AppForm/cmd.ico");
        private static readonly ImageSource ExeDefaultIcon = LoadIcon("Resources/icon/AppForm/exe.ico");

        // 运行状态背景色（对应老项目FreshAppColor）
        private static readonly Brush StatRunningBrush = CreateBrush(Color.FromRgb(154, 205, 50));    // YellowGreen 启动中
        private static readonly Brush StatStoppingBrush = CreateBrush(Color.FromRgb(255, 215, 0));    // Gold 停止中（与启动中区分）
        private static readonly Brush StatSuccessBrush = CreateBrush(Color.FromRgb(144, 238, 144));  // LightGreen 运行中
        private static readonly Brush StatFailBrush = CreateBrush(Color.FromRgb(255, 69, 0));         // OrangeRed 失败
        private static readonly Brush StatUnrunBrush = Brushes.White;                                 // 未运行
        // 组节点聚合色
        private static readonly Brush GroupPinkBrush = CreateBrush(Color.FromRgb(255, 192, 203));     // Pink
        private static readonly Brush GroupYellowBrush = CreateBrush(Color.FromRgb(255, 255, 0));     // Yellow
        private static readonly Brush GroupYellowGreenBrush = CreateBrush(Color.FromRgb(154, 205, 50));

        public MainWindowViewModel() {
            _appService = AppService.Instance;
            _runService = AppRunService.Instance;
            _monitorService = AppMonitorService.Instance;

            AppTree = new ObservableCollection<TreeItemModel>();
            RunningApps = new ObservableCollection<AppRunTabItemModel>();
            AutoRunItems = new ObservableCollection<AutoRunItemModel>();
            ProfilesOptions = new ObservableCollection<string> { "dev", "test", "prod" };
            LogWrapOptions = new ObservableCollection<LogWrapOption>(LogWrapModes.SetOptions);
            RunTabRowsOptions = new ObservableCollection<LogWrapOption> {
                new LogWrapOption { Code = "1", Text = "单行" },
                new LogWrapOption { Code = "2", Text = "双行" },
                new LogWrapOption { Code = "0", Text = "自适应" },
            };

            InitializeCommands();
            WireEvents();
            InitMessageTimer();
            LoadData();
            IpInfo = "IP:" + IpUtils.GetLocalIp();
            AuthorInfo = "作者:" + Configs.AuthorEmail;
        }

        private static ImageSource LoadIcon(string relativePath) {
            var image = new BitmapImage(new Uri($"pack://application:,,,/AppManager2;component/{relativePath}"));
            image.Freeze();
            return image;
        }

        private static Brush CreateBrush(Color color) {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private void InitializeCommands() {
            AddAppCommand = new RelayCommand(OnAddApp);
            EditAppCommand = new RelayCommand(OnEditApp, CanEditOrDeleteApp);
            DeleteAppCommand = new RelayCommand(OnDeleteApp, CanEditOrDeleteApp);
            StartAppCommand = new RelayCommand(OnStartApp, CanStartApp);
            StopAppCommand = new RelayCommand(OnStopApp, CanStopApp);
            ResetRunStatCommand = new RelayCommand(OnResetRunStat);
            AllStartCommand = new RelayCommand(OnAllStart);
            AllStopCommand = new RelayCommand(OnAllStop);
            ClearAllCommand = new RelayCommand(OnClearAll);
            ExportConfigCommand = new RelayCommand(OnExportConfig);
            ImportConfigCommand = new RelayCommand(OnImportConfig);
            CloseTabCommand = new RelayCommand(OnCloseTab);
            CloseStoppedTabsCommand = new RelayCommand(OnCloseStoppedTabs);
            SendInputCommand = new RelayCommand(OnSendInput);
            ExpandAllCommand = new RelayCommand(OnExpandAll);
            CollapseAllCommand = new RelayCommand(OnCollapseAll);
            ToggleRunTabMultiRowCommand = new RelayCommand(OnToggleRunTabMultiRow);
            DetailAppCommand = new RelayCommand(OnDetailApp);
            WarnSwitchAppCommand = new RelayCommand(OnWarnSwitchApp);
            LocateFileCommand = new RelayCommand(OnLocateFile);
            GroupStartCommand = new RelayCommand(OnGroupStart);
            GroupStopCommand = new RelayCommand(OnGroupStop);
            GroupDeleteCommand = new RelayCommand(OnGroupDelete);
            GroupWarnOnCommand = new RelayCommand(OnGroupWarnOn);
            GroupWarnOffCommand = new RelayCommand(OnGroupWarnOff);
            ApplySettingsCommand = new RelayCommand(OnApplySettings);
            ResetSoftwareSettingsCommand = new RelayCommand(OnResetSoftwareSettings, o => {
                bool canExecute = true;
                if((GlobalFontFamily.Equals(Configs.DefaultFontFamily) && GlobalFontSizeText.Equals("12") && GlobalFontColor.Equals("Black")
                    && BtnFontFamily.Equals(Configs.DefaultFontFamily) && BtnFontSizeText.Equals("14")
                    && TreeFontFamily.Equals(Configs.DefaultFontFamily) && TreeFontSizeText.Equals("16") && TreeFontColor.Equals("Black")
                    && LogFontColor.Equals("White") && LogBgColor.Equals("Black") && LogFontSizeText.Equals("14") && LogFontFamily.Equals("Consolas"))
                    || SettingTabControlSelectedIndex == 0) {
                    canExecute = false;
                }
                return canExecute; 
            });
            BrowseJdkCommand = new RelayCommand(OnBrowseJdk);
            BrowseLogCommand = new RelayCommand(OnBrowseLog);
        }

        /// <summary>
        /// 订阅运行/监控服务事件（事件可能在非UI线程触发，统一切换到UI线程处理）
        /// </summary>
        private void WireEvents() {
            _appService.AppListChanged += () => RunOnUi(SyncTree);
            _runService.LogReceived += (section, line) => RunOnUi(() => AppendTabLog(section, line));
            _monitorService.LogReceived += (section, line) => RunOnUi(() => AppendTabLog(section, line));
            _monitorService.RunStatChanged += (section, runStat, isRunning) => RunOnUi(() => UpdateRunStat(section, runStat, isRunning));
            _monitorService.StartRequested += app => RunOnUi(() => AutoStartApp(app));
        }

        private void InitMessageTimer() {
            _messageTimer.Tick += (s, e) => {
                _messageTimer.Stop();
                OperateMessage = string.Empty;
            };
        }

        private void RunOnUi(Action action) {
            var app = Application.Current;
            if (app?.Dispatcher == null) return;
            if (app.Dispatcher.CheckAccess()) {
                action();
            } else {
                app.Dispatcher.BeginInvoke(action);
            }
        }

        private void LoadData() {
            _appService.LoadFromDb();
            _monitorService.LoadSettings(); // 监控/预警配置加载到Configs（对应老项目InitEnv）
            // 设置页各输入项/分栏比例/Tab行数等ini加载由主窗口构造函数统一处理（LoadEnvSettings等）
            BuildTree();
            InitAppMonitor(); // bat脚本校验 + 自启动 + 启动监控定时器（对应老项目InitApp后半段）
            ShowMessage("项目加载完成", Brushes.Green, seconds: 10);
        }

        /// <summary>
        /// 初始化监控：校验每个java应用的启动/停止bat脚本（对应老项目CheckSection），
        /// 对配置了自启动的应用执行启动（MonitorApp的appInit逻辑），最后启动定时监控
        /// </summary>
        private void InitAppMonitor() {
            try {
                foreach (var section in _appService.GetAllSections().ToList()) {
                    var app = _appService.GetAppBySection(section);
                    _monitorService.MonitorApp(app, true);
                }
            } catch (BusinessException ex) {
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _monitorService.Start();
        }


        // LoadSplitRatio/LoadRunTabRows/LoadEnvSettings的ini加载已移至MainWindow构造函数统一处理

        private void OnTreeSplitRatioChanged() {
            // 限制范围在10%-80%之间（拖动只更新运行值，落盘在主窗口关闭时统一处理）
            TreeSplitRatio = Math.Max(0.1, Math.Min(0.8, TreeSplitRatio));
        }

        private void OnSelectedAppChanged() {
            RelayCommand.Raise(StartAppCommand, StopAppCommand, EditAppCommand, DeleteAppCommand);
        }

        // —— 软件设置：界面字体/颜色（ThemeUtils启动时加载到Configs并应用，保存后即时生效）——
        /// <summary>字体类型下拉选项</summary>
        public static List<string> FontOptions = FontsUtils.GetSystemFonts();
        /// <summary>字号下拉选项</summary>
        public static readonly string[] FontSizeOptions = {
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "20", "22", "24", "28"
        };
        /// <summary>颜色下拉选项（色块+颜色名展示）</summary>
        public static readonly IReadOnlyList<ColorOption> ColorOptions = new[] {
            "Black", "White", "Gray", "LightGray", "DarkGray", "Red", "Green", "Blue", "Yellow",
            "Orange", "Purple", "Cyan", "Brown", "Pink", "Gold"
        }.Select(n => new ColorOption(n)).ToArray();
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string GlobalFontFamily { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string GlobalFontSizeText { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string GlobalFontColor { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string BtnFontFamily { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string BtnFontSizeText { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string TreeFontFamily { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string TreeFontSizeText { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string TreeFontColor { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string LogFontColor { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string LogBgColor { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string LogFontSizeText { get; set; }
        [OnChangedMethod(nameof(RaiseResetSettingCommand))]
        public string LogFontFamily { get; set; }

        #region 设置页（对应老项目FormSetEvent）

        // LoadEnvSettings的ini加载已移至MainWindow构造函数统一处理（含Configs缓存与关闭方式默认写）

        /// <summary>
        /// 应用设置（设置页”应用”按钮，对应老项目SetSaveButton_Click的生效部分）：
        /// 把页面当前值生效到运行时——界面字体/颜色写资源、JDK/配置/日志同步JavaAppUtils静态值、
        /// 监控参数即时更新。不写ini也不回写Configs（Configs保持启动基线），
        /// 落盘统一在主窗口关闭时按”页面值 vs Configs基线”比对处理
        /// </summary>
        public void ApplySettings() {
            // 字号解析失败保持当前生效值（基线值），日志字号限8~40
            var globalFontSize = int.TryParse(GlobalFontSizeText, out var gfs) ? gfs : Configs.GlobalFontSize;
            var btnFontSize = int.TryParse(BtnFontSizeText, out var bfs) ? bfs : Configs.BtnFontSize;
            var treeFontSize = int.TryParse(TreeFontSizeText, out var tfs) ? tfs : Configs.TreeFontSize;
            var logFontSize = int.TryParse(LogFontSizeText, out var lfsVal) ? Math.Max(8, Math.Min(40, lfsVal)) : Configs.LogFontSize;

            // 界面字体/颜色立即生效（显式值直写资源，不经过Configs）
            ThemeUtils.ApplyResources(
                GlobalFontFamily, globalFontSize, GlobalFontColor,
                BtnFontFamily, btnFontSize,
                TreeFontFamily, treeFontSize, TreeFontColor,
                LogFontColor, LogBgColor, logFontSize, LogFontFamily);

            // 日志字体可能变化，各运行Tab的不换行宽度按新字体全量重算
            foreach (var tab in RunningApps) {
                tab.RefreshWrapWidth();
            }

            // JDK/配置/日志静态值直接同步（bat脚本生成用；不能走LoadSettings回读ini——尚未落盘）
            JavaAppUtils.JdkPath = JdkPath ?? string.Empty;
            JavaAppUtils.Profiles = SelectedProfile ?? string.Empty;
            JavaAppUtils.LogPath = LogPath ?? string.Empty;

            // 监控参数即时生效（预警开关/地址/机器名为服务运行值，Configs保持基线）
            var heartBeat = HeartBeatMsg ?? string.Empty;
            _monitorService.HeartBeatMsg = string.IsNullOrEmpty(heartBeat) ? Configs.HeartBeat : heartBeat;
            _monitorService.IsWarn = IsWarnEnabled;
            _monitorService.WarnAddress = WarnUrl ?? string.Empty;
            _monitorService.ServerName = ServerName ?? string.Empty;
            if (int.TryParse(MonitorTimeoutText, out var timeout) && timeout > 0) {
                Configs.MonitorTimeout = timeout;
            }
            if (int.TryParse(MonitorIntervalText, out var interval) && interval > 0) {
                Configs.MonitorInterval = interval;
                _monitorService.UpdateInterval();
            }

            // 系统换行配置变化后，项目为“系统”模式的运行Tab按新配置生效
            RefreshRunTabsWrapping();

            ShowMessage("已应用", Brushes.Green);
        }

        private void OnApplySettings(object obj) {
            ApplySettings();
        }

        /// <summary>
        /// Configs中的软件设置值同步到设置页各绑定属性显示
        /// （主窗口LoadEnvSettings与本VM的恢复默认两处调用）
        /// </summary>
        public void SyncSoftwareSettingsToView() {
            GlobalFontFamily = Configs.GlobalFontFamily;
            GlobalFontSizeText = Configs.GlobalFontSize.ToString();
            GlobalFontColor = Configs.GlobalFontColor;
            BtnFontFamily = Configs.BtnFontFamily;
            BtnFontSizeText = Configs.BtnFontSize.ToString();
            TreeFontFamily = Configs.TreeFontFamily;
            TreeFontSizeText = Configs.TreeFontSize.ToString();
            TreeFontColor = Configs.TreeFontColor;
            LogFontColor = Configs.LogFontColor;
            LogBgColor = Configs.LogBgColor;
            LogFontSizeText = Configs.LogFontSize.ToString();
            LogFontFamily = Configs.LogFontFamily;
            SelectedRunTabRows = Configs.RunTabRows.ToString();
            IsClosedExit = Configs.ClosedTypeExit.Equals(Configs.ClosedType);
            IsClosedMin = Configs.ClosedTypeMin.Equals(Configs.ClosedType);
        }

        /// <summary>
        /// 恢复软件设置为默认配置（对应软件设置页“恢复默认”按钮）：
        /// 把关闭方式/界面字体/按钮字体/应用树/日志的各项默认值赋到页面属性并立即应用生效；
        /// 不写ini不回写Configs（Configs保持启动基线），落盘在主窗口关闭时按基线比对处理
        /// </summary>
        private void OnResetSoftwareSettings(object obj) {
            if (MessageBoxResult.OK != MessageBox.Show("确认恢复软件设置为默认配置？\r\n当前的自定义字体、颜色、关闭方式等设置将被重置。",
                "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                return;
            }

            // 默认值与Configs各字段初始值保持一致，直接赋到页面属性
            GlobalFontFamily = Configs.DefaultFontFamily;
            GlobalFontSizeText = "12";
            GlobalFontColor = "Black";
            BtnFontFamily = Configs.DefaultFontFamily;
            BtnFontSizeText = "14";
            TreeFontFamily = Configs.DefaultFontFamily;
            TreeFontSizeText = "16";
            TreeFontColor = "Black";
            LogFontColor = "White";
            LogBgColor = "Black";
            LogFontSizeText = "14";
            LogFontFamily = "Consolas";
            IsClosedExit = true;
            SelectedLogWrap = Configs.LogWrapNone;
            SelectedRunTabRows = "1";

            // 默认值立即应用生效
            ApplySettings();
            ShowMessage("已恢复默认设置", Brushes.Green);
        }

        /// <summary>
        /// 环境配置是否有未保存的修改（对应老项目Form1_Closing的未保存判断）
        /// </summary>
        public bool HasUnsavedEnvChanges() {
            var closedType = IsClosedExit ? Configs.ClosedTypeExit : Configs.ClosedTypeMin;
            return !string.Equals(JdkPath ?? string.Empty, JavaAppUtils.JdkPath ?? string.Empty)
                || !string.Equals(SelectedProfile ?? string.Empty, JavaAppUtils.Profiles ?? string.Empty)
                || !string.Equals(LogPath ?? string.Empty, JavaAppUtils.LogPath ?? string.Empty)
                || !string.Equals(closedType, Configs.ClosedType)
                || !string.Equals(SelectedLogWrap ?? string.Empty, Configs.LogWrap);
        }

        /// <summary>
        /// 重建设置页自启动列表（项目增删改后联动）
        /// </summary>
        private void BuildAutoRunItems() {
            _buildingAutoRun = true;
            AutoRunItems.Clear();
            foreach (var section in _appService.GetAllSections()) {
                var app = _appService.GetAppBySection(section);
                if (app == null) continue;
                var item = new AutoRunItemModel {
                    Section = app.Section,
                    Title = app.Title,
                    IsChecked = app.AutoRun,
                    CheckedChanged = HandleAutoRunItemChanged // 初始赋值后再挂回调，避免加载时误触发入库
                };
                AutoRunItems.Add(item);
            }
            AutoRunAll = AutoRunItems.Count > 0 && AutoRunItems.All(i => i.IsChecked);
            _buildingAutoRun = false;
        }

        /// <summary>
        /// 自启动项勾选变化：同步项目AutoRun并入库（对应老项目AutoRunCheckBox_CheckecChanged）
        /// </summary>
        private void HandleAutoRunItemChanged(AutoRunItemModel item) {
            var app = _appService.GetAppBySection(item.Section);
            if (app == null || app.AutoRun == item.IsChecked) return;

            app.AutoRun = item.IsChecked;
            var record = AppDbService.Instance.GetBySection(item.Section);
            if (record != null) {
                record.AutoRun = item.IsChecked;
                AppDbService.Instance.SaveApp(record);
            }

            // 单项变化后刷新“全选”勾选状态（不触发批量应用）
            _buildingAutoRun = true;
            AutoRunAll = AutoRunItems.Count > 0 && AutoRunItems.All(i => i.IsChecked);
            _buildingAutoRun = false;
        }

        private void OnBrowseJdk(object obj) {
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

        private void OnBrowseLog(object obj) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog {
                Description = "选择日志目录",
                ShowNewFolderButton = true
            };
            if (!string.IsNullOrEmpty(LogPath) && Directory.Exists(LogPath)) {
                dialog.SelectedPath = LogPath;
            }
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                LogPath = dialog.SelectedPath;
            }
        }

        #endregion

        #region 应用树构建

        private void BuildTree() {
            AppTree.Clear();

            var allApps = _appService.GetAllSectionDic();
            var groupList = _appService.GroupList ?? new List<string>();
            // 收缩组名单来自Configs（主窗口启动时LoadTreeCollapsedState已加载ini，VM不读ini）
            var collapsedGroups = new HashSet<string>();
            foreach (var name in (Configs.TreeCollapsed ?? string.Empty).Split('|')) {
                if (!string.IsNullOrEmpty(name)) {
                    collapsedGroups.Add(name);
                }
            }
            // 按Sort排序（拖拽排序持久化后，启动按上次排定的顺序展示）
            var orderedSections = _appService.GetAllSections()
                .OrderBy(s => _appService.GetAppBySection(s)?.Sort ?? 0)
                .ToList();

            foreach (var group in groupList) {
                var groupNode = CreateGroupNode(group);
                // 恢复上次关闭前的收缩状态（名单外的组默认展开）
                groupNode.IsExpanded = !collapsedGroups.Contains(group);

                foreach (var section in orderedSections) {
                    var app = _appService.GetAppBySection(section);
                    if (app != null && app.Group == group) {
                        groupNode.Children.Add(CreateChildNode(app, group));
                    }
                }

                AppTree.Add(groupNode);
            }

            // 添加未分组的应用
            foreach (var section in orderedSections) {
                var app = _appService.GetAppBySection(section);
                if (app != null && string.IsNullOrEmpty(app.Group)) {
                    AppTree.Add(CreateChildNode(app, null));
                }
            }

            // 刷新组节点聚合颜色
            foreach (var group in groupList) {
                UpdateGroupNodeColor(group);
            }

            // 同步设置页自启动列表（对应老项目InitProject中对AutoRunAppFlowLayoutPanel的维护）
            BuildAutoRunItems();
        }

        /// <summary>
        /// 增量同步应用树（AppListChanged时调用）：已有节点实例保留（组的展开/收缩状态不丢），
        /// 只新增/更新变化的节点、移除已删除的节点，不全量重建
        /// </summary>
        private void SyncTree() {
            var dic = _appService.GetAllSectionDic();
            var groupList = _appService.GroupList ?? new List<string>();

            // 现有子节点索引：section → (节点, 所在集合)
            var existIndex = new Dictionary<string, TreeItemModel>();
            var parentIndex = new Dictionary<TreeItemModel, System.Collections.IList>();
            foreach (var groupNode in AppTree) {
                if (groupNode.Flag == Configs.NodeFlagGroup) {
                    foreach (var child in groupNode.Children) {
                        existIndex[child.Section] = child;
                        parentIndex[child] = groupNode.Children;
                    }
                } else if (groupNode.Section != null) {
                    existIndex[groupNode.Section] = groupNode;
                    parentIndex[groupNode] = AppTree;
                }
            }

            // 1. 移除已删除的section节点
            foreach (var entry in existIndex.ToList()) {
                if (!dic.ContainsKey(entry.Key)) {
                    if (parentIndex[entry.Value] is ObservableCollection<TreeItemModel> parent) {
                        parent.Remove(entry.Value);
                    }
                }
            }

            // 2. 移除已不在组集合中的组节点（清空场景；组内项目删空但组还在GroupList中时保留，与老版一致）
            foreach (var groupNode in AppTree.ToList()) {
                if (groupNode.Flag == Configs.NodeFlagGroup && !groupList.Contains(groupNode.GroupName)) {
                    AppTree.Remove(groupNode);
                }
            }

            // 3. 逐个更新/新增当前项目
            foreach (var section in _appService.GetAllSections()) {
                var app = _appService.GetAppBySection(section);
                if (app == null) continue;

                // 计算目标父集合（有组→组节点子级，无组→根）
                ObservableCollection<TreeItemModel> targetParent;
                TreeItemModel targetGroupNode = null;
                if (string.IsNullOrEmpty(app.Group)) {
                    targetParent = AppTree;
                } else {
                    targetGroupNode = AppTree.FirstOrDefault(n => n.Flag == Configs.NodeFlagGroup && n.GroupName == app.Group);
                    if (targetGroupNode == null) {
                        // 新组：创建组节点并追加（对应老项目Init2Tree中按需添加组节点）
                        targetGroupNode = CreateGroupNode(app.Group);
                        AppTree.Add(targetGroupNode);
                    }
                    targetParent = targetGroupNode.Children;
                }

                if (existIndex.TryGetValue(app.Section, out var node)) {
                    // 已存在：原地更新属性；换组时搬移节点（保留节点实例，不影响其它组展开状态）
                    UpdateChildNode(node, app);
                    if (parentIndex[node] is ObservableCollection<TreeItemModel> oldParent && oldParent != targetParent) {
                        oldParent.Remove(node);
                        targetParent.Add(node);
                    }
                } else {
                    // 新增：追加到目标集合末尾（对应老项目Init2Tree的添加位置）
                    targetParent.Add(CreateChildNode(app, app.Group));
                    // 新项目所在的组若处于收缩状态，展开该组让新项目可见；其它组保持不变
                    if (targetGroupNode != null) {
                        targetGroupNode.IsExpanded = true;
                    }
                }
            }

            // 刷新组节点聚合颜色
            foreach (var group in groupList) {
                UpdateGroupNodeColor(group);
            }

            // 同步设置页自启动列表
            BuildAutoRunItems();
        }

        #region 项目树组展开/收缩状态记忆

        // 收缩组名单的ini加载在主窗口构造函数（LoadTreeCollapsedState→Configs.TreeCollapsed），
        // 保存也在主窗口关闭时统一处理（PreCloseSaveData，直接读AppTree）

        /// <summary>
        /// 拖拽排序落定后按树序重排Sort并入库（对应老项目SortApp）：
        /// 深度优先遍历所有项目，遍历序号从1开始连续编号；
        /// 序号与项目当前Sort一致且分组未变化的项目跳过，只update实际变化的项目；
        /// 直接写缓存和数据库，不触发AppListChanged，避免增量同步干扰手动排序结果
        /// </summary>
        public void SaveTreeOrder() {
            try {
                var sort = 1;
                Walk(AppTree);
                void Walk(IEnumerable<TreeItemModel> nodes) {
                    foreach (var node in nodes) {
                        if (Configs.NodeFlagGroup.Equals(node.Flag)) {
                            Walk(node.Children);
                            continue;
                        }
                        if (string.IsNullOrEmpty(node.Section)) continue;

                        var app = _appService.GetAppBySection(node.Section);
                        if (app == null) continue;

                        var newGroup = node.GroupName ?? string.Empty;
                        if (app.Sort == sort && string.Equals(app.Group ?? string.Empty, newGroup)) {
                            // 序号与分组都没变，无需update
                            sort++;
                            continue;
                        }

                        app.Sort = sort++;
                        app.Group = newGroup;
                        _appService.UpdateApp(app.Section, app);

                        var record = AppDbService.Instance.GetBySection(app.Section);
                        if (record != null) {
                            record.Sort = app.Sort;
                            record.Group = app.Group;
                            AppDbService.Instance.SaveApp(record);
                        }
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show($"排序保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private TreeItemModel CreateGroupNode(string group) {
            return new TreeItemModel {
                Name = group,
                Flag = Configs.NodeFlagGroup,
                ImageKey = Configs.GroupNodeImageKey,
                Icon = GroupIcon,
                GroupName = group,
                StartCommand = GroupStartCommand,
                StopCommand = GroupStopCommand,
                DeleteCommand = GroupDeleteCommand,
                WarnOnCommand = GroupWarnOnCommand,
                WarnOffCommand = GroupWarnOffCommand,
            };
        }

        /// <summary>编辑后原地更新节点属性（不动节点实例，展开状态/选中不受影响）</summary>
        private void UpdateChildNode(TreeItemModel node, AppRecord app) {
            node.Name = app.Title;
            node.ImageKey = GetImageKey(app.Type);
            node.Icon = GetNodeIcon(app);
            node.GroupName = app.Group;
            node.RunStat = app.RunStat;
            node.IsRunning = app.IsRunning;
            node.IsWarn = app.IsWarn;
            node.Remark = app.Remark;
            node.BackgroundBrush = GetStatBrush(app.RunStat);
        }

        private TreeItemModel CreateChildNode(AppRecord app, string group) {
            return new TreeItemModel {
                Name = app.Title,
                Flag = Configs.NodeFlagChild,
                Section = app.Section,
                ImageKey = GetImageKey(app.Type),
                Icon = GetNodeIcon(app),
                GroupName = group,
                RunStat = app.RunStat,
                IsRunning = app.IsRunning,
                IsWarn = app.IsWarn,
                Remark = app.Remark,
                BackgroundBrush = GetStatBrush(app.RunStat),
                DetailCommand = DetailAppCommand,
                WarnSwitchCommand = WarnSwitchAppCommand,
                StartCommand = StartAppCommand,
                StopCommand = StopAppCommand,
                ResetRunStatCommand = ResetRunStatCommand,
                EditCommand = EditAppCommand,
                DeleteCommand = DeleteAppCommand,
                LocateFileCommand = LocateFileCommand,
            };
        }

        private string GetImageKey(string type) {
            return type switch {
                Configs.AppTypeJava => Configs.ChildNodeJavaImageKey,
                Configs.AppTypeBat => Configs.ChildNodeBatImageKey,
                Configs.AppTypeExe => Configs.ChildNodeExeImageKey,
                _ => "default.ico"
            };
        }

        private ImageSource GetNodeIcon(AppRecord app) {
            if (Configs.AppTypeExe.Equals(app.Type)) {
                // exe类型优先使用exe内嵌图标（对应老项目ExeIconUtils.GetIconImage）
                return ExeIconUtils.GetIconImage(app.App) ?? ExeDefaultIcon;
            }
            return app.Type switch {
                Configs.AppTypeJava => JavaIcon,
                Configs.AppTypeBat => BatIcon,
                _ => ExeDefaultIcon
            };
        }

        /// <summary>
        /// 按section查找树节点（含组内子节点）
        /// </summary>
        private TreeItemModel FindNodeBySection(string section) {
            foreach (var node in AppTree) {
                if (node.Section == section) return node;
                foreach (var child in node.Children) {
                    if (child.Section == section) return child;
                }
            }
            return null;
        }

        /// <summary>
        /// 运行状态对应节点背景色（对应老项目FreshAppColor）
        /// </summary>
        private Brush GetStatBrush(short runStat) {
            return runStat switch {
                Configs.AppRunStatRunning => StatRunningBrush,
                Configs.AppRunStatStopping => StatStoppingBrush,
                Configs.AppRunStatSuccess => StatSuccessBrush,
                Configs.AppRunStatFail => StatFailBrush,
                _ => StatUnrunBrush
            };
        }

        #endregion

        #region 应用增删改查

        private void OnAddApp(object obj) {
            var window = new Views.AppFormWindow();
            window.SetOperateType(Configs.OperateTypeAdd);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private TreeItemModel GetNode(object obj) {
            // 右键菜单传节点参数；工具栏按钮走当前选中节点
            return obj as TreeItemModel ?? SelectedApp;
        }

        private void OnEditApp(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;

            var app = _appService.GetAppBySection(node.Section);
            var window = new Views.AppFormWindow(app.IsRunning);
            window.SetOperateType(Configs.OperateTypeUpdate, node.Section);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void OnDeleteApp(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;

            var result = MessageBox.Show($"确认删除{node.Name}？", "警告",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK) {
                try {
                    _appService.DeleteFromDb(node.Section);
                    CloseTabBySection(node.Section);
                    ShowMessage($"{node.Name}删除成功！", Brushes.DarkOrange);
                } catch (System.Exception ex) {
                    MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnDetailApp(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;

            var window = new Views.AppFormWindow();
            window.SetOperateType(Configs.OperateTypeDetail, node.Section);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private void OnWarnSwitchApp(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;
            var isWarn = !node.IsWarn;
            SetNodeWarn(node, isWarn);
            ShowMessage($"{(isWarn ? "开启" : "关闭")}项目{node.Name}警报", Brushes.Orange);
        }

        /// <summary>
        /// 设置应用警报开关并同步到数据库和树节点
        /// </summary>
        private void SetNodeWarn(TreeItemModel node, bool isWarn) {
            var app = _appService.GetAppBySection(node.Section);
            if (app == null) return;

            app.IsWarn = isWarn;
            var record = AppDbService.Instance.GetBySection(app.Section);
            if (record != null) {
                record.IsWarn = isWarn;
                AppDbService.Instance.SaveApp(record);
            }
            node.IsWarn = isWarn;
        }

        private void OnLocateFile(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;
            var app = _appService.GetAppBySection(node.Section);
            if (app == null || string.IsNullOrEmpty(app.App) || !File.Exists(app.App)) {
                MessageBox.Show("应用文件不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Process.Start("explorer.exe", $"/select,\"{app.App}\"");
        }

        #endregion

        #region 应用启动/停止（对应老项目EventService + CliWrapUtils.AppStart/AppStop）

        private void OnStartApp(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;

            var app = _appService.GetAppBySection(node.Section);
            if (app == null) return;

            if (app.RunStat == Configs.AppRunStatSuccess || app.RunStat == Configs.AppRunStatRunning) {
                MessageBox.Show("项目不可重复启动!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            StartApp(app, true);
        }

        private void OnStopApp(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;

            var app = _appService.GetAppBySection(node.Section);
            if (app == null) return;

            StopApp(app);
        }

        /// <summary>
        /// 重置运行状态为未运行：程序在软件外异常退出/中断后，监控会持续判失败
        /// （一直刷app is fail并重复预警，节点保持橙色）；手动标识为初始未运行后，
        /// 监控不再判失败（若程序实际仍在运行，下轮监控会自动恢复绿色）
        /// </summary>
        private void OnResetRunStat(object obj) {
            var node = GetNode(obj);
            if (node == null || string.IsNullOrEmpty(node.Section))
                return;

            var app = _appService.GetAppBySection(node.Section);
            if (app == null) return;

            app.RunStat = Configs.AppRunStatUnrun;
            app.IsRunning = false;
            UpdateRunStat(app.Section, Configs.AppRunStatUnrun, false); // 刷新节点颜色/Tab/组聚合色
            ShowMessage($"已将{node.Name}标识为未运行", Brushes.Green);
        }

        /// <summary>
        /// 启动应用：状态置为启动中，创建运行Tab并执行进程（对应老项目BtnRightStartClick + CliWrapUtils.AppStart）
        /// </summary>
        private void StartApp(AppRecord app, bool selectTab) {
            app.RunStat = Configs.AppRunStatRunning;
            UpdateRunStat(app.Section, Configs.AppRunStatRunning, app.IsRunning);
            EnsureTab(app, selectTab);
            _runService.StartApp(app);
        }

        /// <summary>
        /// 自启动（监控初始化时触发，状态已由监控置为启动中）
        /// </summary>
        private void AutoStartApp(AppRecord app) {
            EnsureTab(app, true);
            _runService.StartApp(app);
        }

        /// <summary>
        /// 停止应用（对应老项目EventService.StopApp）：
        /// 配置了actuator地址的先调用监控的shutdown接口，失败时询问是否改用脚本停止；
        /// 未配置的直接走脚本/进程停止
        /// </summary>
        private async void StopApp(AppRecord app) {
            // 状态设置为停止中
            app.RunStat = Configs.AppRunStatStopping;
            UpdateRunStat(app.Section, Configs.AppRunStatStopping, app.IsRunning);
            EnsureTab(app, true);

            if (!string.IsNullOrEmpty(app.Actuator)) {
                var shutDownUrl = (app.Actuator.EndsWith(Configs.PathCharacter) ? app.Actuator : app.Actuator + Configs.PathCharacter) + Configs.ActuatorShutdown;
                try {
                    // actuator请求要使用post，使用默认的application/json方式请求
                    await Task.Run(() => { HttpUtils.PostRequest(shutDownUrl, null, null); });
                } catch (System.Exception ex) {
                    Debug.WriteLine(ex.StackTrace);
                    if (MessageBoxResult.OK == MessageBox.Show("监控/shutdown无法请求,使用脚本停止？", "提醒",
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                        ScriptStop(app);
                    }
                    return;
                }
                // shutdown请求成功，等待监控确认停止
            } else {
                ScriptStop(app);
            }
        }

        /// <summary>
        /// 脚本停止：先结束跟踪中的进程，再执行停止脚本/exe按进程名杀（对应老项目CliWrapUtils.AppStop）；
        /// 进程退出等待（WaitForExit最长1秒）、强杀、停止脚本启动都较慢，放线程池执行避免卡UI
        /// （与启动一致：界面只负责置状态和建Tab，立即返回）；日志经LogReceived→RunOnUi切回UI线程
        /// </summary>
        private void ScriptStop(AppRecord app) {
            EnsureTab(app, true);
            Task.Run(() => _runService.StopApp(app));
        }

        private void OnGroupStart(object obj) {
            if (!(obj is TreeItemModel group))
                return;

            foreach (var child in group.Children.ToList()) {
                var app = _appService.GetAppBySection(child.Section);
                if (app == null || app.RunStat == Configs.AppRunStatSuccess || app.RunStat == Configs.AppRunStatRunning) continue;
                StartApp(app, false);
            }
        }

        private void OnGroupStop(object obj) {
            if (!(obj is TreeItemModel group))
                return;

            foreach (var child in group.Children.ToList()) {
                var app = _appService.GetAppBySection(child.Section);
                if (app == null || app.RunStat != Configs.AppRunStatSuccess) continue;
                StopApp(app);
            }
        }

        /// <summary>
        /// 项目全部操作：确认后按状态过滤批量启动/停止（对应老项目AllAppOperate）
        /// </summary>
        private void OnAllStart(object obj) {
            AllAppOperate(Configs.ProjectOperateTypeStart);
        }

        private void OnAllStop(object obj) {
            AllAppOperate(Configs.ProjectOperateTypeStop);
        }

        private void AllAppOperate(int type) {
            var sectionDic = _appService.GetAllSectionDic();
            if (null == sectionDic || 0 == sectionDic.Count) {
                MessageBox.Show("项目配置为空，请先添加项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var operateMsg = Configs.ProjectOperateTypeStart == type ? "确认全部启动吗?" : "确认全部停止吗?";
            if (MessageBox.Show(operateMsg, "信息", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var entry in sectionDic) {
                var app = entry.Value;
                if (null == app) continue;

                if (Configs.ProjectOperateTypeStart == type && (Configs.AppRunStatUnrun == app.RunStat || Configs.AppRunStatFail == app.RunStat)) {
                    // 项目状态为未运行或运行失败才可运行
                    StartApp(app, false);
                } else if (Configs.ProjectOperateTypeStop == type && Configs.AppRunStatSuccess == app.RunStat) {
                    // 项目状态必须为运行中
                    StopApp(app);
                }
            }
        }

        #endregion

        #region 运行Tab管理（对应老项目AppRunTabControl相关事件）

        /// <summary>
        /// 获取应用对应的运行Tab，不存在时创建（对应老项目CreateAppRunTabPage）
        /// </summary>
        private AppRunTabItemModel EnsureTab(AppRecord app, bool select) {
            var tab = RunningApps.FirstOrDefault(t => t.Section == app.Section);
            if (tab == null) {
                tab = new AppRunTabItemModel {
                    Section = app.Section,
                    Title = app.Title,
                    IsRunning = app.IsRunning,
                    StartTime = DateTime.Now
                };
                RunningApps.Add(tab);
            }
            // 运行日志换行：项目配置的模式 + 按系统配置解析后的实际TextWrapping
            tab.LogWrap = app.LogWrap;
            tab.LogWrapping = Configs.ResolveLogWrapping(app.LogWrap);
            if (select) {
                SelectedTabIndex = RunningApps.IndexOf(tab);
            }
            return tab;
        }

        /// <summary>
        /// 系统换行配置变化后刷新运行Tab的实际换行（项目为“系统”模式的跟随新配置）
        /// </summary>
        private void RefreshRunTabsWrapping() {
            foreach (var tab in RunningApps) {
                var app = _appService.GetAppBySection(tab.Section);
                tab.LogWrap = app?.LogWrap;
                tab.LogWrapping = Configs.ResolveLogWrapping(app?.LogWrap);
            }
        }

        /// <summary>
        /// 运行日志右键切换项目换行模式：更新Tab展示并持久化到数据库（对应警报右键切换的入库方式）
        /// </summary>
        public void SetRunTabLogWrap(AppRunTabItemModel tab, string wrapMode) {
            var app = _appService.GetAppBySection(tab.Section);
            if (app == null) return;

            app.LogWrap = wrapMode;
            tab.LogWrap = wrapMode;
            tab.LogWrapping = Configs.ResolveLogWrapping(wrapMode);

            var record = AppDbService.Instance.GetBySection(app.Section);
            if (record != null) {
                record.LogWrap = wrapMode;
                AppDbService.Instance.SaveApp(record);
            }
        }

        private AppRunTabItemModel FindTab(string section) {
            return RunningApps.FirstOrDefault(t => t.Section == section);
        }

        /// <summary>
        /// 追加运行日志，Tab不存在时自动创建（对应老项目监控中找不到TabPage则创建）
        /// </summary>
        private void AppendTabLog(string section, string line) {
            var app = _appService.GetAppBySection(section);
            var tab = FindTab(section);
            if (tab == null) {
                if (app == null) return;
                tab = EnsureTab(app, false);
            }
            tab.AppendLog(line);
        }

        /// <summary>
        /// 单击树节点时选中对应的运行Tab（对应老项目AppTreeView_NodeMouseClick）
        /// </summary>
        public void SelectRunTab(string section) {
            var tab = FindTab(section);
            if (tab != null) {
                SelectedTabIndex = RunningApps.IndexOf(tab);
            }
        }

        /// <summary>
        /// 关闭Tab：运行中先确认停止（Tab保留，等监控确认停止后再关），已停止的直接移除
        /// （对应老项目AppRunTabControl_MouseDown的关闭区域点击）
        /// </summary>
        private void OnCloseTab(object obj) {
            if (!(obj is AppRunTabItemModel tabItem))
                return;

            var app = _appService.GetAppBySection(tabItem.Section);
            var isRunning = (app != null && app.IsRunning) || _runService.IsProcessAlive(tabItem.Section);
            if (isRunning) {
                if (MessageBoxResult.OK == MessageBox.Show("确认停止该程序？", "警告",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                    if (app != null) {
                        StopApp(app);
                    }
                }
            } else {
                RunningApps.Remove(tabItem);
            }
        }

        /// <summary>
        /// 关闭所有已停止的运行Tab（对应老项目AppRunTab_AllStopedClose）
        /// </summary>
        private void OnCloseStoppedTabs(object obj) {
            var closeTabs = RunningApps
                .Where(t => {
                    var app = _appService.GetAppBySection(t.Section);
                    return (app == null || !app.IsRunning) && !_runService.IsProcessAlive(t.Section);
                })
                .ToList();
            foreach (var tab in closeTabs) {
                RunningApps.Remove(tab);
            }
            if (closeTabs.Count > 0) {
                ShowMessage($"成功关闭{closeTabs.Count}个", Brushes.Green);
            } else {
                ShowMessage("当前无可关闭项", Brushes.DeepPink);
            }
        }

        private void CloseTabBySection(string section) {
            var tab = FindTab(section);
            if (tab != null) {
                RunningApps.Remove(tab);
            }
        }

        /// <summary>
        /// 运行Tab底部输入框回车：向进程stdin发送输入（对应老项目ExecuteApp的KeyDown处理）
        /// </summary>
        private void OnSendInput(object obj) {
            if (!(obj is AppRunTabItemModel tabItem))
                return;

            var input = tabItem.InputText;
            if (string.IsNullOrEmpty(input)) return;

            if (_runService.SendInput(tabItem.Section, input)) {
                tabItem.AppendLog($"> {input}");
                tabItem.InputText = string.Empty;
            }
        }

        #endregion

        #region 监控状态联动（对应老项目TimerUtils.FreshAppColor）

        /// <summary>
        /// 应用运行状态变化：同步树节点颜色/状态、运行Tab的运行标记、组节点聚合色
        /// </summary>
        private void UpdateRunStat(string section, short runStat, bool isRunning) {
            var app = _appService.GetAppBySection(section);
            var node = FindNodeBySection(section);
            if (node != null) {
                node.RunStat = runStat;
                node.IsRunning = isRunning;
                node.BackgroundBrush = GetStatBrush(runStat);
            }
            var tab = FindTab(section);
            if (tab != null) {
                tab.IsRunning = isRunning;
            }
            if (app != null) {
                UpdateGroupNodeColor(app.Group);
            }
            RelayCommand.Raise(StartAppCommand, StopAppCommand, EditAppCommand, DeleteAppCommand);
        }

        /// <summary>
        /// 组节点聚合色：按组内应用的运行状态汇总（对应老项目FreshAppColor的组节点部分）
        /// </summary>
        private void UpdateGroupNodeColor(string group) {
            if (string.IsNullOrEmpty(group)) return;
            var groupNode = AppTree.FirstOrDefault(n => n.Flag == Configs.NodeFlagGroup && n.GroupName == group);
            if (groupNode == null) return;

            var appList = _appService.GetAllSectionDic()?.Values.ToList() ?? new List<AppRecord>();
            var runningNum = 0;
            var unrunNum = 0;
            var failNum = 0;
            foreach (var app in appList) {
                if (app == null || !group.Equals(app.Group)) continue;
                switch (app.RunStat) {
                    case Configs.AppRunStatSuccess:
                        runningNum += 1;
                        break;
                    case Configs.AppRunStatFail:
                        failNum += 1;
                        break;
                    default:
                        unrunNum += 1;
                        break;
                }
            }

            Brush groupColor;
            if (runningNum > 0) {
                if (failNum > 0) {
                    groupColor = GroupPinkBrush;
                } else if (unrunNum > 0) {
                    groupColor = GroupYellowBrush;
                } else {
                    groupColor = StatSuccessBrush;
                }
            } else {
                groupColor = failNum > 0 ? GroupYellowGreenBrush : StatUnrunBrush;
            }
            groupNode.BackgroundBrush = groupColor;
        }

        #endregion

        #region 组操作

        private void OnGroupDelete(object obj) {
            if (!(obj is TreeItemModel group))
                return;

            var result = MessageBox.Show($"确认删除组{group.Name},其下所有项目也将删除？", "警告",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
                return;

            // 项目是否可移除判定（对应老项目GroupBtnRightDeleteClick，运行中不可删除）
            var hasRun = false;
            var willDeleteSections = new List<string>();
            foreach (var child in group.Children.ToList()) {
                var app = _appService.GetAppBySection(child.Section);
                if (app == null) continue;
                if (app.RunStat == Configs.AppRunStatSuccess) {
                    hasRun = true;
                } else {
                    willDeleteSections.Add(child.Section);
                }
            }

            if (hasRun) {
                MessageBox.Show("当前项目正在运行中，不可删除！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try {
                foreach (var section in willDeleteSections) {
                    var node = group.Children.FirstOrDefault(c => c.Section == section);
                    _appService.DeleteFromDb(section);
                    CloseTabBySection(section);
                }
                ShowMessage($"{group.Name}删除成功！", Brushes.DarkOrange);
            } catch (System.Exception ex) {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnGroupWarnOn(object obj) => SetGroupWarn(obj, true);

        private void OnGroupWarnOff(object obj) => SetGroupWarn(obj, false);

        private void SetGroupWarn(object obj, bool isWarn) {
            if (!(obj is TreeItemModel group))
                return;
            foreach (var child in group.Children) {
                SetNodeWarn(child, isWarn);
            }
            ShowMessage($"{(isWarn ? "开启" : "关闭")}组{group.Name}警告", Brushes.Orange);
        }

        /// <summary>
        /// 清除所有应用（对应老项目ClearAppButton_Click）：存在运行项目时二次确认（不会停止项目）
        /// </summary>
        private void OnClearAll(object obj) {
            if (MessageBoxResult.OK != MessageBox.Show("确认清除所有项目?", "警告",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning))
                return;

            var sectionDic = _appService.GetAllSectionDic();
            if (sectionDic == null || sectionDic.Count == 0) return;

            try {
                foreach (var entry in sectionDic) {
                    var app = entry.Value;
                    if (app != null && app.IsRunning) {
                        if (MessageBoxResult.OK == MessageBox.Show("存在运行项目，清空不会停止项目运行，确认继续清空?", "警告",
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                            break;
                        }
                        return;
                    }
                }
            } catch (System.Exception ex) {
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _appService.ClearDb();
            RunningApps.Clear();   // 项目运行Tab清空
            ShowMessage("清空成功", Brushes.Green);
        }

        private void OnExportConfig(object obj) {
            var exportWindow = new Views.AppExportWindow {
                Owner = Application.Current.MainWindow
            };
            exportWindow.ShowDialog();
        }

        /// <summary>
        /// 导入.amp配置文件（对应老项目ImportButton_Click）：AES解密后按 标题+类型+路径+端口 判重，可选覆盖
        /// </summary>
        private void OnImportConfig(object obj) {
            while (true) {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = @"Amp Files(*.amp)|*amp" };
                if (openFileDialog.ShowDialog() != true) return;

                Dictionary<string, AppRecord> appDic;
                try {
                    var encryptContent = File.ReadAllText(openFileDialog.FileName, new UTF8Encoding(false));
                    var content = AesUtils.DecryptContent(encryptContent);
                    appDic = JsonConvert.DeserializeObject<Dictionary<string, AppRecord>>(content);
                } catch (System.Exception ex) {
                    MessageBox.Show($"文件解析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                if (appDic == null || appDic.Count == 0) {
                    MessageBox.Show(@"所选文件无配置项,请重新选择", @"提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    continue;
                }

                string isOverride = null;
                var importNum = 0;
                foreach (var item in appDic) {
                    var app = item.Value;
                    if (app == null) continue;
                    var existSection = FindExistSection(app);
                    if (!string.IsNullOrEmpty(existSection)) {
                        // 存在重复，是否覆盖只询问一次
                        isOverride ??= MessageBoxResult.Yes == MessageBox.Show("存在相同配置，是否覆盖本地?", "警告",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) ? "true" : "false";
                        if ("true".Equals(isOverride)) {
                            app.Section = existSection;
                            SaveProject(app);
                            importNum++;
                        }
                    } else {
                        SaveProject(app);
                        importNum++;
                    }
                }
                MessageBox.Show($"成功导入{importNum}服务！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            }
        }

        /// <summary>
        /// 按标题+类型+路径+端口查找本地已存在的相同配置
        /// </summary>
        private string FindExistSection(AppRecord app) {
            foreach (var entry in _appService.GetAllSectionDic()) {
                var exist = entry.Value;
                if (exist == null) continue;
                if (!string.IsNullOrEmpty(app.Title) && !string.IsNullOrEmpty(exist.Title)
                    && app.Title.Equals(exist.Title)
                    && app.Port != null && exist.Port != null && app.Port.Equals(exist.Port)
                    && !string.IsNullOrEmpty(app.App) && !string.IsNullOrEmpty(exist.App) && app.App.Equals(exist.App)
                    && !string.IsNullOrEmpty(app.Type) && !string.IsNullOrEmpty(exist.Type) && app.Type.Equals(exist.Type)) {
                    return entry.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// 保存导入的项目：入库+更新缓存+生成java脚本（对应老项目SaveProject）
        /// </summary>
        private void SaveProject(AppRecord app) {
            // 清空导出文件带来的库表字段：id为导出端库的主键，留着会与本库自增主键冲突
            app.Id = null;
            app.CreateTime = null;
            app.UpdateTime = null;
            if (string.IsNullOrEmpty(app.LogWrap)) {
                app.LogWrap = Configs.LogWrapSystem;
            }
            app.RunStat = Configs.AppRunStatUnrun;
            app.IsRunning = false;
            _appService.SaveToDb(app);
        }

        #endregion

        #region 状态栏提示（对应老项目TimerUtils.ShowOperateLabelMessage）

        public void ShowMessage(string message, Brush brush = null, int seconds = 5) {
            OperateMessage = message;
            OperateMessageBrush = brush ?? Brushes.Black;
            _messageTimer.Stop();
            _messageTimer.Interval = TimeSpan.FromSeconds(seconds);
            _messageTimer.Start();
        }

        #endregion

        private void OnExpandAll(object obj) {
            ExpandAllRequested?.Invoke();
        }

        private void OnCollapseAll(object obj) {
            CollapseAllRequested?.Invoke();
        }

        private bool CanEditOrDeleteApp(object obj) => GetNode(obj) != null;
        private bool CanStartApp(object obj) => GetNode(obj) is TreeItemModel n && n.CanStart;
        private bool CanStopApp(object obj) => GetNode(obj) is TreeItemModel n && n.CanStop;
    }

    /// <summary>
    /// 颜色下拉选项：颜色名（存ini）+色块画刷（选项模板展示）
    /// </summary>
    public class ColorOption {
        public string Name { get; }
        public Brush Brush { get; }
        public ColorOption(string name) {
            Name = name;
            Brush = AMUtils.ThemeUtils.BrushOf(name) ?? Brushes.Black;
        }
    }
}
