using AppManager2.AMUtils;
using AppManager2.Config;
using PropertyChanged;
using System;
using System.Windows;
using System.Windows.Documents;

namespace AppManager2.Models {
    /// <summary>
    /// 应用运行Tab页（对应老项目CreateAppRunTabPage创建的TabPage+RichTextBox+输入框）：
    /// 日志为RichTextBox彩色显示，日志文档由模型持有，切Tab时控件直挂对应文档
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class AppRunTabItemModel {
        /// <summary>日志面板最大行数（对应老项目Configs.LogTextMaxLines）</summary>
        private const int LogTextMaxLines = 500;

        /// <summary>本项目运行日志文档：按行追加Paragraph，ANSI颜色码解析成彩色Run（须在UI线程操作）</summary>
        public FlowDocument LogDocument { get; } = new FlowDocument { PagePadding = new Thickness(0) };

        /// <summary>ANSI颜色渲染状态（跨行保持，未复位的颜色延续到下一行，与终端一致）</summary>
        private readonly AnsiTextParser.RenderState _ansiState = new AnsiTextParser.RenderState();

        public string Section { get; set; }
        public string Title { get; set; }
        public string InputText { get; set; }
        public bool IsRunning { get; set; }
        public bool IsClosed { get; set; }
        public DateTime StartTime { get; set; }

        // 运行日志换行：LogWrap为项目配置的模式（system/auto/none，右键菜单切换勾选用），
        // LogWrapping为按系统配置解析后的实际TextWrapping（RichTextBox绑定用）
        public string LogWrap { get; set; } = Configs.LogWrapSystem;
        public System.Windows.TextWrapping LogWrapping { get; set; } = System.Windows.TextWrapping.NoWrap;

        // 运行日志字号（Ctrl+滚轮调整）：必须存模型而非控件本地值——TabControl切换Tab时
        // 复用同一棵元素树只换DataContext，控件本地值会串到其他项目；每Tab模型独立即互不影响。
        // 初始值取软件设置的日志字号（[set] log_font_size），缩放不持久化，重启恢复该初始值
        public double LogFontSize { get; set; } = Configs.LogFontSize;

        public AppRunTabItemModel() {
            InputText = string.Empty;
            IsClosed = false;
        }

        /// <summary>
        /// 追加一行日志（ANSI颜色码解析成彩色Run），超过最大行数时裁掉前面的
        /// （对应老项目RichTextBox的TextChanged裁剪）
        /// </summary>
        public void AppendLog(string line) {
            var paragraph = new Paragraph { Margin = new Thickness(0) };
            foreach (var segment in AnsiTextParser.Parse(line, _ansiState)) {
                var run = new Run(segment.Text);
                if (segment.Foreground != null) {
                    run.Foreground = segment.Foreground;
                }
                if (segment.Background != null) {
                    run.Background = segment.Background;
                }
                if (segment.Bold) {
                    run.FontWeight = FontWeights.Bold;
                }
                paragraph.Inlines.Add(run);
            }
            LogDocument.Blocks.Add(paragraph);
            while (LogDocument.Blocks.Count > LogTextMaxLines) {
                LogDocument.Blocks.Remove(LogDocument.Blocks.FirstBlock);
            }
        }

        /// <summary>
        /// 清空日志（连同跨行颜色状态复位）
        /// </summary>
        public void ClearLog() {
            LogDocument.Blocks.Clear();
            _ansiState.Reset();
        }
    }
}
