using AppManager2.Config;
using AppManager2.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Utils;

namespace AppManager2.Services {
    /// <summary>
    /// 应用进程运行服务（对应老项目 CliWrapUtils 的进程管理部分）
    /// 负责：启动进程并重定向输出、停止进程（stdin exit → kill）、执行停止脚本、发送键盘输入
    /// 日志通过事件抛出，由界面层（Dispatcher）接收后追加到对应的运行Tab
    /// </summary>
    public class AppRunService {
        private static AppRunService _instance;
        public static AppRunService Instance => _instance ??= new AppRunService();

        /// <summary>
        /// 日志输出事件（可能在非UI线程触发）：section, 日志内容（不含换行）
        /// </summary>
        public event Action<string, string> LogReceived;

        /// <summary>保存运行中的进程，key为section</summary>
        private readonly Dictionary<string, Process> _runningProcesses = new Dictionary<string, Process>();

        private AppRunService() { }

        /// <summary>
        /// 运行项目：java现场拼启动命令（内容与start.bat一致，仅少title行）交cmd.exe直接执行，
        /// bat/exe直接执行App路径
        /// </summary>
        public void StartApp(AppRecord app) {
            if (Configs.AppTypeJava.Equals(app.Type)) {
                var command = AMUtils.JavaAppUtils.BuildStartCommand(app, Configs.LogFileInfo, Configs.LogFileError, out var error);
                if (command == null) {
                    OnLog(app.Section, error);
                    return;
                }
                ExecuteApp("cmd.exe", "/c " + command, app.Ext, app.Section, true);
                return;
            }

            var cliPath = app.App;
            if (File.Exists(cliPath)) {
                // 启动的进程跟踪stdin，允许交互输入
                ExecuteApp(cliPath, app.Env, app.Ext, app.Section, true);
            } else {
                OnLog(app.Section, $"文件不存在：{cliPath}");
            }
        }

        /// <summary>
        /// 停止应用：先停止跟踪中的进程（stdin exit → 等待1秒 → kill），
        /// 再执行停止脚本（bat用StopApp），java现场拼命令经cmd.exe杀端口进程，exe类型按进程名杀进程
        /// </summary>
        public void StopApp(AppRecord app) {
            StopTrackedProcess(app.Section, app.Title);

            if (Configs.AppTypeJava.Equals(app.Type)) {
                // java停止不走stop.bat：现场按脚本同样逻辑拼命令交cmd.exe直接执行（不生成bat文件）
                ExecuteApp("cmd.exe", "/c " + AMUtils.JavaAppUtils.BuildStopCommand(app), string.Empty, app.Section, false);
                return;
            }

            if (Configs.AppTypeBat.Equals(app.Type) && !string.IsNullOrEmpty(app.StopApp)) {
                if (File.Exists(app.StopApp)) {
                    // 停止脚本不跟踪，避免覆盖启动进程的跟踪
                    ExecuteApp(app.StopApp, app.Env, app.Ext, app.Section, false);
                } else {
                    OnLog(app.Section, $"文件不存在：{app.StopApp}");
                }
                return;
            }

            // bat未配置停止脚本/exe通过进程名结束
            var existList = ProcessUtils.GetProcessByName(ProcessUtils.GetProcessName(app.App), app.App);
            if (null != existList && existList.Count > 0) {
                foreach (var p in existList) {
                    try {
                        OnLog(app.Section, $"开始杀进程{app.Title}");
                        p.Kill();
                        OnLog(app.Section, $"杀进程{app.Title}成功");
                    } catch (Exception ex) {
                        OnLog(app.Section, $"杀进程{app.Title}失败：{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 停止跟踪中的进程：先发送exit让交互式程序正常退出，等待1秒后强杀
        /// </summary>
        public void StopTrackedProcess(string section, string title) {
            Process process;
            lock (_runningProcesses) {
                if (!_runningProcesses.TryGetValue(section, out process)) {
                    return;
                }
            }

            try {
                if (!process.HasExited) {
                    try {
                        // 发送exit命令让进程正常退出（对于交互式程序）
                        process.StandardInput.WriteLine("exit");
                    } catch { }
                    var exited = process.WaitForExit(1000);
                    if (!exited) {
                        process.Kill();
                    }
                }
                OnLog(section, $"已停止进程：{title}");
            } catch (Exception ex) {
                OnLog(section, $"停止进程失败：{ex.Message}");
            } finally {
                lock (_runningProcesses) {
                    _runningProcesses.Remove(section);
                }
            }
        }

        /// <summary>
        /// 向运行中的进程发送键盘输入（Tab页底部输入框回车时调用）
        /// </summary>
        /// <returns>true发送成功</returns>
        public bool SendInput(string section, string input) {
            lock (_runningProcesses) {
                if (!_runningProcesses.TryGetValue(section, out var process)) {
                    OnLog(section, $"进程已结束，无法输入");
                    return false;
                }
                try {
                    if (process.HasExited) {
                        OnLog(section, $"进程已结束，无法输入");
                        return false;
                    }
                    process.StandardInput.WriteLine(input);
                    return true;
                } catch (Exception ex) {
                    OnLog(section, $"输入错误：{ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 进程是否存活（用于Tab关闭时判断）
        /// </summary>
        public bool IsProcessAlive(string section) {
            lock (_runningProcesses) {
                return _runningProcesses.TryGetValue(section, out var process)
                    && !process.HasExited;
            }
        }

        /// <summary>
        /// 使用Process执行程序（对应老项目CliWrapUtils.ExecuteApp），支持键盘输入
        /// </summary>
        private void ExecuteApp(string filePath, string arguments, string ext, string section, bool trackProcess) {
            try {
                string fileName = filePath;
                string argumentsFull = arguments;
                if (filePath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)) {
                    // 如果是 .bat，不直接启动 bat，而是启动 cmd.exe
                    fileName = "cmd.exe";
                    // 关键：先 chcp 65001 切换代码页，再执行 bat
                    argumentsFull = $"/c chcp 65001 > nul && \"{filePath}\" {arguments}";
                }
                var processStartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    WorkingDirectory = Path.GetDirectoryName(filePath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = trackProcess,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                if (!string.IsNullOrEmpty(argumentsFull)) {
                    processStartInfo.Arguments = argumentsFull;
                }
                if (!string.IsNullOrEmpty(ext)) {
                    string[] extArray = Regex.Split(ext, @"\s+")
                      .Where(x => x.Length > 0)
                      .ToArray();
                    foreach (string extSingle in extArray) {
                        if (extSingle.Contains("=")) {
                            string[] keyValueArray = extSingle.Split('=');
                            if (keyValueArray.Length > 1) {
                                string key = keyValueArray[0];
                                string value = keyValueArray[1];
                                processStartInfo.EnvironmentVariables[key] = value;
                            }
                        }
                    }
                }
                var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

                if (trackProcess) {
                    lock (_runningProcesses) {
                        _runningProcesses[section] = process;
                    }
                }

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        OnLog(section, e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        OnLog(section, $"Err->{e.Data}");
                    }
                };
                process.Exited += (sender, e) => {
                    try {
                        OnLog(section, $"--- 进程已退出 (退出码: {process.ExitCode}) ---");
                    } catch { }
                    lock (_runningProcesses) {
                        if (_runningProcesses.TryGetValue(section, out var cur) && cur == process) {
                            _runningProcesses.Remove(section);
                        }
                    }
                    process.Dispose();
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            } catch (Exception ex) {
                OnLog(section, $"启动进程失败：{ex.Message}");
            }
        }

        private void OnLog(string section, string line) {
            LogReceived?.Invoke(section, line);
        }
    }
}
