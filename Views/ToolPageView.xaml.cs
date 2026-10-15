using AppManager2.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AppManager2.Views {
    /// <summary>
    /// 工具页（PostDog）：Code-behind只承担布局记忆和选项即时持久化两类View职责
    /// </summary>
    public partial class ToolPageView : UserControl {
        private ToolPageViewModel ViewModel => DataContext as ToolPageViewModel;

        public ToolPageView() {
            InitializeComponent();
        }

        /// <summary>
        /// 应用记忆的分隔百分比（对应老项目InitToolPage中SplitterDistance赋值）：
        /// HeadSplitPercent=Head占整列百分比；ParamSplitPercent=参数占下方区域（参数+响应）百分比。
        /// 百分比换算为星号长度，行高随父窗口等比缩放
        /// </summary>
        private void ToolPageView_Loaded(object sender, RoutedEventArgs e) {
            var vm = ViewModel;
            if (vm == null) return;
            var headPct = ClampPercent(vm.HeadSplitPercent);
            var paramPct = ClampPercent(vm.ParamSplitPercent);
            var lowerPct = 100 - headPct; // 下方区域（参数+响应）占整列
            HeadRow.Height = new GridLength(headPct, GridUnitType.Star);
            ParamRow.Height = new GridLength(lowerPct * paramPct / 100, GridUnitType.Star);
            ResponseRow.Height = new GridLength(lowerPct * (100 - paramPct) / 100, GridUnitType.Star);
        }

        /// <summary>
        /// 分隔器拖动结束：由实际高度反算百分比回写VM并持久化（对应老项目SplitterMoved）。
        /// 平铺结构下两个分隔器互不影响，任一分隔器变化都可以直接重算两个百分比
        /// </summary>
        private void Splitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            var vm = ViewModel;
            if (vm == null) return;
            var head = HeadRow.ActualHeight;
            var param = ParamRow.ActualHeight;
            var response = ResponseRow.ActualHeight;
            vm.HeadSplitPercent = ToPercent(head, param + response);
            vm.ParamSplitPercent = ToPercent(param, response);
            vm.SaveSplitHeights();
        }

        private static double ClampPercent(double value) {
            return Math.Max(ToolPageViewModel.SplitPercentMin, Math.Min(ToolPageViewModel.SplitPercentMax, value));
        }

        /// <summary>由上下两段实际高度计算上段占比，总高为0时保持原值不动</summary>
        private static double ToPercent(double top, double bottom) {
            return top + bottom <= 0 ? 0 : top / (top + bottom) * 100;
        }

        /// <summary>
        /// 超时选项变化即时持久化（对应老项目SendUrlTimeOutCheckbox/TextBox_ValueChanged）
        /// </summary>
        private void TimeoutSetting_Changed(object sender, RoutedEventArgs e) {
            ViewModel?.SaveTimeoutSetting();
        }

        /// <summary>
        /// 记录请求配置变化即时持久化（对应老项目SaveHttpRequestCheckbox_CheckedChanged）
        /// </summary>
        private void SaveHttpRequest_Changed(object sender, RoutedEventArgs e) {
            ViewModel?.SaveSaveHttpRequestSetting();
        }
    }
}
