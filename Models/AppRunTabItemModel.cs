using AppManager2.Config;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.Text;

namespace AppManager2.Models
{
    /// <summary>
    /// 应用运行Tab页（对应老项目CreateAppRunTabPage创建的TabPage+RichTextBox+输入框）
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class AppRunTabItemModel
    {
        /// <summary>日志面板最大行数（对应老项目Configs.LogTextMaxLines）</summary>
        private const int LogTextMaxLines = 500;

        private readonly StringBuilder _logBuilder = new StringBuilder();

        public string Section { get; set; }
        public string Title { get; set; }
        public string LogContent { get; set; }
        public string InputText { get; set; }
        public bool IsRunning { get; set; }
        public bool IsClosed { get; set; }
        public DateTime StartTime { get; set; }

        public AppRunTabItemModel()
        {
            LogContent = string.Empty;
            InputText = string.Empty;
            IsClosed = false;
        }

        /// <summary>
        /// 追加一行日志，超过最大行数时裁掉前面的（对应老项目RichTextBox的TextChanged裁剪）
        /// </summary>
        public void AppendLog(string line)
        {
            _logBuilder.AppendLine(line);
            // 裁剪：按行数限制
            var content = _logBuilder.ToString();
            var newLineCount = 0;
            var trimIndex = -1;
            for (var i = content.Length - 1; i >= 0; i--)
            {
                if (content[i] == '\n')
                {
                    newLineCount++;
                    if (newLineCount > LogTextMaxLines)
                    {
                        trimIndex = i + 1;
                        break;
                    }
                }
            }
            if (trimIndex > 0)
            {
                _logBuilder.Remove(0, trimIndex);
            }
            LogContent = _logBuilder.ToString();
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public void ClearLog()
        {
            _logBuilder.Clear();
            LogContent = string.Empty;
        }
    }
}
