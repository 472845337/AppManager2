using System;
using System.Globalization;

namespace AppManager2.Converters {
    public class RunStatToTextConverter : BaseValueConverter<RunStatToTextConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is short stat) {
                return stat switch {
                    -1 => "启动失败",
                    0 => "未运行",
                    1 => "启动中...",
                    2 => "运行中",
                    3 => "停止中...",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
