using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AppManager2.Converters
{
    /// <summary>
    /// 将比例值转换为GridLength的转换器
    /// 例如：0.25 转换为 "25*"，即25%的宽度
    /// </summary>
    public class SplitRatioToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double ratio)
            {
                // 将比例（0-1）转换为星号值（例如0.25转为25*）
                // 这样总宽度为1时，各列按比例分配
                return new GridLength(ratio * 1000, GridUnitType.Star);
            }
            return new GridLength(1, GridUnitType.Star); // 默认值
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength && gridLength.IsStar)
            {
                // 将星号值转换回比例
                return gridLength.Value / 1000.0;
            }
            return 0.25; // 默认25%
        }
    }
}
