using AppManager2.Config;
using AppManager2.Models;
using AppManager2.ViewModels;
using IniParser.Model;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using IniParserUtils = global::Utils.IniParserUtils;

namespace AppManager2 {
    public partial class MainWindow : Window {
        // 无字段初始化器：构造函数中先加载ini到Configs（建树依赖），再创建VM、给VM属性赋值
        private readonly MainWindowViewModel ViewModel;

        // 托盘图标（对应老项目AppManagerNotifyIcon）
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        // 托盘“退出”直达关闭，跳过关闭方式判断
        private bool _reallyClose;

        public MainWindow() {
            // 启动统一读取System.ini一次，按功能分发给各加载方法（VM不再直接读ini）
            var iniData = IniParserUtils.GetIniData(SystemIniPath);

            // 1. 纯Configs加载：项目树收缩组名单（VM建树前需就位）
            LoadTreeCollapsedState(iniData);

            InitializeComponent();

            // 2. 创建VM（建树/服务初始化从Configs取配置，不读ini）
            ViewModel = new MainWindowViewModel();

            // 3. 窗口/控件状态加载
            LoadWindowGeometry(iniData);
            LoadMainTabIndex(iniData);
            LoadSendFormConfig(iniData);

            // 4. 需要赋值到ViewModel的加载（值同时进Configs缓存，保存时比对用）
            LoadSplitRatio(iniData);
            LoadRunTabRows(iniData);
            LoadEnvSettings(iniData);

            RegisterEvents();
            InitNotifyIcon();
            InitAutoRunPanel();
            // 模型赋值
            DataContext = ViewModel;
        }

        /// <summary>
        /// 恢复上次关闭时选中的主Tab索引（System.ini [app] main_tab_index，保存见PreCloseSaveData）
        /// </summary>
        private void LoadMainTabIndex(IniData iniData) {
            try {
                var indexStr = iniData[Configs.IniSectionApp][Configs.IniKeyAppMainTabIndex];
                if (int.TryParse(indexStr, out var index) && index >= 0 && index < MainTabControl.Items.Count) {
                    MainTabControl.SelectedIndex = index;
                }
                Configs.SelectedIndex = MainTabControl.SelectedIndex;
            } catch { }
        }

        #region 自启动列表（对应老项目AutoRunAppFlowLayoutPanel逐个添加CheckBox的方式，WPF用WrapPanel实现自动换行）

        /// <summary>
        /// 监听AutoRunItems集合变化，往WrapPanel中逐个添加自启动CheckBox
        /// </summary>
        private void InitAutoRunPanel() {
            var items = ViewModel?.AutoRunItems;
            if (items == null) return;
            items.CollectionChanged += (s, e) => SyncAutoRunPanel();
            SyncAutoRunPanel();
        }

