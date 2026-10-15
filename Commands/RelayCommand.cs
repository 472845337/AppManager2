using System;
using System.Windows.Input;

namespace AppManager2.Commands {
    public class RelayCommand : ICommand {
        public event EventHandler CanExecuteChanged;
        private readonly Action<object> _action;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> action, Func<object, bool> canExecute = null) {
            _action = action;
            _canExecute = canExecute;
        }

        public void RaiseCanExecuteChanged() {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter) {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter) {
            _action?.Invoke(parameter);
        }
        public static void Raise<C>(params C[] cs) where C : RelayCommand {
            foreach (C c in cs) {
                c?.RaiseCanExecuteChanged();
            }
        }
    }
}
