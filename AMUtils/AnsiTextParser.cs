using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace AppManager2.AMUtils {
    /// <summary>
    /// ANSI转义序列文本解析：把日志中的SGR颜色码（\x1b[31m等）转成带色文本段，
    /// 其他控制序列（光标移动/OSC窗口标题等）剔除，使日志在RichTextBox中显示得和终端一致。
    /// 颜色表采用Windows Terminal默认Campbell配色（16色），256色与真彩色按标准算法计算。
    /// </summary>
    public static class AnsiTextParser {
        /// <summary>解析出的一个带样式文本段（Brush为null表示继承控件默认前景/透明背景）</summary>
        public class Segment {
            public string Text;
            public Brush Foreground;
            public Brush Background;
            public bool Bold;
        }

        /// <summary>
        /// 跨行的SGR渲染状态：每条日志流（每个运行Tab）共用一份，
        /// 颜色未复位（无\x1b[0m）时延续到下一行，与终端行为一致
        /// </summary>
        public class RenderState {
            public Brush Foreground;
            public Brush Background;
            public bool Bold;

            public void Reset() {
                Foreground = null;
                Background = null;
                Bold = false;
            }
        }

        // CSI序列：ESC [ 参数 中间字符 终止符（参数部分兼容':'分隔的子参数写法）
        private static readonly Regex CsiRegex = new Regex("\x1B\\[([0-9;:?]*)([ -/]*)([@-~])", RegexOptions.Compiled);
        // OSC序列（窗口标题等）：ESC ] ... BEL 或 ESC ] ... ESC \
        private static readonly Regex OscRegex = new Regex("\x1B\\][^\x07\x1b]*(?:\x07|\x1B\\\\)", RegexOptions.Compiled);
        // 其余零散单字符转义（ESC后跟一个字符）
        private static readonly Regex EscRegex = new Regex("\x1B.", RegexOptions.Compiled);

        // Campbell配色（Windows Terminal默认）：下标0-7常规色，8-15亮色
        private static readonly Color[] BasicColors =
        {
            Color.FromRgb(0x0C, 0x0C, 0x0C), Color.FromRgb(0xC5, 0x0F, 0x1F),
            Color.FromRgb(0x13, 0xA1, 0x0E), Color.FromRgb(0xC1, 0x9C, 0x00),
            Color.FromRgb(0x00, 0x37, 0xDA), Color.FromRgb(0x88, 0x17, 0x98),
            Color.FromRgb(0x3A, 0x96, 0xDD), Color.FromRgb(0xCC, 0xCC, 0xCC),
            Color.FromRgb(0x76, 0x76, 0x76), Color.FromRgb(0xE7, 0x48, 0x56),
            Color.FromRgb(0x16, 0xC6, 0x0C), Color.FromRgb(0xF9, 0xF1, 0xA5),
            Color.FromRgb(0x3B, 0x78, 0xFF), Color.FromRgb(0xB4, 0x00, 0x9E),
            Color.FromRgb(0x61, 0xD6, 0xD6), Color.FromRgb(0xF2, 0xF2, 0xF2),
        };

        // 256色立方体每个通道的6级亮度
        private static readonly int[] CubeLevels = { 0, 95, 135, 175, 215, 255 };

        // 冻结画刷缓存（同色复用，避免每段日志新建Brush）
        private static readonly Dictionary<Color, SolidColorBrush> BrushCache = new Dictionary<Color, SolidColorBrush>();

        /// <summary>
        /// 解析一行日志：按转义序列切分为带色文本段，SGR写入state（跨行保持），其余序列剔除
        /// </summary>
        public static List<Segment> Parse(string line, RenderState state) {
            var result = new List<Segment>();
            if (string.IsNullOrEmpty(line)) return result;

            line = OscRegex.Replace(line, string.Empty);
            var cursor = 0;
            foreach (Match m in CsiRegex.Matches(line)) {
                AddText(result, line.Substring(cursor, m.Index - cursor), state);
                if (m.Groups[3].Value == "m") {
                    ApplySgr(m.Groups[1].Value, state);
                }
                cursor = m.Index + m.Length;
            }
            AddText(result, line.Substring(cursor), state);
            return result;
        }

        private static void AddText(List<Segment> result, string text, RenderState state) {
            if (text.Length == 0) return;
            // 残留的非CSI转义（个别单字符序列）剔除后再输出
            text = EscRegex.Replace(text, string.Empty);
            if (text.Length == 0) return;
            result.Add(new Segment {
                Text = text,
                Foreground = state.Foreground,
                Background = state.Background,
                Bold = state.Bold
            });
        }

        /// <summary>
        /// 应用SGR参数（颜色/加粗等）：0复位、1加粗、30-37/90-97前景、40-47/100-107背景、
        /// 38/48扩展色（5;n为256色，2;r;g;b为真彩色）、39/49恢复默认
        /// </summary>
        private static void ApplySgr(string paramsText, RenderState state) {
            if (string.IsNullOrEmpty(paramsText)) {
                state.Reset();
                return;
            }
            // 同时兼容';'和':'分隔（个别程序输出38:5:196这类子参数写法）
            var parts = paramsText.Split(';', ':');
            var codes = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++) {
                // 空参数按0处理（\x1b[m等价于\x1b[0m）
                codes[i] = int.TryParse(parts[i], out var v) ? v : 0;
            }

            for (var i = 0; i < codes.Length; i++) {
                var code = codes[i];
                if (code == 0) state.Reset();
                else if (code == 1) state.Bold = true;
                else if (code == 22) state.Bold = false;
                else if (code == 39) state.Foreground = null;
                else if (code == 49) state.Background = null;
                else if (code >= 30 && code <= 37) state.Foreground = GetBrush(ColorFrom256(code - 30));
                else if (code >= 90 && code <= 97) state.Foreground = GetBrush(ColorFrom256(code - 90 + 8));
                else if (code >= 40 && code <= 47) state.Background = GetBrush(ColorFrom256(code - 40));
                else if (code >= 100 && code <= 107) state.Background = GetBrush(ColorFrom256(code - 100 + 8));
                else if (code == 38 || code == 48) {
                    // 扩展色：参数不足时放弃（保持原色）
                    if (i + 1 >= codes.Length) break;
                    var mode = codes[++i];
                    if (mode == 5 && i + 1 < codes.Length) {
                        var color = GetBrush(ColorFrom256(codes[++i]));
                        if (code == 38) state.Foreground = color; else state.Background = color;
                    } else if (mode == 2 && i + 3 < codes.Length) {
                        var color = GetBrush(Color.FromRgb(
                            Clamp(codes[i + 1]), Clamp(codes[i + 2]), Clamp(codes[i + 3])));
                        i += 3;
                        if (code == 38) state.Foreground = color; else state.Background = color;
                    } else {
                        break;
                    }
                }
                // 其余SGR码（斜体/下划线/反显等）日志场景不渲染，忽略
            }
        }

        /// <summary>256色索引转颜色：0-15基础色，16-231六立方体，232-255灰阶</summary>
        private static Color ColorFrom256(int index) {
            if (index < 0) index = 0;
            if (index < 16) return BasicColors[index];
            if (index < 232) {
                var i = index - 16;
                return Color.FromRgb(
                    (byte)CubeLevels[(i / 36) % 6],
                    (byte)CubeLevels[(i / 6) % 6],
                    (byte)CubeLevels[i % 6]);
            }
            if (index > 255) index = 255;
            var gray = 8 + (index - 232) * 10;
            return Color.FromRgb((byte)gray, (byte)gray, (byte)gray);
        }

        private static byte Clamp(int value) {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }

        private static SolidColorBrush GetBrush(Color color) {
            if (!BrushCache.TryGetValue(color, out var brush)) {
                brush = new SolidColorBrush(color);
                brush.Freeze();
                BrushCache[color] = brush;
            }
            return brush;
        }
    }
}
