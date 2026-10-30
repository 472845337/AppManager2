using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppManager2.ViewModels;

namespace AppManager2.Views {
    /// <summary>
    /// 多标签浏览器视图：容器（标签增删/选中/Tab头）与单页布局都经XAML绑定到
    /// WebBrowserPageViewModel / WebBrowserTabViewModel；Code-behind只剩地址栏的
    /// 聚焦与输入状态两个纯View细节
    /// </summary>
    public partial class WebBrowserPageView : UserControl {
        public WebBrowserPageView() {
            InitializeComponent();
        }

        /// <summary>新建且尚未访问过地址时聚焦地址栏，方便直接输入；切回已浏览的标签不打断</summary>
        private void AddressBox_Loaded(object sender, RoutedEventArgs e) {
            if (sender is TextBox addressBox && addressBox.DataContext is WebBrowserTabViewModel tab && !tab.HasNavigated) {
                addressBox.Focus();
            }
        }

        private void AddressBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            SetAddressEditing(sender, true);
        }

        private void AddressBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            SetAddressEditing(sender, false);
        }

        /// <summary>输入状态记到VM：站内跳转回写地址时避免打断正在输入的用户</summary>
        private static void SetAddressEditing(object sender, bool editing) {
            if (sender is TextBox addressBox && addressBox.DataContext is WebBrowserTabViewModel tab) {
                tab.AddressEditing = editing;
            }
        }

        /// <summary>左键点开收藏下拉（ContextMenu默认右键才弹出，这里由三角按钮主动打开）</summary>
        private void FavoriteButton_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.ContextMenu != null) {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
