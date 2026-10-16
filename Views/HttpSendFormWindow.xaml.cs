using AppManager2.Config;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AppManager2.Views {
    /// <summary>
    /// 发送记录窗口：窗口尺寸/位置与左右分栏宽度只读写Configs（对应老项目HttpSendForm_Load/ResizeEnd/SplitterMoved）；
    /// ini的加载/保存统一在主窗口：程序启动时加载进Configs，主窗口关闭时比对写盘
    /// </summary>
    public partial class HttpSendFormWindow : Window {
        public HttpSendFormWindow() {
            InitializeComponent();
            RestoreFromConfigs();
        }

        /// <summary>
        /// 从Configs恢复记忆的窗口大小/位置与分栏宽度（对应老项目HttpSendForm_Load）。
        /// 必须在构造函数里、窗口句柄创建之前赋值：窗口按最终尺寸一次生成，
        /// 避免Show后（SourceInitialized阶段）再改尺寸导致首帧闪变、按钮重排
        /// </summary>
        private void RestoreFromConfigs() {
            // 首次使用（ini无记录，Configs为默认0）时不赋值，按XAML默认布局
            if (Configs.SendFormHeight > 0) Height = Configs.SendFormHeight;
            if (Configs.SendFormWidth > 0) Width = Configs.SendFormWidth;
            if (Configs.SendFormLeft != 0) Left = Configs.SendFormLeft;
            if (Configs.SendFormTop != 0) Top = Configs.SendFormTop;
            if (Configs.SendSplitDis > 0) ListColumn.Width = new GridLength(Configs.SendSplitDis);
        }

        /// <summary>
        /// 分栏拖动结束回写宽度到Configs（对应老项目HttpToolSplitContainer_SplitterMoved）；
        /// 不写ini，主窗口关闭时统一保存
        /// </summary>
        private void Splitter_DragCompleted(object sender, DragCompletedEventArgs e) {
            Configs.SendSplitDis = Convert.ToInt32(ListColumn.Width.Value);
        }

        /// <summary>
        /// 关闭时把窗口大小/位置回写Configs（对应老项目ResizeEnd/FormClosed）；
        /// 不写ini，主窗口关闭时统一保存
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Configs.SendFormHeight = Convert.ToInt32(ActualHeight);
            Configs.SendFormWidth = Convert.ToInt32(ActualWidth);
            Configs.SendFormLeft = Convert.ToInt32(Left);
            Configs.SendFormTop = Convert.ToInt32(Top);
            base.OnClosed(e);
        }

        /// <summary>
        /// 去重下拉菜单（对应老项目DeduplicateButton_Click弹出DeduplicateMenuStrip）
        /// </summary>
        private void DeduplicateButton_Click(object sender, RoutedEventArgs e) {
            if (sender is Button button && button.ContextMenu != null) {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
