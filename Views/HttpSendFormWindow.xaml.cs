using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Utils;

namespace AppManager2.Views
{
    /// <summary>
    /// 发送记录窗口：窗口尺寸/位置与左右分栏宽度持久化到System.ini [send]（对应老项目HttpSendForm_Load/ResizeEnd/SplitterMoved）
    /// </summary>
    public partial class HttpSendFormWindow : Window
    {
        public HttpSendFormWindow()
        {
            InitializeComponent();
            RestoreFromIni();
        }

        private static string IniPath => global::AppManager2.Config.Configs.AppPath + "\\" + global::AppManager2.Config.Configs.SystemIniPath;

        /// <summary>
        /// 加载记忆的窗口大小/位置与分栏宽度（对应老项目HttpSendForm_Load）。
        /// 必须在构造函数里、窗口句柄创建之前赋值：窗口按最终尺寸一次生成，
        /// 避免Show后（SourceInitialized阶段）再改尺寸导致首帧闪变、按钮重排
        /// </summary>
        private void RestoreFromIni()
        {
            var iniData = IniParserUtils.GetIniData(IniPath);
            var section = iniData[global::AppManager2.Config.Configs.IniSectionSend];

            var heightStr = section[global::AppManager2.Config.Configs.IniKeyFormHeight];
            var widthStr = section[global::AppManager2.Config.Configs.IniKeyFormWidth];
            var locationX = section[global::AppManager2.Config.Configs.IniKeyFormLocationX];
            var locationY = section[global::AppManager2.Config.Configs.IniKeyFormLocationY];

            if (!string.IsNullOrEmpty(heightStr) && double.TryParse(heightStr, out var h))
            {
                Height = h;
            }
            if (!string.IsNullOrEmpty(widthStr) && double.TryParse(widthStr, out var w))
            {
                Width = w;
            }
            if (!string.IsNullOrEmpty(locationX) && double.TryParse(locationX, out var x))
            {
                Left = x;
            }
            if (!string.IsNullOrEmpty(locationY) && double.TryParse(locationY, out var y))
            {
                Top = y;
            }

            var splitDis = section[global::AppManager2.Config.Configs.IniKeySendSplitDis];
            if (!string.IsNullOrEmpty(splitDis) && double.TryParse(splitDis, out var split))
            {
                ListColumn.Width = new GridLength(split);
            }
        }

        /// <summary>
        /// 分栏拖动结束保存宽度（对应老项目HttpToolSplitContainer_SplitterMoved）
        /// </summary>
        private void Splitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IniParserUtils.SaveIniData(IniPath, global::AppManager2.Config.Configs.IniSectionSend, global::AppManager2.Config.Configs.IniKeySendSplitDis,
                Convert.ToInt32(ListColumn.Width.Value).ToString());
        }

        /// <summary>
        /// 关闭时保存窗口大小/位置（对应老项目ResizeEnd/FormClosed）
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            var iniPath = IniPath;
            IniParserUtils.SaveIniData(iniPath, global::AppManager2.Config.Configs.IniSectionSend, global::AppManager2.Config.Configs.IniKeyFormHeight, Convert.ToInt32(ActualHeight).ToString());
            IniParserUtils.SaveIniData(iniPath, global::AppManager2.Config.Configs.IniSectionSend, global::AppManager2.Config.Configs.IniKeyFormWidth, Convert.ToInt32(ActualWidth).ToString());
            IniParserUtils.SaveIniData(iniPath, global::AppManager2.Config.Configs.IniSectionSend, global::AppManager2.Config.Configs.IniKeyFormLocationX, Convert.ToInt32(Left).ToString());
            IniParserUtils.SaveIniData(iniPath, global::AppManager2.Config.Configs.IniSectionSend, global::AppManager2.Config.Configs.IniKeyFormLocationY, Convert.ToInt32(Top).ToString());
            base.OnClosed(e);
        }

        /// <summary>
        /// 去重下拉菜单（对应老项目DeduplicateButton_Click弹出DeduplicateMenuStrip）
        /// </summary>
        private void DeduplicateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
