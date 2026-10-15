using PropertyChanged;
using System;

namespace AppManager2.Models {
    /// <summary>
    /// 设置页自启动列表项（对应老项目AutoRunAppFlowLayoutPanel中的CheckBox）
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class AutoRunItemModel {
        public string Section { get; set; }
        public string Title { get; set; }

        /// <summary>勾选变化回调（由VM注入，负责联动项目AutoRun并入库）</summary>
        internal Action<AutoRunItemModel> CheckedChanged;

        [OnChangedMethod(nameof(OnIsCheckedChanged))]
        public bool IsChecked { get; set; }

        private void OnIsCheckedChanged() {
            CheckedChanged?.Invoke(this);
        }
    }
}
