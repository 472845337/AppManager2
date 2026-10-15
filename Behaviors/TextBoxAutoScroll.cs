using System.Windows;
using System.Windows.Controls;

namespace AppManager2.Behaviors {
    /// <summary>
    /// TextBox自动滚动附加属性：日志内容变化时滚动到底部（对应老项目RichTextBox的ScrollToCaret）
    /// 用法：&lt;TextBox behaviors:TextBoxAutoScroll.AutoScroll="True"/&gt;
    /// </summary>
    public static class TextBoxAutoScroll {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(TextBoxAutoScroll),
                new PropertyMetadata(false, OnAutoScrollChanged));

        public static bool GetAutoScroll(DependencyObject obj) {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value) {
            obj.SetValue(AutoScrollProperty, value);
        }

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is TextBox textBox) {
                if ((bool)e.NewValue) {
                    textBox.TextChanged += (s, args) => ScrollToEnd(textBox);
                }
            }
        }

        private static void ScrollToEnd(TextBox textBox) {
            if (!textBox.IsLoaded) {
                return;
            }
            textBox.CaretIndex = textBox.Text.Length;
            textBox.ScrollToEnd();
        }
    }
}
