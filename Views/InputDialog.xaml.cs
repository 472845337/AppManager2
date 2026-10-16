using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AppManager2.Views {
    /// <summary>
    /// 通用单行输入对话框：确定返回输入内容（可为空串），取消返回null
    /// </summary>
    public partial class InputDialog : Window {
        private InputDialog(string title, string prompt, string defaultValue) {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue ?? string.Empty;
            Loaded += (s, e) => {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        /// <summary>
        /// 模态显示输入对话框，Owner取当前激活窗口
        /// </summary>
        public static string Show(string title, string prompt, string defaultValue = null) {
            var dialog = new InputDialog(title, prompt, defaultValue) {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                        ?? Application.Current.MainWindow
            };
            return dialog.ShowDialog() == true ? dialog.InputTextBox.Text : null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        /// <summary>Enter确定、Esc取消</summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                DialogResult = true;
            } else if (e.Key == Key.Escape) {
                DialogResult = false;
            }
        }
    }
}
