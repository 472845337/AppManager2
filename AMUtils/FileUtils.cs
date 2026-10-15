using AppManager2.Config;
using System.IO;
using System.Text;

namespace AppManager2.AMUtils {
    internal static class FileUtils {
        public static string GetBatFilePath(string projectTitle, string type) {
            var batTypePath = type switch {
                Configs.BatFileTypeStart => Configs.BatFileNameStart,
                Configs.BatFileTypeStop => Configs.BatFileNameStop,
                _ => string.Empty
            };
            return Configs.AppPath + Configs.BatPath + projectTitle + batTypePath;
        }

        public static void CreateFile(string filePath, string content, Encoding encoding) {
            var directPath = Path.GetDirectoryName(filePath);
            CreateDirectory(directPath);
            encoding ??= Encoding.UTF8;
            File.WriteAllText(filePath, content, encoding);
        }

        public static void CreateDirectory(string directPath) {
            if (!string.IsNullOrEmpty(directPath) && !Directory.Exists(directPath)) {
                Directory.CreateDirectory(directPath);
            }
        }
    }
}
