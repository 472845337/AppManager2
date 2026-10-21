using AppManager2.AMUtils;
using AppManager2.ViewModels;
using System.Windows;

namespace AppManager2.Views {
    /// <summary>
    /// 应用导出窗口（对应老项目 AppExportForm）
    /// </summary>
    public partial class AppExportWindow : Window {
        private AppExportViewModel ViewModel => DataContext as AppExportViewModel;

        public AppExportWindow() {
            InitializeComponent();
            ThemeUtils.ApplyToWindow(this); // 全局字体/字色（独立窗口不随主窗口资源更新，构造时按Configs设置）
            ViewModel.SetWindow(this);
        }
    }
}
