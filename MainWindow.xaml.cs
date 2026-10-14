using AppManager2.Config;
using AppManager2.Models;
using AppManager2.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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
        }

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
                var resource = Application.GetResourceStream(new Uri("pack://application:,,,/AppManager2;component/Resources/menu-icon/project.ico"));
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

        /// <summary>
        /// 最小化时隐藏到托盘（对应老项目Form1_SizeChanged）
        /// </summary>
        protected override void OnStateChanged(EventArgs e) {
            if (WindowState == WindowState.Minimized) {
                Hide();
                ShowInTaskbar = false;
            }
            base.OnStateChanged(e);
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
        /// 为最小化时，隐藏到托盘（通过托盘图标恢复或退出）
        /// </summary>
        protected override void OnClosing(CancelEventArgs e) {
            if (_reallyClose) {
                base.OnClosing(e);
                return;
            }

            var vm = ViewModel;
            if (vm != null) {
                if (Configs.ClosedTypeExit.Equals(Configs.ClosedType)) {
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
                } else {
                    if (vm.IsClosedExit) {
                        // 该选项未保存
                        MessageBox.Show("关闭退出设置未保存！", "提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                    } else {
                        // 最小化：隐藏到托盘
                        Hide();
                        ShowInTaskbar = false;
                    }
                    e.Cancel = true;
                    return;
                }
            }
            base.OnClosing(e);
        }

        private void RegisterEvents() {
            if (ViewModel != null) {
                ViewModel.ExpandAllRequested += () => ExpandAllTreeViewItems();
                ViewModel.CollapseAllRequested += () => CollapseAllTreeViewItems();
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (ViewModel != null && e.NewValue is TreeItemModel item) {
                ViewModel.SelectedApp = item;
            }
        }

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ViewModel != null && sender is TabControl tabControl) {
                ViewModel.SelectedTab = tabControl.SelectedItem as AppRunTabItemModel;
            }
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
            if ((sender as FrameworkElement)?.Parent is ContextMenu menu
                && menu.PlacementTarget is TextBox textBox) {
                if (string.IsNullOrEmpty(textBox.SelectedText)) {
                    MessageBox.Show("复制内容为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    textBox.Copy();
                    ViewModel?.ShowMessage("复制成功");
                }
            }
        }

        /// <summary>
        /// 运行日志右键-清空
        /// </summary>
        private void RunLogClear_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.Parent is ContextMenu menu
                && menu.PlacementTarget is TextBox textBox
                && textBox.DataContext is AppRunTabItemModel tabItem) {
                tabItem.ClearLog();
            }
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
                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container != null) {
                    container.IsExpanded = true;
                    ExpandTreeViewItems(container);
                }
            }
        }

        private void CollapseTreeViewItems(ItemsControl itemsControl) {
            if (itemsControl == null) return;

            foreach (var item in itemsControl.Items) {
                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container != null) {
                    container.IsExpanded = false;
                    CollapseTreeViewItems(container);
                }
            }
        }
    }
}