        /// <summary>
        /// 按当前集合内容重建面板中的CheckBox（IsChecked双向绑定到项，勾选联动入库由VM回调处理）
        /// </summary>
        private void SyncAutoRunPanel() {
            AutoRunPanel.Children.Clear();
            if (ViewModel == null) return;
            foreach (var item in ViewModel.AutoRunItems) {
                var checkBox = new CheckBox {
                    Content = item.Title,
                    Margin = new Thickness(18, 8, 0, 0),
                    DataContext = item
                };
                checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(Models.AutoRunItemModel.IsChecked)) { Mode = BindingMode.TwoWay });
                AutoRunPanel.Children.Add(checkBox);
            }
        }

        #endregion

        #region 窗口位置与尺寸记忆（对应老项目InitMainForm窗口加载 + ControlUtils.SaveFormSize/SaveFormLocation）

        private static string SystemIniPath => Configs.AppPath + "\\" + Configs.SystemIniPath;

        /// <summary>
        /// 从System.ini [system]读取上次的窗口位置和宽高；无记录时居中。
        /// 位置超出虚拟屏幕时拉回可见范围（对应老项目InitMainForm的VirtualScreen修正）；
        /// 上次以最大化关闭时恢复全屏（几何仍按正常态记录，还原时回到原尺寸）
        /// </summary>
        private void LoadWindowGeometry(IniData iniData) {
            var section = iniData[Configs.IniSectionSystem];
            var heightStr = section[Configs.IniKeyFormHeight];
            var widthStr = section[Configs.IniKeyFormWidth];
            var xStr = section[Configs.IniKeyFormLocationX];
            var yStr = section[Configs.IniKeyFormLocationY];
            Configs.WindowMaximized = "1".Equals(section[Configs.IniKeyFormMaximized]);

            var heightOk = double.TryParse(heightStr, out var height);
            var widthOk = double.TryParse(widthStr, out var width);
            var xOk = double.TryParse(xStr, out var x);
            var yOk = double.TryParse(yStr, out var y);
            var hasSize = heightOk && widthOk;
            var hasLocation = xOk && yOk;

            if (hasSize && height > MinHeight && width > MinWidth) {
                Height = height;
                Width = width;
            }
            Configs.Height = Convert.ToInt32(Height);
            Configs.Width = Convert.ToInt32(Width);
            if (hasSize || hasLocation) {
                if (!hasLocation) {
                    // 只有尺寸记录时保持默认居中坐标
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                } else {
                    // 位置超出虚拟屏幕时，调整位置（显示器拔掉/分辨率变化后窗口不至于丢在屏幕外）
                    var virtualLeft = SystemParameters.VirtualScreenLeft;
                    var virtualTop = SystemParameters.VirtualScreenTop;
                    var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
                    var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;
                    if (x + ActualWidth > virtualRight) x = virtualRight - Width;
                    else if (x < virtualLeft) x = virtualLeft;
                    if (y + Height > virtualBottom) y = virtualBottom - Height;
                    else if (y < virtualTop) y = virtualTop;
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = x;
                    Top = y;
                }
            } else {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            // 无位置记录时窗口由CenterScreen在Show阶段才定位，此时Left/Top为NaN，
            // Convert.ToInt32(NaN)会抛异常；缓存0，关闭时按实际位置正常写入
            Configs.Left = double.IsNaN(Left) ? 0 : Convert.ToInt32(Left);
            Configs.Top = double.IsNaN(Top) ? 0 : Convert.ToInt32(Top);

            // 恢复上次的 maximized 状态（Left/Top/Width/Height仍为正常态几何，还原时不丢原尺寸）
            if (Configs.WindowMaximized) {
                WindowState = WindowState.Maximized;
            }
        }

        // 窗口几何的保存在SaveWindowStateToIni中统一处理（对应老项目ResizeEnd/FormClose保存）

        #endregion

        #region 发送记录窗口布局记忆（System.ini [send]，ini读写集中在主窗口）

        // 启动时ini加载的初始值：主窗口关闭时与Configs运行值比对，值变化才写盘
        // （Configs.SendForm*为运行值：发送记录窗口打开时读取恢复，关闭时回写）
        private int _iniSendHeight;
        private int _iniSendWidth;
        private int _iniSendLeft;
        private int _iniSendTop;
        private int _iniSendSplitDis;

        /// <summary>
        /// 程序启动时把发送记录窗口的大小/位置/分栏宽度从System.ini [send]加载到Configs，
        /// 发送记录窗口打开时直接用Configs值恢复，其关闭时只回写Configs，不写ini
        /// </summary>
        private void LoadSendFormConfig(IniData iniData) {
            var section = iniData[Configs.IniSectionSend];
            if (int.TryParse(section[Configs.IniKeyFormHeight], out var h)) Configs.SendFormHeight = h;
            if (int.TryParse(section[Configs.IniKeyFormWidth], out var w)) Configs.SendFormWidth = w;
            if (int.TryParse(section[Configs.IniKeyFormLocationX], out var x)) Configs.SendFormLeft = x;
            if (int.TryParse(section[Configs.IniKeyFormLocationY], out var y)) Configs.SendFormTop = y;
            if (int.TryParse(section[Configs.IniKeySendSplitDis], out var s)) Configs.SendSplitDis = s;

            _iniSendHeight = Configs.SendFormHeight;
            _iniSendWidth = Configs.SendFormWidth;
            _iniSendLeft = Configs.SendFormLeft;
            _iniSendTop = Configs.SendFormTop;
            _iniSendSplitDis = Configs.SendSplitDis;
        }

        // 发送记录窗口布局的保存在SaveWindowStateToIni中统一处理
        // （_iniSend*为启动初值缓存，关闭时与Configs运行值比对）

        #endregion

        #region ini配置加载（启动时GetIniData读取一次，各方法按功能从iniData取值，VM不再直接读ini）

        /// <summary>
        /// 项目树收缩组名单（[app] tree_collapsed，|分隔）→ Configs缓存（须在VM建树前加载）
        /// </summary>
        private void LoadTreeCollapsedState(IniData iniData) {
            Configs.TreeCollapsed = iniData[Configs.IniSectionApp][Configs.IniKeyAppTreeCollapsed] ?? string.Empty;
        }

        /// <summary>
        /// 分栏比例（[app] split_ratio）→ Configs缓存 + ViewModel两侧比例
        /// </summary>
        private void LoadSplitRatio(IniData iniData) {
            try {
                if (iniData.Sections.ContainsSection(Configs.IniSectionApp)) {
                    var ratioStr = iniData[Configs.IniSectionApp][Configs.IniKeyAppSplitRatio];
                    if (!string.IsNullOrEmpty(ratioStr) && double.TryParse(ratioStr, out var ratio)) {
                        // 限制在10%-80%之间
                        ViewModel.TreeSplitRatio = Math.Max(0.1, Math.Min(0.8, ratio));
                        ViewModel.RunSplitRatio = 1 - ViewModel.TreeSplitRatio;
                    }
                }
            } catch (Exception) {
                // 加载失败使用默认值
                ViewModel.TreeSplitRatio = 0.25;
                ViewModel.RunSplitRatio = 0.75;
            }
            // 按保存格式缓存当前值（关闭时PreCloseSaveData统一比对落盘）
            Configs.TreeSplitRatio = ViewModel.TreeSplitRatio.ToString("F3");
        }

        /// <summary>
        /// 运行Tab表头行数（[set] run_tab_rows：1单行/2双行/0不限）→ Configs缓存 + ViewModel选中项
        /// </summary>
        private void LoadRunTabRows(IniData iniData) {
            var rowStr = iniData[Configs.IniSectionSet][Configs.IniKeySetRunTabRows];
            if (int.TryParse(rowStr, out var rows) && rows >= 0) {
                Configs.RunTabRows = rows;
            }
            ViewModel.SelectedRunTabRows = Configs.RunTabRows.ToString();
        }

        /// <summary>
        /// 设置页各输入项加载（[set]心跳/预警/JDK/配置/日志/超时/频次/换行/关闭方式/删除计数，
        /// 对应老项目InitEnv的UI赋值部分）：值进ViewModel属性与Configs缓存（保存时比对用），
        /// 并刷新JavaAppUtils静态值（bat脚本生成使用）
        /// </summary>
        private void LoadEnvSettings(IniData iniData) {
            var vm = ViewModel;
            var section = iniData[Configs.IniSectionSet];

            vm.HeartBeatMsg = section[Configs.IniKeySetHeartBeat];
            vm.IsWarnEnabled = bool.TrueString.Equals(section[Configs.IniKeySetIsWarn]);
            vm.WarnUrl = section[Configs.IniKeySetWarnTo];
            vm.ServerName = section[Configs.IniKeySetServerName];
            vm.JdkPath = section[Configs.IniKeySetJdkpath];
            vm.SelectedProfile = section[Configs.IniKeySetProfiles];
            vm.LogPath = section[Configs.IniKeySetLogpath];
            vm.MonitorTimeoutText = section[Configs.IniKeySetTimeout];
            vm.MonitorIntervalText = section[Configs.IniKeySetInterval];
            if (string.IsNullOrEmpty(vm.MonitorTimeoutText)) vm.MonitorTimeoutText = "15";
            if (string.IsNullOrEmpty(vm.MonitorIntervalText)) vm.MonitorIntervalText = "5";

            // 系统级日志换行：只认auto/none，缺省与非法值按默认不换行处理
            var logWrap = section[Configs.IniKeySetLogWrap];
            if (!Configs.LogWrapAuto.Equals(logWrap) && !Configs.LogWrapNone.Equals(logWrap)) {
                logWrap = Configs.LogWrapNone;
            }
            vm.SelectedLogWrap = logWrap;

            // 各项加载到Configs缓存（SaveSettings保存前比对，值变化才写盘）
            Configs.HeartBeat = vm.HeartBeatMsg ?? string.Empty;
            Configs.IsWarn = vm.IsWarnEnabled;
            Configs.WarnAddress = vm.WarnUrl ?? string.Empty;
            Configs.ServerName = vm.ServerName ?? string.Empty;
            Configs.JdkPath = vm.JdkPath ?? string.Empty;
            Configs.Profiles = vm.SelectedProfile ?? string.Empty;
            Configs.LogPath = vm.LogPath ?? string.Empty;
            Configs.TimeoutText = vm.MonitorTimeoutText ?? string.Empty;
            Configs.IntervalText = vm.MonitorIntervalText ?? string.Empty;
            Configs.LogWrap = logWrap;

            // 数据库删除计数（达到阈值后执行VACUUM）：PriDelCount存启动初始值，
            // 主窗口关闭时与DelCount比对，不同才写盘
            Configs.PriDelCount = int.TryParse(section[Configs.IniKeySetDelCount], out var delCount) ? delCount : 0;
            Configs.DelCount = Configs.PriDelCount;

            // 界面字体/颜色等软件设置：ThemeUtils.Init启动时已加载到Configs并应用，同步到设置页显示
            vm.SyncSoftwareSettingsToView();

            var closedType = section[Configs.IniKeySetClosedType];
            if (string.IsNullOrEmpty(closedType)) {
                // 缺省按退出处理（不写盘，基线即默认值；页面值与基线一致时关闭也不产生写入）
                closedType = Configs.ClosedTypeExit;
            }
            Configs.ClosedType = closedType;
            vm.IsClosedExit = Configs.ClosedTypeExit.Equals(closedType);
            vm.IsClosedMin = Configs.ClosedTypeMin.Equals(closedType);

            // JDK/配置/日志静态值刷新（bat脚本生成使用）
            AMUtils.JavaAppUtils.LoadSettings();
        }

        #endregion

        #region 托盘图标（对应老项目AppManagerNotifyIcon相关事件）

        private void InitNotifyIcon() {
            _notifyIcon = new System.Windows.Forms.NotifyIcon {
                Text = "应用管理",
                Icon = LoadNotifyIcon(),
                Visible = true
            };
            var trayMenu = new System.Windows.Forms.ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, (s, e) => ShowMainWindow());
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            trayMenu.Items.Add("退出", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = trayMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private static System.Drawing.Icon LoadNotifyIcon() {
            try {
                // 老项目AppManagerNotifyIcon.Icon同款（从Form1.resx提取）
                var resource = Application.GetResourceStream(new Uri("pack://application:,,,/AppManager2;component/Resources/icon/Window/notify.ico"));
                if (resource != null) {
                    return new System.Drawing.Icon(resource.Stream);
                }
            } catch { }
            return System.Drawing.SystemIcons.Application;
        }

        /// <summary>
        /// 显示主窗口（供发送记录窗口“重发”跳转使用，对应老项目FormShow）
        /// </summary>
        public void ShowMainWindow() {
            Show();
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        /// <summary>
        /// 切换主Tab（0应用管理/1设置/2工具/3帮助），供发送记录窗口“重发”跳转使用
        /// </summary>
        public void SelectMainTab(int index) {
            MainTabControl.SelectedIndex = index;
        }

        private bool _helpLoaded;

        /// <summary>
        /// 帮助页加载doc目录下的help.rtf（对应老项目Form1_Load的HelpRichTextBox.LoadFile），只加载一次
        /// </summary>
        private void HelpTab_Loaded(object sender, RoutedEventArgs e) {
            if (_helpLoaded) return;
            _helpLoaded = true;

            try {
                var helpFile = Path.Combine(Configs.AppPath, "doc", "help.rtf");
                if (!File.Exists(helpFile)) {
                    SetHelpText($"帮助文档不存在：{helpFile}");
                    return;
                }
                var range = new TextRange(HelpRichTextBox.Document.ContentStart, HelpRichTextBox.Document.ContentEnd);
                using var fs = new FileStream(helpFile, FileMode.Open, FileAccess.Read);
                range.Load(fs, DataFormats.Rtf);
            } catch (Exception ex) {
                SetHelpText($"帮助文档加载失败：{ex.Message}");
            }
        }

        private void SetHelpText(string message) {
            HelpRichTextBox.Document = new FlowDocument(new Paragraph(new Run(message)));
        }

        /// <summary>
        /// 托盘退出：直达关闭（不经过关闭方式判断，对应老项目FormClose）
        /// </summary>
        private void ExitApplication() {
            _reallyClose = true;
            Close();
        }

        protected override void OnClosed(EventArgs e) {
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            base.OnClosed(e);
        }

        #endregion

        /// <summary>
        /// 关闭按钮点击后，退出还是最小化（对应老项目Form1_Closing）：
        /// 保存的关闭方式为退出时，检查未保存的最小化选项与环境配置修改；
        /// 为最小化时，最小化并保留任务栏图标（托盘图标同样可恢复/退出）
        /// </summary>
        protected override void OnClosing(CancelEventArgs e) {
            
            if (!_reallyClose) {
                if (Configs.ClosedTypeMin.Equals(Configs.ClosedType)) {
                    // 最小化：用最小化代替Hide（Hide会连任务栏按钮一起移除），任务栏图标保留
                    WindowState = WindowState.Minimized;
                    e.Cancel = true;
                    // 托盘气泡提示已最小化（超时时间Win10起由系统Toast决定，传值仅作兼容）
                    _notifyIcon?.ShowBalloonTip(3000, "应用管理", "应用管理已最小化", System.Windows.Forms.ToolTipIcon.Info);
                    return;
                } else {
                    var vm = ViewModel;
                    if (vm != null) {
                        if (vm.IsClosedMin) {
                            // 该选项未保存
                            MessageBox.Show("关闭最小化设置未保存！", "提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                            e.Cancel = true;
                            return;
                        }
                        if (vm.HasUnsavedEnvChanges()) {
                            var result = MessageBox.Show("存在未应用的参数,是否应用？", "警告",
                                MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Cancel) {
                                // 不退出
                                e.Cancel = true;
                                return;
                            }
                            if (result == MessageBoxResult.Yes) {
                                // 应用后退出（落盘统一在PreCloseSaveData）
                                vm.ApplySettings();
                            }
                        }
                    }
                }
            }
            PreCloseSaveData();
            base.OnClosing(e);
        }

        /// <summary>
        /// 关闭时统一保存界面状态到System.ini（单次写盘，合并自原SaveWindowGeometry/SaveMainTabIndex/
        /// SaveDelCount/SaveSendFormConfig与VM的SaveTreeExpandState/SaveSplitRatio/运行Tab行数即时写）：
        /// 各项与Configs/启动初值缓存比对，变化才写入对应键，全无变化不写盘。
        /// VM侧只保留运行值（拖分栏/切Tab行数即时更新界面），落盘统一收敛到这里
        /// </summary>
        private void PreCloseSaveData() {
            var iniData = new IniData();

            // —— 窗口几何与最大化标记（[system]）：最小化按正常状态记录；非正常态取RestoreBounds（还原后的正常态尺寸）——
            var isMaximized = WindowState == WindowState.Maximized;
            if (Configs.WindowMaximized != isMaximized) {
                // maximized沿用"1"/"0"格式手工比对保存（加载端按"1"判断，不走ConfigIniData的True/False格式）
                iniData[Configs.IniSectionSystem][Configs.IniKeyFormMaximized] = isMaximized ? "1" : "0";
                Configs.WindowMaximized = isMaximized;
            }
            double x, y, w, h;
            if (WindowState == WindowState.Normal) {
                x = Left; y = Top; w = Width; h = Height;
            } else {
                var bounds = RestoreBounds;
                x = bounds.X; y = bounds.Y; w = bounds.Width; h = bounds.Height;
            }
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormHeight, ref Configs.Height, Convert.ToInt32(h));
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormWidth, ref Configs.Width, Convert.ToInt32(w));
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormLocationX, ref Configs.Left, Convert.ToInt32(x));
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormLocationY, ref Configs.Top, Convert.ToInt32(y));

            // —— 主Tab索引（[app] main_tab_index）——
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionApp, Configs.IniKeyAppMainTabIndex, ref Configs.SelectedIndex, MainTabControl.SelectedIndex);

            // —— 分栏比例（[app] split_ratio）：拖动只更新VM值，此处统一落盘 ——
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionApp, Configs.IniKeyAppSplitRatio, ref Configs.TreeSplitRatio, ViewModel.TreeSplitRatio.ToString("F3"));

            // —— 项目树收缩组名单（[app] tree_collapsed）——
            try {
                var collapsedNames = new System.Collections.Generic.List<string>();
                foreach (var node in ViewModel.AppTree) {
                    if (Configs.NodeFlagGroup.Equals(node.Flag) && !node.IsExpanded && !string.IsNullOrEmpty(node.GroupName)) {
                        collapsedNames.Add(node.GroupName);
                    }
                }
                IniParserUtils.ConfigIniData(iniData, Configs.IniSectionApp, Configs.IniKeyAppTreeCollapsed, ref Configs.TreeCollapsed, string.Join("|", collapsedNames));
            } catch (Exception) {
                // 收集失败不影响其余项保存
            }

            // —— 发送记录窗口布局（[send]）：Configs运行值与启动加载初值比对 ——
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormHeight, ref _iniSendHeight, Configs.SendFormHeight);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormWidth, ref _iniSendWidth, Configs.SendFormWidth);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormLocationX, ref _iniSendLeft, Configs.SendFormLeft);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormLocationY, ref _iniSendTop, Configs.SendFormTop);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeySendSplitDis, ref _iniSendSplitDis, Configs.SendSplitDis);

            // —— 数据库删除计数（[set] DelCount）：与启动初值PriDelCount比对，无删除不写 ——
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSet, Configs.IniKeySetDelCount, ref Configs.PriDelCount, Configs.DelCount);

            // —— 设置页各项（[set]）：页面/VM当前值 与Configs启动基线比对（Configs全程不回写，
            //    “应用/恢复默认”只生效运行时不改基线），不同才写入；字号解析失败按基线（不产生写入）——
            var vm = ViewModel;
            SaveSetIfChanged(iniData, Configs.IniKeySetHeartBeat, vm.HeartBeatMsg ?? string.Empty, Configs.HeartBeat);
            SaveSetIfChanged(iniData, Configs.IniKeySetIsWarn, vm.IsWarnEnabled ? bool.TrueString : bool.FalseString, Configs.IsWarn ? bool.TrueString : bool.FalseString);
            SaveSetIfChanged(iniData, Configs.IniKeySetWarnTo, vm.WarnUrl ?? string.Empty, Configs.WarnAddress);
            SaveSetIfChanged(iniData, Configs.IniKeySetServerName, vm.ServerName ?? string.Empty, Configs.ServerName);
            SaveSetIfChanged(iniData, Configs.IniKeySetJdkpath, vm.JdkPath ?? string.Empty, Configs.JdkPath);
            SaveSetIfChanged(iniData, Configs.IniKeySetProfiles, vm.SelectedProfile ?? string.Empty, Configs.Profiles);
            SaveSetIfChanged(iniData, Configs.IniKeySetLogpath, vm.LogPath ?? string.Empty, Configs.LogPath);
            SaveSetIfChanged(iniData, Configs.IniKeySetClosedType, vm.IsClosedExit ? Configs.ClosedTypeExit : Configs.ClosedTypeMin, Configs.ClosedType);
            SaveSetIfChanged(iniData, Configs.IniKeySetTimeout, vm.MonitorTimeoutText ?? string.Empty, Configs.TimeoutText);
            SaveSetIfChanged(iniData, Configs.IniKeySetInterval, vm.MonitorIntervalText ?? string.Empty, Configs.IntervalText);
            SaveSetIfChanged(iniData, Configs.IniKeySetLogWrap, vm.SelectedLogWrap ?? Configs.LogWrapNone, Configs.LogWrap);
            SaveSetIfChanged(iniData, Configs.IniKeySetRunTabRows, vm.RunTabRows.ToString(), Configs.RunTabRows.ToString());
            SaveSetIfChanged(iniData, Configs.IniKeySetFontFamily, vm.GlobalFontFamily ?? string.Empty, Configs.GlobalFontFamily);
            SaveSetIfChanged(iniData, Configs.IniKeySetFontSize,
                int.TryParse(vm.GlobalFontSizeText, out var gfs) ? gfs.ToString() : null, Configs.GlobalFontSize.ToString());
            SaveSetIfChanged(iniData, Configs.IniKeySetFontColor, vm.GlobalFontColor ?? string.Empty, Configs.GlobalFontColor);
            SaveSetIfChanged(iniData, Configs.IniKeySetBtnFontFamily, vm.BtnFontFamily ?? string.Empty, Configs.BtnFontFamily);
            SaveSetIfChanged(iniData, Configs.IniKeySetBtnFontSize,
                int.TryParse(vm.BtnFontSizeText, out var bfs) ? bfs.ToString() : null, Configs.BtnFontSize.ToString());
            SaveSetIfChanged(iniData, Configs.IniKeySetTreeFontFamily, vm.TreeFontFamily ?? string.Empty, Configs.TreeFontFamily);
            SaveSetIfChanged(iniData, Configs.IniKeySetTreeFontSize,
                int.TryParse(vm.TreeFontSizeText, out var tfs) ? tfs.ToString() : null, Configs.TreeFontSize.ToString());
            SaveSetIfChanged(iniData, Configs.IniKeySetTreeFontColor, vm.TreeFontColor ?? string.Empty, Configs.TreeFontColor);
            SaveSetIfChanged(iniData, Configs.IniKeySetLogFontColor, vm.LogFontColor ?? string.Empty, Configs.LogFontColor);
            SaveSetIfChanged(iniData, Configs.IniKeySetLogBgColor, vm.LogBgColor ?? string.Empty, Configs.LogBgColor);
            SaveSetIfChanged(iniData, Configs.IniKeySetLogFontSize,
                int.TryParse(vm.LogFontSizeText, out var lfs) ? Math.Max(8, Math.Min(40, lfs)).ToString() : null, Configs.LogFontSize.ToString());
            SaveSetIfChanged(iniData, Configs.IniKeySetLogFontFamily, vm.LogFontFamily ?? string.Empty, Configs.LogFontFamily);

            IniParserUtils.SaveIniData(SystemIniPath, iniData);
        }

        /// <summary>
        /// [set]键差量写入：当前值（页面/VM）与Configs启动基线不同才写入（null当前值视为与基线相同）
        /// </summary>
        private static void SaveSetIfChanged(IniData iniData, string key, string current, string baseline) {
            if (current == null) return;
            if (!string.Equals(current, baseline ?? string.Empty)) {
                iniData[Configs.IniSectionSet][key] = current;
            }
        }

        private void RegisterEvents() {
            if (ViewModel != null) {
                ViewModel.ExpandAllRequested += () => ExpandAllTreeViewItems();
                ViewModel.CollapseAllRequested += () => CollapseAllTreeViewItems();
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (ViewModel == null) return;
            if (e.NewValue is TreeItemModel item) {
                ViewModel.SelectedApp = item;
                // 选中项目时状态栏提示项目名：组内项目带组名前缀显示为“组名/项目名”，紫色
                var displayName = "";

                if (item.IsGroupMember && !string.IsNullOrEmpty(item.GroupName)) {
                    // 有组的项目才显示组名
                    displayName += item.GroupName;
                }

                if (!string.IsNullOrEmpty(displayName)) {
                    displayName += "/";
                }
                displayName += item.Name;

                ViewModel.ShowMessage("已选中：" + displayName, Brushes.Purple);
            } else if (e.NewValue == null) {
                // 空白处取消选中后同步清除VM选中节点，联动刷新工具栏命令可用状态
                ViewModel.SelectedApp = null;
            }
        }

        private ScrollViewer _treeScrollViewer;

        /// <summary>
        /// 拦截节点选中/聚焦时的自动BringIntoView：长项目名的行比可视区宽，WPF默认会把水平滚动条拉到最右。
        /// 改为垂直方向不可见时仅滚动垂直，水平方向始终保持不动（键盘方向键导航的垂直跟随不受影响）。
        /// 必须挂在TreeViewItem容器上：事件沿可视树冒泡，先于TreeView模板内部的ScrollViewer收到
        /// </summary>
        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e) {
            if (!(sender is TreeViewItem item)) return;
            _treeScrollViewer ??= FindVisualChild<ScrollViewer>(AppTreeView);
            if (_treeScrollViewer == null) return;

            e.Handled = true; // 阻止默认的整矩形滚入（含水平滚动）
            try {
                var y = item.TransformToAncestor(_treeScrollViewer).Transform(new Point(0, 0)).Y;
                if (y < 0) {
                    _treeScrollViewer.ScrollToVerticalOffset(_treeScrollViewer.VerticalOffset + y);
                } else if (y + item.ActualHeight > _treeScrollViewer.ViewportHeight) {
                    _treeScrollViewer.ScrollToVerticalOffset(_treeScrollViewer.VerticalOffset + y + item.ActualHeight - _treeScrollViewer.ViewportHeight);
                }
            } catch (InvalidOperationException) {
                // 节点不在可视树（如收缩组内部）时坐标变换不可用，忽略
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ViewModel != null && sender is TabControl tabControl) {
                ViewModel.SelectedTab = tabControl.SelectedItem as AppRunTabItemModel;
            }
        }

        /// <summary>
        /// 主Tab切换后不把键盘焦点给新页面里的第一个输入框（如设置页的心跳字符）：
        /// TabControl切换内容时会自动聚焦首个可聚焦控件，这里在切换完成后把焦点收回到TabControl自身。
        /// SelectionChanged是冒泡事件，需按OriginalSource过滤，只处理主TabControl自身的切换
        /// </summary>
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!ReferenceEquals(e.OriginalSource, MainTabControl)) return;
            Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(MainTabControl)));
        }

        /// <summary>
        /// 单击应用树节点时，选中对应的运行Tab（对应老项目AppTreeView_NodeMouseClick）
        /// </summary>
        private void AppTreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (ViewModel == null) return;
            var treeViewItem = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem?.DataContext is TreeItemModel node
                && node.Flag == Configs.NodeFlagChild
                && !string.IsNullOrEmpty(node.Section)) {
                ViewModel.SelectRunTab(node.Section);
            }
        }

        #region 项目树拖拽排序（对应老项目AppTreeView_ItemDrag/DragEnter/DragDrop + SortApp）

        // 按下起点，移动超过阈值才发起拖拽（避免和普通单击冲突）
        private Point _treeDragStart;
        private bool _treeDragPending;

        private void AppTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var onNode = e.OriginalSource is DependencyObject src
                && FindAncestor<TreeViewItem>(src) != null;
            _treeDragPending = onNode;
            _treeDragStart = e.GetPosition(AppTreeView);
            if (onNode || FindAncestor<System.Windows.Controls.Primitives.ScrollBar>(e.OriginalSource as DependencyObject) != null) return;
            // 点击树内空白区域（节点行以外、滚动条除外）取消选中：
            // WPF点击空白处不会把键盘焦点从节点移到树上（失焦事件不触发），需手动清除选中并把焦点移到树
            AppTreeView.Focus();
            var selected = FindSelectedTreeViewItem(AppTreeView);
            if (selected != null) {
                selected.IsSelected = false;
            }
        }

        private void AppTreeView_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (!_treeDragPending || e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(AppTreeView);
            if (Math.Abs(pos.X - _treeDragStart.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(pos.Y - _treeDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            _treeDragPending = false;
            var treeViewItem = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem?.DataContext is TreeItemModel node) {
                DragDrop.DoDragDrop(AppTreeView, node, DragDropEffects.Move);
            }
        }

        private void AppTreeView_DragOver(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(typeof(TreeItemModel)) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void AppTreeView_Drop(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.GetDataPresent(typeof(TreeItemModel))) return;
            var vm = ViewModel;
            var moveNode = (TreeItemModel)e.Data.GetData(typeof(TreeItemModel));
            if (vm == null || moveNode == null) return;

            var isGroupMove = Configs.NodeFlagGroup.Equals(moveNode.Flag);
            var targetItem = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);

            // 拖动节点当前所在集合
            var oldParent = FindNodeCollection(vm.AppTree, moveNode);

            System.Collections.ObjectModel.ObservableCollection<TreeItemModel> targetParent;
            int index;
            string group;

            if (!(targetItem?.DataContext is TreeItemModel targetNode)) {
                // 拖到树空白处：放到根级末尾（子节点清组）
                targetParent = vm.AppTree;
                index = vm.AppTree.Count;
                group = string.Empty;
            } else {
                if (ReferenceEquals(targetNode, moveNode)) return;
                GetDropPosition(targetItem, e.GetPosition(targetItem), out var isUp, out var atTop, out var atMiddle);

                if (Configs.NodeFlagGroup.Equals(targetNode.Flag) && !isGroupMove) {
                    // 子节点拖到组节点：上1/3放组前、中间入组内末尾、下1/3放组后
                    if (oldParent == targetNode.Children && atMiddle) {
                        vm.ShowMessage("无效拖拽", Brushes.Orange);
                        return;
                    }
                    if (atTop) {
                        targetParent = vm.AppTree;
                        index = vm.AppTree.IndexOf(targetNode);
                        group = string.Empty;
                    } else if (atMiddle) {
                        targetParent = targetNode.Children;
                        index = targetNode.Children.Count;
                        group = targetNode.GroupName;
                        // 拖入收缩组时展开该组，保证移入节点可见且容器已生成（与新增项目进组的行为一致）
                        targetNode.IsExpanded = true;
                    } else {
                        targetParent = vm.AppTree;
                        index = vm.AppTree.IndexOf(targetNode) + 1;
                        group = string.Empty;
                    }
                } else {
                    if (isGroupMove && Configs.NodeFlagChild.Equals(targetNode.Flag)) {
                        vm.ShowMessage("无效拖拽，只允许一层目录", Brushes.Orange);
                        return;
                    }
                    // 拖到项目节点：与目标同级（目标在组内则入该组），上半区插目标前、下半区插目标后
                    var targetGroupNode = FindNodeGroup(vm.AppTree, targetNode);
                    if (targetGroupNode != null) {
                        targetParent = targetGroupNode.Children;
                        group = targetGroupNode.GroupName;
                    } else {
                        targetParent = vm.AppTree;
                        group = string.Empty;
                    }
                    index = IsUpIndex(targetParent, targetNode, isUp);
                }
            }

            // 从原位置移除（同集合内移动时修正插入索引）
            // WPF移除选中节点时会把选中重置到首个根节点：先清除选中避免跳选，插入后恢复选中拖拽节点
            var selectedContainer = FindSelectedTreeViewItem(AppTreeView);
            if (selectedContainer != null && ReferenceEquals(selectedContainer.DataContext, moveNode)) {
                selectedContainer.IsSelected = false;
            }
            if (oldParent != null) {
                var removedAt = oldParent.IndexOf(moveNode);
                if (removedAt >= 0) {
                    oldParent.RemoveAt(removedAt);
                    if (oldParent == targetParent && removedAt < index) {
                        index--;
                    }
                }
            }
            if (index < 0) index = 0;
            if (index > targetParent.Count) index = targetParent.Count;
            targetParent.Insert(index, moveNode);
            if (!isGroupMove) {
                moveNode.GroupName = group;
            }

            // 排序与分组写入数据库（Sort从1开始按树序重排）
            vm.SaveTreeOrder();

            // 恢复选中拖拽节点：连同键盘焦点一起还原（焦点若落在树空白处会触发取消选中）
            AppTreeView.UpdateLayout();
            var movedContainer = FindTreeViewItemForNode(AppTreeView, moveNode);
            if (movedContainer != null) {
                movedContainer.IsSelected = true;
                movedContainer.Focus();
            }
        }

        private static int IsUpIndex(System.Collections.ObjectModel.ObservableCollection<TreeItemModel> parent, TreeItemModel targetNode, bool isUp) {
            var targetIndex = parent.IndexOf(targetNode);
            return isUp ? targetIndex : targetIndex + 1;
        }

        /// <summary>
        /// 落点在目标行内的位置：组节点用三段（上1/3/中/下1/3），项目节点用上下两半
        /// （对应老项目按targetNode.Bounds计算locationStat/isUp）
        /// 注意：这里刻意用 out 参数而不是元组返回值——C# 元组依赖 System.ValueTuple，
        /// 该类型 .NET 4.7 才并入 mscorlib；程序若在只装 .NET 4.6 的服务器上运行，
        /// JIT 本方法时会抛 System.TypeLoadException 导致拖拽松手即崩溃。
        /// </summary>
        private static void GetDropPosition(TreeViewItem item, Point posInItem,
            out bool isUp, out bool atTop, out bool atMiddle) {
            // 用头部行高度计算（展开的组节点整高包含子级，不能用）
            double h, rel;
            if (item.Template?.FindName("PART_Header", item) is ContentPresenter header && header.ActualHeight > 0) {
                var origin = header.TranslatePoint(new Point(0, 0), item);
                h = header.ActualHeight;
                rel = posInItem.Y - origin.Y;
            } else {
                h = item.ActualHeight;
                rel = posInItem.Y;
            }
            isUp = rel >= 0 && rel < h / 2;
            atTop = rel >= 0 && rel < h / 3;
            atMiddle = rel >= h / 3 && rel < h * 2 / 3;
        }

        /// <summary>查找节点所在的集合（根级返回根集合）</summary>
        private static System.Collections.ObjectModel.ObservableCollection<TreeItemModel> FindNodeCollection(
            System.Collections.ObjectModel.ObservableCollection<TreeItemModel> root, TreeItemModel node) {
            foreach (var n in root) {
                if (ReferenceEquals(n, node)) return root;
                if (Configs.NodeFlagGroup.Equals(n.Flag)) {
                    foreach (var c in n.Children) {
                        if (ReferenceEquals(c, node)) return n.Children;
                    }
                }
            }
            return null;
        }

        /// <summary>查找节点所在的组节点（根级返回null）</summary>
        private static TreeItemModel FindNodeGroup(
            System.Collections.ObjectModel.ObservableCollection<TreeItemModel> root, TreeItemModel node) {
            foreach (var n in root) {
                if (Configs.NodeFlagGroup.Equals(n.Flag)) {
                    foreach (var c in n.Children) {
                        if (ReferenceEquals(c, node)) return n;
                    }
                }
            }
            return null;
        }

        #endregion

        #region 运行Tab标题拖拽排序（仅调整当前会话显示顺序，不持久化；实现方式与项目树拖拽一致）

        // 按下起点，移动超过阈值才发起拖拽（避免和普通单击选中/关闭按钮冲突）
        private Point _runTabDragStart;

        private void RunTabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            _runTabDragStart = e.GetPosition(RunTabControl);
        }

        private void RunTabControl_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(RunTabControl);
            if (Math.Abs(pos.X - _runTabDragStart.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(pos.Y - _runTabDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            // 可视树找TabItem：Tab头才命中；选中Tab的内容(RichTextBox)挂在TabControl的内容呈现器下，
            // 不在TabItem子树内，内容区点击/拖选文本不会误触发拖拽
            var tabItem = FindAncestor<TabItem>(e.OriginalSource as DependencyObject);
            if (tabItem != null) {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
            }
        }

        private void RunTabControl_DragOver(object sender, DragEventArgs e) {
            e.Effects = FindAncestor<TabItem>(e.OriginalSource as DependencyObject) != null
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void RunTabControl_Drop(object sender, DragEventArgs e) {
            e.Handled = true;
            var targetItem = FindAncestor<TabItem>(e.OriginalSource as DependencyObject);
            var sourceItem = e.Data.GetData(typeof(TabItem)) as TabItem;
            var vm = ViewModel;
            if (targetItem == null || sourceItem == null || ReferenceEquals(sourceItem, targetItem) || vm == null) return;
            if (!(sourceItem.DataContext is AppRunTabItemModel sourceModel)
                || !(targetItem.DataContext is AppRunTabItemModel targetModel)) return;

            var tabs = vm.RunningApps;
            var oldIndex = tabs.IndexOf(sourceModel);
            var targetIndex = tabs.IndexOf(targetModel);
            if (oldIndex < 0 || targetIndex < 0) return;

            // 落点在目标Tab左半区插到其前、右半区插到其后（对应项目树拖拽的上下半区插入）
            var pos = e.GetPosition(targetItem);
            var insertIndex = pos.X < targetItem.ActualWidth / 2 ? targetIndex : targetIndex + 1;
            if (insertIndex == oldIndex || insertIndex == oldIndex + 1) return;

            tabs.Move(oldIndex, insertIndex > oldIndex ? insertIndex - 1 : insertIndex);
            // 拖拽的Tab保持选中（索引随移动修正，TwoWay绑定同步TabControl选中态）
            vm.SelectedTabIndex = tabs.IndexOf(sourceModel);
        }

        #endregion

        /// <summary>
        /// 项目树键盘焦点变化时取消选中：
        /// 焦点离开项目树（点击应用内其他控件），或焦点落在树的空白处（点击节点行以外区域，焦点移到TreeView自身），
        /// 均视为“项目树失焦”取消选中；焦点移入/切换于节点之间、或被右键菜单临时接管时保留选中
        /// </summary>
        private void AppTreeView_KeyboardFocusChanged(object sender, KeyboardFocusChangedEventArgs e) {
            // 新焦点仍是树内节点（节点间切换/移入）时保留选中
            if (FindAncestor<TreeViewItem>(e.NewFocus as DependencyObject) != null) return;
            // 右键菜单临时接管焦点（菜单命令仍以节点为操作对象），此时不取消选中
            if (e.NewFocus is ContextMenu || e.NewFocus is MenuItem) return;
            var selected = FindSelectedTreeViewItem(AppTreeView);
            if (selected != null) {
                selected.IsSelected = false;
            }
        }

        /// <summary>
        /// 递归查找当前处于选中状态的节点容器（TreeView.SelectedItem只读，需通过容器IsSelected清除选中）
        /// </summary>
        private static TreeViewItem FindSelectedTreeViewItem(ItemsControl itemsControl) {
            if (itemsControl == null) return null;
            foreach (var item in itemsControl.Items) {
                if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container) {
                    if (container.IsSelected) return container;
                    var childSelected = FindSelectedTreeViewItem(container);
                    if (childSelected != null) return childSelected;
                }
            }
            return null;
        }

        /// <summary>
        /// 递归查找节点数据对应的TreeViewItem容器（拖拽落定后恢复选中用；组节点需处于展开状态才有子容器）
        /// </summary>
        private static TreeViewItem FindTreeViewItemForNode(ItemsControl itemsControl, TreeItemModel node) {
            if (itemsControl == null) return null;
            foreach (var item in itemsControl.Items) {
                if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container) {
                    if (ReferenceEquals(item, node)) return container;
                    var childResult = FindTreeViewItemForNode(container, node);
                    if (childResult != null) return childResult;
                }
            }
            return null;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject {
            while (current != null) {
                if (current is T matched) {
                    return matched;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// 运行日志右键-复制（对应老项目RichTextBox右键的复制：复制选中的文本）
        /// </summary>
        private void RunLogCopy_Click(object sender, RoutedEventArgs e) {
            if (sender is FrameworkElement { Parent: ContextMenu menu }
                && menu.PlacementTarget is RichTextBox richTextBox) {
                if (string.IsNullOrEmpty(richTextBox.Selection.Text)) {
                    MessageBox.Show("复制内容为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    richTextBox.Copy();
                    ViewModel?.ShowMessage("复制成功", Brushes.Green);
                }
            }
        }

        /// <summary>
        /// 运行日志右键-清空
        /// </summary>
        private void RunLogClear_Click(object sender, RoutedEventArgs e) {
            if (sender is FrameworkElement { Parent: ContextMenu menu }
                && menu.PlacementTarget is RichTextBox richTextBox
                && richTextBox.DataContext is AppRunTabItemModel tabItem) {
                tabItem.ClearLog();
            }
        }

        /// <summary>
        /// 切换运行Tab时换挂对应项目的日志文档：
        /// TabControl复用同一棵元素树只换DataContext，RichTextBox的Document须随DataContext手动切换
        /// （换行模式由模型在自己文档上维护，这里只负责挂接和滚动）
        /// </summary>
        private void RunLogRichTextBox_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is RichTextBox richTextBox) {
                richTextBox.Document = e.NewValue is AppRunTabItemModel tabItem
                    ? tabItem.LogDocument
                    : new FlowDocument();
                richTextBox.ScrollToEnd();
            }
        }

        /// <summary>
        /// 日志滚动"粘底"跟随（对应老项目RichTextBox的ScrollToCaret，升级为仅贴底时跟随）：
        /// 用户往上翻阅历史日志时不自动滚到底，只有滚动条本就贴着底部才跟随新日志。
        /// 用内部ScrollViewer的ScrollChanged实现——TextChanged触发时内容已增长无法回推追加前的位置，
        /// 而ScrollChangedEventArgs带ExtentHeightChange（纵向内容变化量），可还原追加前的底界
        /// </summary>
        private void RunLogRichTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (!(e.OriginalSource is ScrollViewer scrollViewer) || e.ExtentHeightChange == 0) {
                return;
            }
            // 追加前的可视底边 = 当前Extent - 本次变化量；与追加前的偏移比较（留2像素容差抵浮点/亚像素）
            var bottomBefore = e.VerticalOffset + scrollViewer.ViewportHeight
                >= scrollViewer.ExtentHeight - e.ExtentHeightChange - 2;
            if (bottomBefore) {
                scrollViewer.ScrollToEnd();
            }
        }

        /// <summary>
        /// 运行日志右键-切换日志换行（系统/自动/不换行）：
        /// 子菜单项位于子菜单Popup中，其视觉树到PopupRoot即断链（GetParent为null），
        /// FindAncestor找不到ContextMenu，改用菜单项继承的DataContext（即日志TextBox的DataContext=运行Tab模型）；
        /// 换行应用由模型OnChanged更新自身LogDocument完成，这里只更新配置
        /// </summary>
        private void RunLogWrapMenu_Click(object sender, RoutedEventArgs e) {
            if (!(sender is FrameworkElement item) || !(item.Tag is string wrapMode)) return;
            if (item.DataContext is AppRunTabItemModel tabItem) {
                ViewModel?.SetRunTabLogWrap(tabItem, wrapMode);
            }
        }

        // Ctrl+滚轮字体缩放范围（步长1，超限保持不变且不提示；大小不持久化，重启恢复默认字号）
        private const double RunLogFontMinSize = 8;
        private const double RunLogFontMaxSize = 40;

        /// <summary>
        /// 运行日志Ctrl+滚轮调整字体大小（浏览器/编辑器通用习惯）：
        /// 按住Ctrl向上滚动放大、向下缩小；Ctrl按住时滚轮专用于缩放（不滚动日志），
        /// 超出上下限静默不变。字号写入当前Tab模型（每项目独立），
        /// 不直接设控件本地值——TabControl切换Tab复用元素树，本地值会串到其他项目
        /// </summary>
        private void RunLogTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            e.Handled = true;
            if (!(sender is RichTextBox { DataContext: AppRunTabItemModel tabItem })) return;

            var fontSize = tabItem.LogFontSize + (e.Delta > 0 ? 1 : -1);
            if (fontSize < RunLogFontMinSize || fontSize > RunLogFontMaxSize) return;
            tabItem.LogFontSize = fontSize;
        }

        /// <summary>
        /// 展开所有TreeView节点
        /// </summary>
        public void ExpandAllTreeViewItems() {
            ExpandTreeViewItems(AppTreeView);
        }

        /// <summary>
        /// 收缩所有TreeView节点
        /// </summary>
        public void CollapseAllTreeViewItems() {
            CollapseTreeViewItems(AppTreeView);
        }

        private void ExpandTreeViewItems(ItemsControl itemsControl) {
            if (itemsControl == null) return;

            foreach (var item in itemsControl.Items) {
                if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container) {
                    container.IsExpanded = true;
                    ExpandTreeViewItems(container);
                }
            }
        }

        private void CollapseTreeViewItems(ItemsControl itemsControl) {
            if (itemsControl == null) return;

            foreach (var item in itemsControl.Items) {
                if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container) {
                    container.IsExpanded = false;
                    CollapseTreeViewItems(container);
                }
            }
        }

        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e) {

        }
    }
}
