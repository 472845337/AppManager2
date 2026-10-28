using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AppManager2.Controls {
    /// <summary>
    /// 支持溢出下拉的TabControl（参考IntelliJ IDEA的Tab设计）：
    /// 表头由OverflowTabPanel按MaxRows分行排布，放不下的Tab收进右侧下拉箭头，
    /// 点击下拉项选中对应Tab（面板保证选中项回到可见区）。
    /// 模板部件：HeaderPanel(OverflowTabPanel)/PART_OverflowButton/PART_OverflowPopup/PART_OverflowList
    /// </summary>
    [TemplatePart(Name = "HeaderPanel", Type = typeof(OverflowTabPanel))]
    [TemplatePart(Name = "PART_OverflowButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_OverflowPopup", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_OverflowList", Type = typeof(ListBox))]
    [TemplatePart(Name = "PART_TabMenuButton", Type = typeof(ButtonBase))]
    public class OverflowTabControl : TabControl {
        public static readonly DependencyProperty MaxRowsProperty = DependencyProperty.Register(
            "MaxRows", typeof(int), typeof(OverflowTabControl),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure, OnMaxRowsChanged));

        /// <summary>表头最大行数：1单行、2双行、0不限（模板传递给OverflowTabPanel）</summary>
        public int MaxRows {
            get => (int)GetValue(MaxRowsProperty);
            set => SetValue(MaxRowsProperty, value);
        }

        /// <summary>
        /// 模式切换时刷新箭头可见性（箭头显示只由模式决定，与布局测量时序无关：
        /// 多行(0)一定不显示；单行/双行一定显示，全部放得下时下拉里只是没有隐藏项）
        /// </summary>
        private static void OnMaxRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((OverflowTabControl)d).UpdateOverflowButtonVisibility();
        }

        private void UpdateOverflowButtonVisibility() {
            if (_overflowButton != null) {
                _overflowButton.Visibility = MaxRows > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private OverflowTabPanel _headerPanel;
        private ButtonBase _overflowButton;
        private Popup _overflowPopup;
        private ListBox _overflowList;
        private ButtonBase _tabMenuButton;
        // 事件处理器持有引用，模板重应用时先解绑防泄漏
        private EventHandler _onOverflowChanged;
        private RoutedEventHandler _onOverflowButtonClick;
        private SelectionChangedEventHandler _onOverflowListSelectionChanged;
        private RoutedEventHandler _onTabMenuButtonClick;

        // 无DefaultStyleKey覆盖：模板由使用处的隐式Style提供（MainWindow资源），
        // 避免老式工程引入Themes/Generic.xaml

        public OverflowTabControl() {
            // 下拉列表内容兜底刷新时机：面板溢出靠测量事件推送，启动期多层布局叠加时
            // 可能错过最后一轮事件（箭头可见性已改为只随模式变化，不受此影响）
            Loaded += (s, e) => SyncOverflowUI();
            SizeChanged += (s, e) => SyncOverflowUI();
            ItemContainerGenerator.ItemsChanged += (s, e) => SyncOverflowUI();
        }

        /// <summary>
        /// 强制面板重新测量，并在本轮布局完成后按面板当前溢出集合刷新下拉列表内容
        /// </summary>
        private void SyncOverflowUI() {
            _headerPanel?.InvalidateMeasure();
            // 用InvokeAsync而非BeginInvoke(delegate[,priority])：后者绑定的是params object[]重载，
            // 零参调用会被Roslyn编译成Array.Empty<object>()（.NET 4.6新增），4.5.x服务器上抛MissingMethodException
            Dispatcher.InvokeAsync(RefreshOverflowUI, System.Windows.Threading.DispatcherPriority.Background);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            // 解绑旧模板部件的事件
            if (_headerPanel != null && _onOverflowChanged != null) _headerPanel.OverflowChanged -= _onOverflowChanged;
            if (_overflowButton != null && _onOverflowButtonClick != null) _overflowButton.Click -= _onOverflowButtonClick;
            if (_overflowList != null && _onOverflowListSelectionChanged != null) _overflowList.SelectionChanged -= _onOverflowListSelectionChanged;
            if (_tabMenuButton != null && _onTabMenuButtonClick != null) _tabMenuButton.Click -= _onTabMenuButtonClick;

            _headerPanel = GetTemplateChild("HeaderPanel") as OverflowTabPanel;
            _overflowButton = GetTemplateChild("PART_OverflowButton") as ButtonBase;
            _overflowPopup = GetTemplateChild("PART_OverflowPopup") as Popup;
            _overflowList = GetTemplateChild("PART_OverflowList") as ListBox;
            _tabMenuButton = GetTemplateChild("PART_TabMenuButton") as ButtonBase;

            if (_headerPanel != null) {
                _onOverflowChanged = (s, e) => RefreshOverflowUI();
                _headerPanel.OverflowChanged += _onOverflowChanged;
            }
            if (_overflowButton != null) {
                _onOverflowButtonClick = (s, e) => {
                    if (_overflowPopup != null) {
                        // 关闭时清掉高亮，下次点开不再残留上次的选中
                        if (_overflowPopup.IsOpen && _overflowList != null) {
                            _overflowList.SelectedItem = null;
                        }
                        // 打开前按面板当前溢出刷新一次列表（兜底启动期事件遗漏，列表内容始终新鲜）
                        RefreshOverflowUI();
                        _overflowPopup.IsOpen = !_overflowPopup.IsOpen;
                    }
                };
                _overflowButton.Click += _onOverflowButtonClick;
            }
            if (_overflowList != null) {
                _onOverflowListSelectionChanged = OnOverflowListSelectionChanged;
                _overflowList.SelectionChanged += _onOverflowListSelectionChanged;
            }
            if (_tabMenuButton != null) {
                // 选项卡选项按钮：点击弹出其ContextMenu（参考VS2022选项卡工具按钮）
                _onTabMenuButtonClick = (s, e) => {
                    var menu = _tabMenuButton.ContextMenu;
                    if (menu != null) {
                        menu.PlacementTarget = _tabMenuButton;
                        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                        menu.IsOpen = !menu.IsOpen;
                    }
                };
                _tabMenuButton.Click += _onTabMenuButtonClick;
            }
            // 箭头可见性只随模式（单/双行显示、多行不显示），模板应用后立即设定，不依赖测量
            UpdateOverflowButtonVisibility();
            RefreshOverflowUI();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
            base.OnSelectionChanged(e);
            // 选中变化重新布局：面板要把选中项换回可见区（隐藏的Tab被下拉选中后显示出来）
            _headerPanel?.InvalidateMeasure();
            if (_overflowPopup != null && _overflowPopup.IsOpen) {
                _overflowPopup.IsOpen = false;
            }
        }

        /// <summary>溢出集合变化：更新下拉列表内容（列表显示Tab的数据项）；箭头可见性与溢出无关</summary>
        private void RefreshOverflowUI() {
            if (_overflowList == null) return;
            _overflowList.Items.Clear();
            var hasOverflow = false;
            if (_headerPanel != null) {
                foreach (var element in _headerPanel.OverflowItems) {
                    if (element is TabItem tabItem && tabItem.DataContext != null) {
                        _overflowList.Items.Add(tabItem.DataContext);
                        hasOverflow = true;
                    }
                }
            }
            // 单/双行模式下箭头常显，全部放得下时下拉里没有隐藏项，给个禁用的占位提示（不可选中）
            if (!hasOverflow) {
                _overflowList.Items.Add(new ListBoxItem {
                    Content = "（无隐藏选项卡）",
                    IsEnabled = false,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
            }
        }

        private void OnOverflowListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 0) return;
            // 选中下拉项对应的Tab（SelectedItem变化触发OnSelectionChanged→面板把该项换回可见区）
            SelectedItem = e.AddedItems[0];
        }
    }
}
