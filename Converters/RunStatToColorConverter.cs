using System;
using System.Globalization;
using System.Windows.Media;

namespace AppManager2.Converters
{
    public class RunStatToColorConverter : BaseValueConverter<RunStatToColorConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is short stat)
            {
                return stat switch
                {
                    -1 => new SolidColorBrush(Color.FromRgb(217, 83, 79)),   // 失败 - 红色
                    0 => new SolidColorBrush(Color.FromRgb(255, 255, 255)),  // 未运行 - 白色
                    1 => new SolidColorBrush(Color.FromRgb(240, 173, 78)),   // 启动中 - 黄色
                    2 => new SolidColorBrush(Color.FromRgb(92, 184, 92)),    // 成功 - 绿色
                    3 => new SolidColorBrush(Color.FromRgb(240, 173, 78)),   // 停止中 - 黄色
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            return new SolidColorBrush(Colors.White);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
