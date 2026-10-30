using System.Windows;

namespace AppManager2.Converters {
    /// <summary>
    /// 绑定代理：ContextMenu/Popup不在可视树内，内部元素无法用RelativeSource找到祖先控件，
    /// 利用Freezable能继承声明处DataContext的特性，把外层DataContext（如容器VM）带进菜单：
    /// 声明 <converters:BindingProxy x:Key="proxy" Data="{Binding}"/>，
    /// 菜单内引用 {Binding Data.XXX, Source={StaticResource proxy}}
    /// </summary>
    public class BindingProxy : Freezable {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new PropertyMetadata(null));

        public object Data {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() {
            return new BindingProxy();
        }
    }
}
