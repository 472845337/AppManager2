using AppManager2.Config;
using System;
using System.Windows;
using System.Windows.Media;
using Utils;

namespace AppManager2.AMUtils {
    /// <summary>
    /// 界面字体/颜色主题：启动时从System.ini [set]加载到Configs并写入应用资源；
    /// 各控件（窗口/按钮样式/应用树/日志）用DynamicResource引用资源键，
    /// 设置页保存后调ApplyResources即时生效
    /// </summary>
    public static class ThemeUtils {
        // 应用资源键（App.xaml中有同名默认值）
        public const string KeyAppFontFamily = "AppFontFamily";
        public const string KeyAppFontSize = "AppFontSize";
        public const string KeyAppFontColor = "AppFontColor";
        public const string KeyBtnFontFamily = "BtnFontFamily";
        public const string KeyBtnFontSize = "BtnFontSize";
        public const string KeyTreeFontFamily = "TreeFontFamily";
        public const string KeyTreeFontSize = "TreeFontSize";
        public const string KeyTreeFontColor = "TreeFontColor";
        public const string KeyLogFontColor = "LogFontColor";
        public const string KeyLogBgColor = "LogBgColor";
        public const string KeyLogFontSize = "LogFontSize";
        public const string KeyLogFontFamily = "LogFontFamily";

        /// <summary>
        /// 启动时（主窗口创建前）加载字体/颜色配置到Configs并应用到资源；
        /// 非法/缺失值回落默认
        /// </summary>
        public static void Init() {
            var section = IniParserUtils.GetIniData(Configs.AppPath + "\\" + Configs.SystemIniPath)[Configs.IniSectionSet];
            Configs.GlobalFontFamily = TextOrDefault(section[Configs.IniKeySetFontFamily], Configs.DefaultFontFamily);
            Configs.GlobalFontSize = IntOrDefault(section[Configs.IniKeySetFontSize], Configs.GlobalFontSize);
            Configs.GlobalFontColor = ColorOrDefault(section[Configs.IniKeySetFontColor], Configs.GlobalFontColor);
            Configs.BtnFontFamily = TextOrDefault(section[Configs.IniKeySetBtnFontFamily], Configs.DefaultFontFamily);
            Configs.BtnFontSize = IntOrDefault(section[Configs.IniKeySetBtnFontSize], Configs.BtnFontSize);
            Configs.TreeFontFamily = TextOrDefault(section[Configs.IniKeySetTreeFontFamily], Configs.DefaultFontFamily);
            Configs.TreeFontSize = IntOrDefault(section[Configs.IniKeySetTreeFontSize], Configs.TreeFontSize);
            Configs.TreeFontColor = ColorOrDefault(section[Configs.IniKeySetTreeFontColor], Configs.TreeFontColor);
            Configs.LogFontColor = ColorOrDefault(section[Configs.IniKeySetLogFontColor], Configs.LogFontColor);
            Configs.LogBgColor = ColorOrDefault(section[Configs.IniKeySetLogBgColor], Configs.LogBgColor);
            Configs.LogFontSize = IntOrDefault(section[Configs.IniKeySetLogFontSize], Configs.LogFontSize);
            Configs.LogFontFamily = TextOrDefault(section[Configs.IniKeySetLogFontFamily], Configs.LogFontFamily);
            ApplyResources();
        }

        /// <summary>
        /// 把Configs中的字体/颜色写入应用资源，DynamicResource引用处即时更新
        /// </summary>
        public static void ApplyResources() {
            var res = Application.Current.Resources;
            res[KeyAppFontFamily] = new FontFamily(Configs.GlobalFontFamily);
            res[KeyAppFontSize] = (double)Configs.GlobalFontSize;
            res[KeyAppFontColor] = BrushOf(Configs.GlobalFontColor);
            res[KeyBtnFontFamily] = new FontFamily(Configs.BtnFontFamily);
            res[KeyBtnFontSize] = (double)Configs.BtnFontSize;
            res[KeyTreeFontFamily] = new FontFamily(Configs.TreeFontFamily);
            res[KeyTreeFontSize] = (double)Configs.TreeFontSize;
            res[KeyTreeFontColor] = BrushOf(Configs.TreeFontColor);
            res[KeyLogFontColor] = BrushOf(Configs.LogFontColor);
            res[KeyLogBgColor] = BrushOf(Configs.LogBgColor);
            res[KeyLogFontSize] = (double)Configs.LogFontSize;
            res[KeyLogFontFamily] = new FontFamily(Configs.LogFontFamily);
        }

        /// <summary>
        /// 独立弹窗窗口（应用编辑/导出/发送记录）应用全局字体：
        /// 不在主窗口可视树内，不随资源更新，构造时按Configs设置一次
        /// </summary>
        public static void ApplyToWindow(Window window) {
            window.FontFamily = new FontFamily(Configs.GlobalFontFamily);
            window.FontSize = Configs.GlobalFontSize;
            window.Foreground = BrushOf(Configs.GlobalFontColor);
        }

        /// <summary>颜色名/#RRGGBB转画刷（冻结），非法值返回null</summary>
        public static SolidColorBrush BrushOf(string colorName) {
            try {
                if (ColorConverter.ConvertFromString(colorName) is Color color) {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }
            } catch (Exception) {
                // 非法颜色名回落调用方默认
            }
            return null;
        }

        private static string TextOrDefault(string value, string defaultValue) {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private static int IntOrDefault(string value, int defaultValue) {
            return int.TryParse(value, out var v) && v >= 8 && v <= 40 ? v : defaultValue;
        }

        private static string ColorOrDefault(string value, string defaultValue) {
            return string.IsNullOrEmpty(value) || BrushOf(value) == null ? defaultValue : value;
        }
    }
}
