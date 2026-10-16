using AppManager2.Config;
using AppManager2.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace AppManager2.Services {
    /// <summary>
    /// 应用监控服务（对应老项目 TimerUtils 的 Monitor 部分）
    /// 定时对每个应用做三级存活判定：心跳Url → 端口占用 → exe进程，
    /// 刷新运行状态（树节点颜色由界面层根据事件刷新），异常停止时发送企业微信机器人预警
    /// </summary>
    public class AppMonitorService {
        private static AppMonitorService _instance;
        public static AppMonitorService Instance => _instance ??= new AppMonitorService();

        private readonly System.Timers.Timer _monitorTimer = new System.Timers.Timer();
        private readonly AppRunService _runService = AppRunService.Instance;
        private readonly AppService _appService = AppService.Instance;

        /// <summary>心跳接口预期的返回内容，可在设置页自定义（保存后由VM更新）</summary>
        public string HeartBeatMsg { get; set; } = Configs.HeartBeatMsg;

        /// <summary>应用运行状态变化（可能在非UI线程触发）：section, 运行状态, 是否运行中</summary>
        public event Action<string, short, bool> RunStatChanged;

        /// <summary>监控日志（可能在非UI线程触发）：section, 日志内容</summary>
        public event Action<string, string> LogReceived;

        /// <summary>自启动请求（初始化监控时应用未运行且配置了AutoRun）</summary>
        public event Action<AppRecord> StartRequested;

        private AppMonitorService() {
            // 订阅只做一次，避免 Start 被多次调用时重复触发监控
            _monitorTimer.AutoReset = true;
            _monitorTimer.Elapsed += (sender, e) => MonitorAll();
        }

        /// <summary>
        /// 加载环境配置（对应老项目 InitEnv 中监控相关的部分）
        /// </summary>
        public void LoadSettings() {
            var iniData = IniParserUtils.GetIniData(Configs.AppPath + "\\" + Configs.SystemIniPath);
            var heartBeat = iniData[Configs.IniSectionSet][Configs.IniKeySetHeartBeat];
            var isWarn = iniData[Configs.IniSectionSet][Configs.IniKeySetIsWarn];
            var warnTo = iniData[Configs.IniSectionSet][Configs.IniKeySetWarnTo];
            var serverName = iniData[Configs.IniSectionSet][Configs.IniKeySetServerName];
            var timeout = iniData[Configs.IniSectionSet][Configs.IniKeySetTimeout];
            var interval = iniData[Configs.IniSectionSet][Configs.IniKeySetInterval];

            HeartBeatMsg = string.IsNullOrEmpty(heartBeat) ? Configs.HeartBeatMsg : heartBeat;
            Configs.IsWarn = bool.TrueString.Equals(isWarn);
            Configs.WarnAddress = warnTo;
            Configs.ServerName = serverName;

            if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out var timeoutVal) && timeoutVal > 0) {
                Configs.MonitorTimeout = timeoutVal;
            }
            if (!string.IsNullOrEmpty(interval) && int.TryParse(interval, out var intervalVal) && intervalVal > 0) {
                Configs.MonitorInterval = intervalVal;
            }
        }

        /// <summary>
        /// 启动监控定时器
        /// </summary>
        public void Start() {
            // MonitorInterval 单位为秒，需转换为毫秒（漏乘会导致定时器以毫秒级间隔高频触发）
            _monitorTimer.Interval = Configs.MonitorInterval * 1000;
            _monitorTimer.Enabled = true;
        }

        /// <summary>
        /// 停止监控定时器
        /// </summary>
        public void Stop() {
            _monitorTimer.Enabled = false;
        }

        /// <summary>
        /// 设置页保存后调用：应用新的监控频次（对应老项目保存设置后更新Configs.monitorInterval）
        /// </summary>
        public void UpdateInterval() {
            if (_monitorTimer.Enabled) {
                _monitorTimer.Interval = Configs.MonitorInterval * 1000;
            }
        }

        private int _monitoring;

        private void MonitorAll() {
            // 防重入：一轮检测最坏耗时约 2×MonitorTimeout 秒（心跳POST+GET都超时），
            // 上一轮未结束时跳过本轮，避免监控任务堆积拖垮线程池
            if (Interlocked.CompareExchange(ref _monitoring, 1, 0) != 0) return;
            Task.Run(async () => {
                try {
                    var sections = _appService.GetAllSections();
                    if (sections == null) return;
                    var tasks = sections
                        .Select(section => MonitorAppAsync(_appService.GetAppBySection(section), false))
                        .ToArray();
                    await Task.WhenAll(tasks);
                } finally {
                    Volatile.Write(ref _monitoring, 0);
                }
            });
        }

        /// <summary>
        /// 使用异步对应用进行监听（对应老项目 TimerUtils.Monitor 扩展方法）
        /// 优先调用心跳Url连接判定，然后使用端口判断占用情况，再判断进程是否运行（用于.exe）
        /// </summary>
        /// <param name="app">应用</param>
        /// <param name="appInit">是否应用初始化监控（应用启动/程序启动时传true，会触发AutoRun）</param>
        public void MonitorApp(AppRecord app, bool appInit) {
            MonitorAppAsync(app, appInit);
        }

        private Task MonitorAppAsync(AppRecord app, bool appInit) {
            if (app == null) return Task.CompletedTask;
            return Task.Run(() => {
                try {
                    MonitorCore(app, appInit);
                } catch (Exception ex) {
                    Debug.WriteLine($"监控[{app.Title}]异常：{ex.Message}");
                }
            });
        }

        private void MonitorCore(AppRecord app, bool appInit) {
            // 三级存活判定
            string result = null;
            var heartBeatUrl = app.HeartBeat;
            if (!string.IsNullOrEmpty(heartBeatUrl)) {
                try {
                    result = HttpUtils.PostRequest(heartBeatUrl, null, null, Configs.MonitorTimeout * 1000);
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }

                if (!HeartBeatMsg.Equals(result)) {
                    try {
                        HttpUtils.GetRequest(heartBeatUrl, null, Configs.MonitorTimeout * 1000);
                        // 请求无异常，表示地址是可以访问的
                        result = HeartBeatMsg;
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                }
            } else if (!string.IsNullOrEmpty(app.Port)) {
                if (PortUtils.PortInUse(Convert.ToInt16(app.Port))) {
                    result = HeartBeatMsg;
                }
            } else if (Configs.AppTypeExe.Equals(app.Type)) {
                // 仅exe类型进行进程判断
                var existList = ProcessUtils.GetProcessByName(ProcessUtils.GetProcessName(app.App), app.App);
                if (null != existList && existList.Count > 0) {
                    result = HeartBeatMsg;
                }
            }

            var runStat = app.RunStat;
            if (HeartBeatMsg.Equals(result)) {
                // 项目运行中
                if (app.RunStat != Configs.AppRunStatSuccess && app.RunStat != Configs.AppRunStatStopping) {
                    LogReceived?.Invoke(app.Section, "app is running!");
                }
                if (app.RunStat != Configs.AppRunStatStopping) {
                    runStat = Configs.AppRunStatSuccess;
                }
                app.IsRunning = true;
            } else {
                if (appInit && app.AutoRun) {
                    // 程序初始化时，应用配置了自启动，执行启动。
                    // 启动当次直接结束判定（老版本此处会继续走状态机把刚启动的应用判为失败并预警，属于原实现瑕疵）
                    app.IsRunning = true;
                    app.RunStat = Configs.AppRunStatRunning;
                    StartRequested?.Invoke(app);
                    RunStatChanged?.Invoke(app.Section, Configs.AppRunStatRunning, app.IsRunning);
                    return;
                }

                if (app.IsRunning) {
                    switch (app.RunStat) {
                        case Configs.AppRunStatStopping:
                        case Configs.AppRunStatUnrun:
                            // 停止中/未运行状态，确认项目已停止
                            runStat = Configs.AppRunStatUnrun;
                            LogReceived?.Invoke(app.Section, "app is unrun!");
                            if (Configs.AppRunStatStopping == app.RunStat) {
                                // 停止中状态，判断到项目已经停止，则将任务运行状态置为否
                                app.IsRunning = false;
                            }
                            break;
                        case Configs.AppRunStatRunning:
                        case Configs.AppRunStatSuccess:
                        case Configs.AppRunStatFail:
                            // 运行中/运行成功/运行失败，则判定为运行失败
                            runStat = Configs.AppRunStatFail;
                            LogReceived?.Invoke(app.Section, "app is fail!");
                            SendWarn(app);
                            break;
                    }
                } else {
                    runStat = Configs.AppRunStatUnrun;
                }
            }

            app.RunStat = runStat;
            // 通知界面刷新节点状态和颜色（对应老项目 FreshAppColor）
            RunStatChanged?.Invoke(app.Section, runStat, app.IsRunning);
        }

        /// <summary>
        /// 项目异常停止预警（企业微信机器人）
        /// </summary>
        private void SendWarn(AppRecord app) {
            try {
                if (!Configs.IsWarn || !app.IsWarn) {
                    return;
                }
                if (string.IsNullOrEmpty(app.WarnUrl) && string.IsNullOrEmpty(Configs.WarnAddress)) {
                    return;
                }

                var text = new WxRobotMessage.TextClass {
                    Content = $"[错误]{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{(string.IsNullOrEmpty(Configs.ServerName) ? IpUtils.GetLocalIp() : Configs.ServerName)}] [{app.Title}]项目异常停止，请处理！"
                };
                var wxRobotMessage = new WxRobotMessage { Msgtype = WxRobotMessage.MsgtypeText, Text = text };
                string wxUrl = string.IsNullOrEmpty(app.WarnUrl) ? Configs.WarnAddress : app.WarnUrl;
                HttpUtils.PostRequest(wxUrl, JsonConvert.SerializeObject(wxRobotMessage), null, Configs.MonitorTimeout * 1000);
            } catch (Exception ex) {
                Debug.WriteLine($"预警发送失败：{ex.Message}");
            }
        }
    }
}
