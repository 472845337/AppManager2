using AppManager2.Config;
using AppManager2.Models;
using System;
using System.IO;
using System.Text;
using Utils;

namespace AppManager2.AMUtils {
    /// <summary>
    /// java进程工具，创建bat脚本（参考 AppManager 的生成方式）
    /// </summary>
    internal static class JavaAppUtils {
        public static string Profiles { get; set; }
        public static string JdkPath { get; set; }
        public static string LogPath { get; set; }

        /// <summary>
        /// 从 System.ini 加载全局设置（JDK、日志目录、运行环境），供脚本生成使用
        /// </summary>
        public static void LoadSettings() {
            var iniData = IniParserUtils.GetIniData(Configs.AppPath + "\\" + Configs.SystemIniPath);
            JdkPath = iniData[Configs.IniSectionSet][Configs.IniKeySetJdkpath];
            Profiles = iniData[Configs.IniSectionSet][Configs.IniKeySetProfiles];
            LogPath = iniData[Configs.IniSectionSet][Configs.IniKeySetLogpath];
        }

        /// <summary>
        /// 创建启动脚本，成功返回 bool.TrueString，失败返回错误信息
        /// </summary>
        public static string CreateStartBat(AppRecord app, string logFileName, string errorLogFileName) {
            var result = bool.TrueString;
            if (string.IsNullOrEmpty(app.Jdk)) {
                if (!string.IsNullOrEmpty(JdkPath) && !Directory.Exists(JdkPath)) {
                    return $"JDK路径{JdkPath}不存在，请重新配置！";
                }
            } else {
                if (!Directory.Exists(app.Jdk)) {
                    return $"项目:[{app.Title}]的JDK路径:[{app.Jdk}]不存在，请重新配置！";
                }
            }

            var script = new StringBuilder();
            script.Append("@echo off").Append(Environment.NewLine);
            script.Append("chcp 65001").Append(Environment.NewLine);
            script.Append("title ").Append("\"").Append(app.Title).Append("\"").Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(app.Ext)) {
                script.Append(StringUtils.TxtDecode(NewLine2Space(app.Ext))).Append(Environment.NewLine);
            }
            // 创建日志目录（如果需要输出日志）
            if (!app.IsPrintLog && !string.IsNullOrEmpty(LogPath) && !string.IsNullOrEmpty(logFileName)) {
                var projectLogPath = (LogPath.EndsWith(Configs.PathCharacter) ? LogPath : LogPath + Configs.PathCharacter) + app.Title + Configs.PathCharacter;
                FileUtils.CreateDirectory(projectLogPath);
                script.Append("if not exist \"").Append(projectLogPath).Append("\" mkdir \"").Append(projectLogPath).Append("\"").Append(Environment.NewLine);
            }
            // jdk路径
            var usedJdkPath = string.IsNullOrEmpty(app.Jdk) ? JdkPath : app.Jdk;
            if (Directory.Exists(usedJdkPath)) {
                script.Append("\"").Append(usedJdkPath).Append("\\bin\\java.exe\"");
            } else {
                script.Append("java");
            }
            // 启动参数
            if (!string.IsNullOrEmpty(app.Param)) {
                script.Append(" ").Append(StringUtils.EscapeBatSpecialChar(StringUtils.TxtDecode(NewLine2Space(app.Param))));
            }
            // jar包路径
            script.Append(" -jar ").Append("\"").Append(app.App).Append("\"");

            var useEnvProFiles = false;
            // 环境参数
            if (!string.IsNullOrEmpty(app.Env)) {
                script.Append(" ").Append(StringUtils.EscapeBatSpecialChar(StringUtils.TxtDecode(NewLine2Space(app.Env))));

                if (app.Env.Contains("spring.profiles.active=")) {
                    // 环境参数中配置了指定了启动环境
                    useEnvProFiles = true;
                }
            }
            // profile配置
            if (!useEnvProFiles) {
                script.Append(" --spring.profiles.active=").Append(Profiles);
            }
            // 指定启动端口
            script.Append(" --server.port=").Append(app.Port);
            // 是否将运行日志输出，配置文件后，日志将不会在console中输出
            if (!app.IsPrintLog && !string.IsNullOrEmpty(LogPath) && !string.IsNullOrEmpty(logFileName)) {
                var projectLogPath = (LogPath.EndsWith(Configs.PathCharacter) ? LogPath : LogPath + Configs.PathCharacter) + app.Title + Configs.PathCharacter;
                // 日志文件
                script.Append(" > \"").Append(projectLogPath).Append(logFileName).Append("\"");
                if (!string.IsNullOrEmpty(errorLogFileName)) {
                    // 异常日志
                    script.Append(" 2>").Append("\"").Append(projectLogPath).Append(errorLogFileName).Append("\"");
                }
                script.Append(" &");
            }
            script.Append(Environment.NewLine);
            // 窗口不关闭
            script.Append("pause");
            // 创建启动bat文件
            FileUtils.CreateFile(FileUtils.GetBatFilePath(app.Title, Configs.BatFileTypeStart), script.ToString(), new UTF8Encoding(false));
            return result;
        }

        /// <summary>
        /// 创建杀进程的脚本文件
        /// </summary>
        public static void CreateStopBat(AppRecord app) {
            var script = new StringBuilder();
            script.Append("@echo off").Append(Environment.NewLine);
            script.Append("setlocal enabledelayedexpansion").Append(Environment.NewLine);
            script.Append("for /f \"tokens=1-5\" %%a in ('netstat -ano ^| find \":").Append(app.Port).Append("\"') do (").Append(Environment.NewLine);
            script.Append("    echo %%b% | find \"").Append(app.Port).Append("\" >nul && (").Append(Environment.NewLine);
            script.Append("        if \"%%e%\" == \"\" (").Append(Environment.NewLine);
            script.Append("            set pid=%%d").Append(Environment.NewLine);
            script.Append("        ) else (").Append(Environment.NewLine);
            script.Append("            set pid=%%e").Append(Environment.NewLine);
            script.Append("        )").Append(Environment.NewLine);
            script.Append("        if pid GTR 0 (").Append(Environment.NewLine);
            script.Append("            echo will kill java progress:!pid!").Append(Environment.NewLine);
            script.Append("            taskkill -f -pid !pid!").Append(Environment.NewLine);
            script.Append("            echo kill java progress success").Append(Environment.NewLine);
            script.Append("            goto:end").Append(Environment.NewLine);
            script.Append("        )").Append(Environment.NewLine);
            script.Append("    )").Append(Environment.NewLine);
            script.Append(")").Append(Environment.NewLine);
            script.Append(":end");
            FileUtils.CreateFile(FileUtils.GetBatFilePath(app.Title, Configs.BatFileTypeStop), script.ToString(), new UTF8Encoding(false));
        }

        public static void RemoveBat(string title) {
            File.Delete(FileUtils.GetBatFilePath(title, Configs.BatFileTypeStop));
            File.Delete(FileUtils.GetBatFilePath(title, Configs.BatFileTypeStart));
        }

        /// <summary>
        /// 将换行替换为空格（多行参数拼成单行命令）
        /// </summary>
        private static string NewLine2Space(string str) {
            if (!string.IsNullOrEmpty(str)) {
                str = str.Replace("\\r\\n", " ").Replace("\\n", " ").Replace("\\r", " ");
            }
            return str;
        }
    }
}
