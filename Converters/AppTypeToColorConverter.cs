using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AppManager2.AMUtils;
using AppManager2.Config;

namespace AppManager2.Converters {
    /// <summary>
    /// 根据应用类型和字段类型返回对应颜色的转换器
    /// 用法：ConverterParameter="字段类型"
    /// 字段类型: title(名称), path(路径), stop(终止), port(端口), heartbeat(心跳), jdk(JDK)
    ///
    /// 颜色规则：
    /// - 红色: 必填项
    /// - 橙色: 可选项但端口/心跳必填其一
    /// - 其他: 可选项，用系统配置的全局字色（AppFontColor资源）
    /// </summary>
    public class AppTypeToColorConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values == null || values.Length == 0 || values[0] == null)
                return SystemBrush();

            var appType = values[0].ToString();
            var fieldType = parameter?.ToString();

            return GetColor(appType, fieldType);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        private SolidColorBrush GetColor(string appType, string fieldType) {
            if (string.IsNullOrEmpty(fieldType))
                return SystemBrush();

            // 根据字段类型和应用类型返回颜色
            switch (fieldType) {
                case "title": // 名称 - 所有类型都必填
                case "path":  // 路径 - 所有类型都必填
                    return new SolidColorBrush(Colors.Red);

                case "stop": // 终止脚本
                    if (Configs.AppTypeBat.Equals(appType))
                        return new SolidColorBrush(Colors.Red); // 红色
                    return SystemBrush();

                case "port": // 端口
                    if (Configs.AppTypeJava.Equals(appType)) {
                        return new SolidColorBrush(Colors.Red); // 红色
                    } else if (Configs.AppTypeBat.Equals(appType)) {
                        return new SolidColorBrush(Colors.Orange); // 橙色
                    } else {
                        return SystemBrush();
                    }
                case "heartbeat": // 心跳：bat无端口监控必填（红），exe端口/心跳二选一（橙），
                    if (Configs.AppTypeBat.Equals(appType))
                        return new SolidColorBrush(Colors.Orange); // 红色
                    return SystemBrush();

                case "jdk":
                case "actuator":
                default:
                    return SystemBrush();
            }
        }

        /// <summary>系统配置的全局字色（设置-软件设置-界面字体-字色），未配置时回落黑色</summary>
        private static SolidColorBrush SystemBrush() {
            var brush = Application.Current?.TryFindResource(ThemeUtils.KeyAppFontColor) as SolidColorBrush;
            return brush ?? Brushes.Black;
        }
    }
}
