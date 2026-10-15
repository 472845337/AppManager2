using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AppManager2.Converters {
    /// <summary>
    /// 根据应用类型和字段类型返回对应颜色的转换器
    /// 用法：ConverterParameter="字段类型"
    /// 字段类型: title(名称), path(路径), stop(终止), port(端口), heartbeat(心跳), jdk(JDK)
    ///
    /// 颜色规则：
    /// - 红色: 必填项
    /// - 橙色: 可选项但端口/心跳必填其一
    /// - 黑色: 可选项
    /// </summary>
    public class AppTypeToColorConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values == null || values.Length == 0 || values[0] == null)
                return new SolidColorBrush(Colors.Black);

            var appType = values[0].ToString();
            var fieldType = parameter?.ToString();

            return GetColor(appType, fieldType);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        private SolidColorBrush GetColor(string appType, string fieldType) {
            if (string.IsNullOrEmpty(fieldType))
                return new SolidColorBrush(Colors.Black);

            // 根据字段类型和应用类型返回颜色
            switch (fieldType) {
                case "title": // 名称 - 所有类型都必填
                case "path":  // 路径 - 所有类型都必填
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 红色 #DC3545

                case "stop": // 终止脚本
                    if (appType == "bat")
                        return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 红色
                    if (appType == "exe")
                        return new SolidColorBrush(Color.FromRgb(253, 126, 20)); // 橙色 #FD7E14
                    return new SolidColorBrush(Colors.Black);

                case "port": // 端口
                    if (appType == "java")
                        return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 红色
                    return new SolidColorBrush(Color.FromRgb(253, 126, 20)); // 橙色

                case "heartbeat": // 心跳
                    if (appType == "bat")
                        return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 红色
                    return new SolidColorBrush(Color.FromRgb(253, 126, 20)); // 橙色

                case "jdk":
                case "actuator":
                default:
                    return new SolidColorBrush(Colors.Black);
            }
        }
    }
}
