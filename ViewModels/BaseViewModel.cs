using AppManager2.Commands;
using PropertyChanged;
using System.Windows;

namespace AppManager2.ViewModels {
    [AddINotifyPropertyChangedInterface]
    public class BaseViewModel {
        public int FontSize { get; set; } = 14;

        public RelayCommand CancelCommand { get; set; } = new RelayCommand(Cancel);

        public static void Cancel(object obj) {
            var window = (Window)obj;
            if (null != window) {
                window.DialogResult = false;
            }
        }
    }
}
