using System;
using System.Globalization;

namespace AppManager2.Converters {
    /// <summary>
    /// bool取反转换器（如 x-www-form-urlencoded 单选框绑定 FormIsFormData 的反值）
    /// </summary>
    public class InverseBoolConverter : BaseValueConverter<InverseBoolConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool b) {
                return !b;
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool b) {
                return !b;
            }
            return false;
        }
    }
}
