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
            ViewModel.SetWindow(this);
        }
    }
}
