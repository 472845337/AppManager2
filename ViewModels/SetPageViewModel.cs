using AppManager2.Commands;
using PropertyChanged;
using System.Windows.Input;

namespace AppManager2.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class SetPageViewModel : BaseViewModel
    {
        public string JdkPath { get; set; }
        public string LogPath { get; set; }
        public string SelectedProfile { get; set; }
        public int MonitorInterval { get; set; } = 5000;
        public int MonitorTimeout { get; set; } = 15;
        public string ServerName { get; set; }
        public string WarnUrl { get; set; }
        public bool IsWarnEnabled { get; set; }

        public ICommand SaveSettingsCommand { get; set; }

        public SetPageViewModel()
        {
            SaveSettingsCommand = new RelayCommand(OnSaveSettings);
        }

        private void OnSaveSettings(object obj) { }
    }
}
