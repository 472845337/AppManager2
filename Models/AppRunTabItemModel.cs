using AppManager2.AMUtils;
using AppManager2.Config;
using PropertyChanged;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

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
        // LogWrapping为按系统配置解析后的实际换行模式（RichTextBox无TextWrapping属性，
        // 变更时由OnChanged应用到LogDocument.PageWidth，保存设置/右键切换都即时生效）
        public string LogWrap { get; set; } = Configs.LogWrapSystem;
        [OnChangedMethod(nameof(ApplyWrapToDocument))]
        public System.Windows.TextWrapping LogWrapping { get; set; } = System.Windows.TextWrapping.NoWrap;

        // 运行日志字号（Ctrl+滚轮调整）：必须存模型而非控件本地值——TabControl切换Tab时
        // 复用同一棵元素树只换DataContext，控件本地值会串到其他项目；每Tab模型独立即互不影响。
        // 初始值取软件设置的日志字号（[set] log_font_size），缩放不持久化，重启恢复该初始值。
        // 变更后全量重算不换行宽度（字号影响实测行宽）
        [OnChangedMethod(nameof(RefreshWrapWidth))]
        public double LogFontSize { get; set; } = Configs.LogFontSize;

        // 不换行模式下已输出各行的实测最大宽度（FormattedText测量，与终端式日志查看器一致：
        // 水平滚动条只到最宽的一行，而不是固定的16384）
        private double _maxLineWidth;

        public AppRunTabItemModel() {
            InputText = string.Empty;
            IsClosed = false;
            // 初始换行模式直接应用到文档（构造后EnsureTab若设为相同值不再触发变更事件）
            ApplyWrapToDocument();
        }

        /// <summary>
        /// 换行模式应用到日志文档：RichTextBox无TextWrapping属性（TextBox独有），
        /// 不换行时PageWidth取各行实测最大宽度（滚动条到最宽行；无内容时Auto跟随视口），
        /// 换行恢复PageWidth默认（NaN，跟随视口宽度自动折行）。
        /// 模型持有文档，设置页保存/右键切换更新本属性后，当前显示的RichTextBox即时跟随
        /// </summary>
        private void ApplyWrapToDocument() {
            LogDocument.PageWidth = LogWrapping == System.Windows.TextWrapping.NoWrap ? WrapPageWidth() : double.NaN;
        }

        /// <summary>不换行时的文档宽度：最宽行宽+边距；无内容返回NaN（跟随视口）</summary>
        private double WrapPageWidth() {
            return _maxLineWidth > 0 ? _maxLineWidth + 12 : double.NaN;
        }

        /// <summary>
        /// 全量重算各行实测宽度（字号调整/设置页换字体后调用），不换行时刷新PageWidth
        /// </summary>
        public void RefreshWrapWidth() {
            var width = 0.0;
            foreach (Paragraph paragraph in LogDocument.Blocks.Cast<Paragraph>()) {
                var line = 0.0;
                foreach (Run run in paragraph.Inlines.Cast<Run>()) {
                    line += MeasureWidth(run.Text, run.FontWeight == FontWeights.Bold);
                }
                if (line > width) width = line;
            }
            _maxLineWidth = width;
            if (LogWrapping == System.Windows.TextWrapping.NoWrap) {
                LogDocument.PageWidth = WrapPageWidth();
            }
        }

        /// <summary>用当前日志字体（资源LogFontFamily+模型LogFontSize）实测文本宽度</summary>
        private double MeasureWidth(string text, bool bold) {
            if (string.IsNullOrEmpty(text)) return 0;
            var family = Application.Current?.TryFindResource(ThemeUtils.KeyLogFontFamily) as FontFamily
                ?? SystemFonts.MessageFontFamily;
            var typeface = new Typeface(family, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
            var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight, typeface, LogFontSize, Brushes.Black, 1.0);
            return formatted.Width;
        }

        /// <summary>
        /// 追加一行日志（ANSI颜色码解析成彩色Run），超过最大行数时裁掉前面的
        /// （对应老项目RichTextBox的TextChanged裁剪）
        /// </summary>
        public void AppendLog(string line) {
            var paragraph = new Paragraph { Margin = new Thickness(0) };
            var lineWidth = 0.0;
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
                lineWidth += MeasureWidth(segment.Text, segment.Bold);
            }
            LogDocument.Blocks.Add(paragraph);
            if (lineWidth > _maxLineWidth) {
                _maxLineWidth = lineWidth;
                // 不换行时新行更宽才需要扩PageWidth（变窄的行不影响滚动范围）
                if (LogWrapping == System.Windows.TextWrapping.NoWrap) {
                    LogDocument.PageWidth = WrapPageWidth();
                }
            }
            while (LogDocument.Blocks.Count > LogTextMaxLines) {
                LogDocument.Blocks.Remove(LogDocument.Blocks.FirstBlock);
            }
        }

        /// <summary>
        /// 清空日志（连同跨行颜色状态复位与不换行宽度复位）
        /// </summary>
        public void ClearLog() {
            LogDocument.Blocks.Clear();
            _ansiState.Reset();
            _maxLineWidth = 0;
            if (LogWrapping == System.Windows.TextWrapping.NoWrap) {
                LogDocument.PageWidth = double.NaN;
            }
        }
    }
}
