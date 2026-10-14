using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AppManager2.Utils
{
    /// <summary>
    /// exe图标提取工具（对应老项目 Utils/ExeIconUtils），提取结果缓存，避免重复IO
    /// </summary>
    internal static class ExeIconUtils
    {
        private static readonly Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>();

        /// <summary>
        /// 获取exe文件的图标，失败返回null（调用方使用默认exe.ico）
        /// </summary>
        public static ImageSource GetIconImage(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return null;
            }

            lock (Cache)
            {
                if (Cache.TryGetValue(exePath, out var cached))
                {
                    return cached;
                }
            }

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon == null)
                {
                    return null;
                }
                var source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                lock (Cache)
                {
                    Cache[exePath] = source;
                }
                return source;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
