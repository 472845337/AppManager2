using AppManager2.ViewModels;
using System.Windows;

namespace AppManager2.Views
{
    public partial class AppFormWindow : Window
    {
        private AppFormViewModel ViewModel => DataContext as AppFormViewModel;

        public AppFormWindow()
        {
            InitializeComponent();
            ViewModel.SetWindow(this);
        }

        public void SetOperateType(short operateType, string section = null)
        {

            ViewModel.SetOperateType(operateType, section);
        }
    }
}
