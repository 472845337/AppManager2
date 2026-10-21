using AppManager2.AMUtils;
using AppManager2.ViewModels;
using System.Windows;

namespace AppManager2.Views {
    public partial class AppFormWindow : Window {
        private AppFormViewModel ViewModel => DataContext as AppFormViewModel;

        public AppFormWindow() {
            InitializeComponent();
            ThemeUtils.ApplyToWindow(this); // 全局字体/字色（独立窗口不随主窗口资源更新，构造时按Configs设置）
            ViewModel.SetWindow(this);
        }

        public void SetOperateType(short operateType, string section = null) {

            ViewModel.SetOperateType(operateType, section);
        }
    }
}
