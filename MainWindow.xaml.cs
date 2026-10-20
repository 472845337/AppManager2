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
        private MainWindowViewModel ViewModel => DataContext as MainWindowViewModel;

        // 托盘图标（对应老项目AppManagerNotifyIcon）
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        // 托盘“退出”直达关闭，跳过关闭方式判断
        private bool _reallyClose;

        public MainWindow() {
            InitializeComponent();
            RegisterEvents();
            InitNotifyIcon();
            LoadWindowGeometry();
            LoadMainTabIndex();
            LoadSendFormConfig();
            InitAutoRunPanel();
        }

        /// <summary>
        /// 恢复上次关闭时选中的主Tab（System.ini [app] main_tab_index，0应用管理/1设置/2工具/3帮助）
        /// </summary>
        private void LoadMainTabIndex() {
            try {
                var iniData = IniParserUtils.GetIniData(SystemIniPath);
                var indexStr = iniData[Configs.IniSectionApp][Configs.IniKeyAppMainTabIndex];
                if (int.TryParse(indexStr, out var index) && index >= 0 && index < MainTabControl.Items.Count) {
                    MainTabControl.SelectedIndex = index;
                }
                Configs.SelectedIndex = MainTabControl.SelectedIndex;
            } catch { }
        }

        /// <summary>
        /// 保存当前选中的主Tab索引（关闭程序前调用）：
        /// 与Configs缓存比对，值变化才写入（Configs.SelectedIndex由LoadMainTabIndex加载）
        /// </summary>
        private void SaveMainTabIndex() {
            var iniData = new IniData();
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionApp, Configs.IniKeyAppMainTabIndex, ref Configs.SelectedIndex, MainTabControl.SelectedIndex);
            IniParserUtils.SaveIniData(SystemIniPath, iniData);
        }

        /// <summary>
        /// 保存数据库删除计数（关闭程序前调用）：
        /// 运行值与启动加载的初始值（PriDelCount）比对，不同才写入ini
        /// </summary>
        private void SaveDelCount() {
            if (Configs.PriDelCount == Configs.DelCount) {
                return;
            }
            IniParserUtils.SaveIniData(SystemIniPath, Configs.IniSectionSet, Configs.IniKeySetDelCount, Convert.ToString(Configs.DelCount));
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
        private void LoadWindowGeometry() {
            var iniData = IniParserUtils.GetIniData(SystemIniPath);
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

        /// <summary>
        /// 保存窗口位置、宽高和最大化状态到System.ini [system]（对应老项目ResizeEnd/FormClose保存）；
        /// 最大化时记录标记（下次启动恢复全屏）；最小化按正常状态处理；
        /// 非正常态下Left/Top/Width/Height是中间态，几何统一取RestoreBounds（还原后的正常态尺寸）
        /// </summary>
        private void SaveWindowGeometry() {
            // 最小化关闭：WindowState非Maximized，标记为正常状态
            // maximized沿用"1"/"0"格式手工比对保存（加载端按"1"判断，不走ConfigIniData的True/False格式）
            var isMaximized = WindowState == WindowState.Maximized;
            if (Configs.WindowMaximized != isMaximized) {
                IniParserUtils.SaveIniData(SystemIniPath, Configs.IniSectionSystem, Configs.IniKeyFormMaximized, isMaximized ? "1" : "0");
                Configs.WindowMaximized = isMaximized;
            }

            double x, y, w, h;
            if (WindowState == WindowState.Normal) {
                x = Left; y = Top; w = Width; h = Height;
            } else {
                var bounds = RestoreBounds;
                x = bounds.X; y = bounds.Y; w = bounds.Width; h = bounds.Height;
            }
            // 几何值与Configs缓存比对，只有值变化才写入IniData（参考XStart2.0的ConfigIniData方式）；
            // 无变化时IniData为空，SaveIniData不写盘
            var iniData = new IniData();
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormHeight, ref Configs.Height, Convert.ToInt32(h));
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormWidth, ref Configs.Width, Convert.ToInt32(w));
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormLocationX, ref Configs.Left, Convert.ToInt32(x));
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSystem, Configs.IniKeyFormLocationY, ref Configs.Top, Convert.ToInt32(y));
            IniParserUtils.SaveIniData(SystemIniPath, iniData);
        }

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
        private void LoadSendFormConfig() {
            var section = IniParserUtils.GetIniData(SystemIniPath)[Configs.IniSectionSend];
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

        /// <summary>
        /// 主窗口关闭时保存发送记录窗口布局（关闭程序前调用）：
        /// Configs运行值与启动加载的初始值比对，值变化才写入，无变化不写盘
        /// </summary>
        private void SaveSendFormConfig() {
            var iniData = new IniData();
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormHeight, ref _iniSendHeight, Configs.SendFormHeight);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormWidth, ref _iniSendWidth, Configs.SendFormWidth);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormLocationX, ref _iniSendLeft, Configs.SendFormLeft);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeyFormLocationY, ref _iniSendTop, Configs.SendFormTop);
            IniParserUtils.ConfigIniData(iniData, Configs.IniSectionSend, Configs.IniKeySendSplitDis, ref _iniSendSplitDis, Configs.SendSplitDis);
            IniParserUtils.SaveIniData(SystemIniPath, iniData);
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
                            var result = MessageBox.Show("存在未保存的参数,是否保存？", "警告",
                                MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Cancel) {
                                // 不退出
                                e.Cancel = true;
                                return;
                            }
                            if (result == MessageBoxResult.Yes) {
                                // 保存并退出
                                vm.SaveSettings();
                            }
                        }
                    }
                }
            }
            PreCloseSaveData();
            base.OnClosing(e);
        }

        private void PreCloseSaveData() {
            // 所有关闭路径（退出/托盘退出/最小化隐藏）都记录当前窗口位置和尺寸
            SaveWindowGeometry();
            // 记录项目树组的展开/收缩状态，下次启动恢复
            ViewModel?.SaveTreeExpandState();
            SaveMainTabIndex();
            // 数据库删除计数有变化时写盘（无删除则不写）
            SaveDelCount();
            // 发送记录窗口布局有变化时写盘（窗口未打开/未调整则不写）
            SaveSendFormConfig();
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
                var (isUp, atTop, atMiddle) = GetDropPosition(targetItem, e.GetPosition(targetItem));

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
        /// </summary>
        private static (bool isUp, bool atTop, bool atMiddle) GetDropPosition(TreeViewItem item, Point posInItem) {
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
            var isUp = rel >= 0 && rel < h / 2;
            var atTop = rel >= 0 && rel < h / 3;
            var atMiddle = rel >= h / 3 && rel < h * 2 / 3;
            return (isUp, atTop, atMiddle);
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
                && menu.PlacementTarget is TextBox textBox) {
                if (string.IsNullOrEmpty(textBox.SelectedText)) {
                    MessageBox.Show("复制内容为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    textBox.Copy();
                    ViewModel?.ShowMessage("复制成功", Brushes.Green);
                }
            }
        }

        /// <summary>
        /// 运行日志右键-清空
        /// </summary>
        private void RunLogClear_Click(object sender, RoutedEventArgs e) {
            if (sender is FrameworkElement { Parent: ContextMenu menu }
                && menu.PlacementTarget is TextBox textBox
                && textBox.DataContext is AppRunTabItemModel tabItem) {
                tabItem.ClearLog();
            }
        }

        /// <summary>
        /// 运行日志右键-切换日志换行（系统/自动/不换行）：
        /// 子菜单项位于子菜单Popup中，其视觉树到PopupRoot即断链（GetParent为null），
        /// FindAncestor找不到ContextMenu，改用菜单项继承的DataContext（即日志TextBox的DataContext=运行Tab模型）
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
            if (!(sender is TextBox { DataContext: AppRunTabItemModel tabItem })) return;

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
