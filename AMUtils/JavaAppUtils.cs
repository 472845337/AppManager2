using AppManager2.Config;
using AppManager2.Models;
using System;
using System.Collections.Generic;
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

        // ini只在进程内首次加载一次：设置页“应用”会直接更新上面的运行值（不落盘），
        // 后续LoadSettings调用（bat重建/启动命令等）保留运行值，不回读ini覆盖
        private static bool _settingsLoaded;

        /// <summary>
        /// 从 System.ini 加载全局设置（JDK、日志目录、运行环境），供脚本生成使用；
        /// 首次调用后为空操作（运行值以“应用”更新为准，落盘在主窗口关闭时统一处理）
        /// </summary>
        public static void LoadSettings() {
            if (_settingsLoaded) {
                return;
            }
            var iniData = IniParserUtils.GetIniData(Configs.AppPath + "\\" + Configs.SystemIniPath);
            JdkPath = iniData[Configs.IniSectionSet][Configs.IniKeySetJdkpath];
            Profiles = iniData[Configs.IniSectionSet][Configs.IniKeySetProfiles];
            LogPath = iniData[Configs.IniSectionSet][Configs.IniKeySetLogpath];
            _settingsLoaded = true;
        }
        private static string GetFullLogPath(AppRecord app) {
            return (LogPath.EndsWith(Configs.PathCharacter) ? LogPath : (LogPath + Configs.PathCharacter)) + app.Title + Configs.PathCharacter;
        }
        /// <summary>
        /// 现场构建java启动命令（不落bat文件，运行时交cmd.exe直接执行）：
        /// 内容与CreateStartBat生成的start.bat一致，仅去掉title行，多行命令以" & "连接为单行。
        /// 日志目录由C#预建（见方法内注释），不能放进命令行
        /// </summary>
        /// <returns>命令串；JDK校验失败时返回null，原因见error</returns>
        public static string BuildStartCommand(AppRecord app, string logFileName, string errorLogFileName, out string error) {
            // 旧流程执行的是bat（日志路径在生成时固化），本流程依赖静态配置，
            // 启动前刷新保证LogPath/JdkPath/Profiles非空且为当前配置
            LoadSettings();
            error = ValidateJdk(app);
            if (error != null) {
                return null;
            }

            // 日志目录预建（原CreateStartBat的FileUtils.CreateDirectory职责）：
            // 不能拼成"if not exist 目录 mkdir 目录 & java..."——cmd的if会把同一行&
            // 链接的后续命令全部纳入控制体，目录已存在时java整条被跳过（实测），必须代码建目录
            if (!app.IsPrintLog && !string.IsNullOrEmpty(LogPath) && !string.IsNullOrEmpty(logFileName)) {
                string projectLogPath = GetFullLogPath(app);
                FileUtils.CreateDirectory(projectLogPath);
            }

            var parts = new List<string> { "@echo off", "chcp 65001" };
            var extLine = BuildExtLine(app);
            if (extLine != null) {
                parts.Add(extLine);
            }
            parts.Add(BuildJavaLine(app, logFileName, errorLogFileName));
            // 窗口不关闭（与start.bat末行一致）
            parts.Add("pause");
            return string.Join(" & ", parts);
        }

        /// <summary>
        /// JDK路径校验，通过返回null，否则返回错误信息（建bat与直接执行共用）
        /// </summary>
        private static string ValidateJdk(AppRecord app) {
            if (string.IsNullOrEmpty(app.Jdk)) {
                if (!string.IsNullOrEmpty(JdkPath) && !Directory.Exists(JdkPath)) {
                    return $"JDK路径{JdkPath}不存在，请重新配置！";
                }
            } else {
                if (!Directory.Exists(app.Jdk)) {
                    return $"项目:[{app.Title}]的JDK路径:[{app.Jdk}]不存在，请重新配置！";
                }
            }
            return null;
        }

        /// <summary>扩展命令行（Ext解码换行转空格），未配置返回null</summary>
        private static string BuildExtLine(AppRecord app) {
            if (string.IsNullOrEmpty(app.Ext)) {
                return null;
            }
            return StringUtils.TxtDecode(NewLine2Space(app.Ext));
        }

        /// <summary>
        /// java启动命令行：jdk路径 + 启动参数 + -jar + 环境参数 + profiles + 端口 + 日志重定向
        /// </summary>
        private static string BuildJavaLine(AppRecord app, string logFileName, string errorLogFileName) {
            var line = new StringBuilder();
            // jdk路径
            var usedJdkPath = string.IsNullOrEmpty(app.Jdk) ? JdkPath : app.Jdk;
            if (Directory.Exists(usedJdkPath)) {
                line.Append("\"").Append(usedJdkPath).Append("\\bin\\java.exe\"");
            } else {
                line.Append("java");
            }
            // 启动参数
            if (!string.IsNullOrEmpty(app.Param)) {
                line.Append(" ").Append(StringUtils.EscapeBatSpecialChar(StringUtils.TxtDecode(NewLine2Space(app.Param))));
            }
            // jar包路径
            line.Append(" -jar ").Append("\"").Append(app.App).Append("\"");

            var useEnvProFiles = false;
            // 环境参数
            if (!string.IsNullOrEmpty(app.Env)) {
                line.Append(" ").Append(StringUtils.EscapeBatSpecialChar(StringUtils.TxtDecode(NewLine2Space(app.Env))));

                if (app.Env.Contains("spring.profiles.active=")) {
                    // 环境参数中配置了指定了启动环境
                    useEnvProFiles = true;
                }
            }
            // profile配置
            if (!useEnvProFiles) {
                line.Append(" --spring.profiles.active=").Append(Profiles);
            }
            // 指定启动端口
            line.Append(" --server.port=").Append(app.Port);
            // 是否将运行日志输出，配置文件后，日志将不会在console中输出
            if (!app.IsPrintLog && !string.IsNullOrEmpty(LogPath) && !string.IsNullOrEmpty(logFileName)) {
                var projectLogPath = GetFullLogPath(app);
                // 日志文件
                line.Append(" > \"").Append(projectLogPath).Append(logFileName).Append("\"");
                if (!string.IsNullOrEmpty(errorLogFileName)) {
                    // 异常日志
                    line.Append(" 2>").Append("\"").Append(projectLogPath).Append(errorLogFileName).Append("\"");
                }
            }
            return line.ToString();
        }

        /// <summary>
        /// 现场构建java停止命令（不落bat文件，运行时交cmd.exe直接执行）：
        /// 逻辑与原stop.bat一致——netstat找监听端口的PID后taskkill强杀。
        /// 单行命令形式的必要适配：for变量%%a改%a（双百分号是bat文件专用）；
        /// 去掉setlocal延迟展开（直接用%e取PID，不再需要!pid!中转）；
        /// 去掉goto:end（单行命令无标签可用，其"杀一个即停"由本地地址含端口的过滤保证——
        /// 命中的都是同一服务进程）；if pid GTR 0为原脚本笔误（比较字面量pid恒真）一并省去
        /// </summary>
        public static string BuildStopCommand(AppRecord app) {
            var line = new StringBuilder();
            line.Append("for /f \"tokens=1-5\" %a in ('netstat -ano ^| find \":").Append(app.Port)
                .Append("\"') do @(echo %b | find \":").Append(app.Port)
                .Append("\" >nul && (echo will kill java progress:%e & taskkill -f -pid %e & echo kill java progress success))");
            return line.ToString();
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
