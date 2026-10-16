using AppManager2.Config;
using System.Collections.Generic;

namespace AppManager2.Models {
    /// <summary>
    /// 日志换行下拉选项（Code存库/ini，Text界面显示）
    /// </summary>
    public class LogWrapOption {
        public string Code { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// 日志换行选项集：项目编辑为“系统/自动/不换行”三选，设置页只有“不换行/自动换行”两选
    /// </summary>
    public static class LogWrapModes {
        public static readonly IReadOnlyList<LogWrapOption> AppOptions = new[] {
            new LogWrapOption { Code = Configs.LogWrapSystem, Text = "系统配置" },
            new LogWrapOption { Code = Configs.LogWrapNone, Text = "不换行" },
            new LogWrapOption { Code = Configs.LogWrapAuto, Text = "自动换行" },
        };

        public static readonly IReadOnlyList<LogWrapOption> SetOptions = new[] {
            new LogWrapOption { Code = Configs.LogWrapNone, Text = "不换行" },
            new LogWrapOption { Code = Configs.LogWrapAuto, Text = "自动换行" }
        };
    }
}
