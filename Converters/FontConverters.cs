using AppManager2.AMUtils;
using AppManager2.Config;
using System;
using System.Globalization;
using System.Windows.Media;

namespace AppManager2.Converters {
    /// <summary>
    /// 字体名字符串转FontFamily（空值回落默认字体）——软件设置的字体预览用
    /// </summary>
    public class StringToFontFamilyConverter : BaseValueConverter<StringToFontFamilyConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var name = value as string;
            return new FontFamily(string.IsNullOrEmpty(name) ? Configs.DefaultFontFamily : name);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null; // 单向转换
        }
    }

    /// <summary>
    /// 字号字符串转double（解析失败回落ConverterParameter指定值，缺省12）——软件设置的字体预览用
    /// </summary>
    public class StringToFontSizeConverter : BaseValueConverter<StringToFontSizeConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string s && double.TryParse(s, out var size) && size >= 8 && size <= 40) {
                return size;
            }
            return double.TryParse(parameter as string, out var defaultValue) ? defaultValue : 12.0;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null; // 单向转换
        }
    }

    /// <summary>
    /// 颜色名字符串转SolidColorBrush（非法值回落黑色）——软件设置的颜色预览用
    /// </summary>
    public class StringToBrushConverter : BaseValueConverter<StringToBrushConverter> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return ThemeUtils.BrushOf(value as string) ?? Brushes.Black;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null; // 单向转换
        }
    }
}
