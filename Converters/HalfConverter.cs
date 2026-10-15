using System;
using System.Globalization;
using System.Windows.Data;

namespace AppManager2.Converters {
    public class HalfConverter : BaseValueConverter<HalfConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null)
                return 0d;

            double result;

            // 兼容 double / int / float / string 等多种输入
            if (value is double d)
                result = d;
            else if (value is int i)
                result = i;
            else if (value is float f)
                result = f;
            else if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out var parsed))
                result = parsed;
            else
                return 0d;

            result /= 2.0;

            // 根据目标类型返回（CornerRadius 是 double，这里直接返回 double）
            if (targetType == typeof(double) || targetType == typeof(object))
                return result;

            return System.Convert.ChangeType(result, targetType, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // CornerRadius -> Width 的场景很少用，如需可乘以 2
            if (value is double d)
                return d * 2.0;

            return Binding.DoNothing;
        }
    }
}
