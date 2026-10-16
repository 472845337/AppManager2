using System;
using System.Globalization;
using System.Windows.Data;

namespace AppManager2.Converters {
    /// <summary>
    /// 字符串相等判断：值与parameter相等返回true（用于菜单项勾选状态标记当前选项）
    /// </summary>
    public class StringEqualityConverter : BaseValueConverter<StringEqualityConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return string.Equals(System.Convert.ToString(value), System.Convert.ToString(parameter), StringComparison.OrdinalIgnoreCase);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // 只用于单向勾选展示，不支持反向转换
            return Binding.DoNothing;
        }
    }
}
