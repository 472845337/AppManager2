using System;

namespace AppManager2.Config {
    public static class Configs {
        public static string AppPath => AppDomain.CurrentDomain.BaseDirectory;
        public const string AuthorEmail = "1320311706@qq.com";

        public const string PathCharacter = "\\";
        public const string SystemIniPath = "ini\\System.ini";

        // 数据库文件名（由 App 启动时 SqliteConfig.Init 配置，SqliteUtils 会在程序运行目录下自动建库）
        public const string DbName = "AppManager.db";
        public const string DbPassword = "r4PdXkFrCvPyMCk4KNG3";

        public const string BatPath = "bat\\";
        public const string BatFileTypeStart = "start";
        public const string BatFileNameStart = "_start.bat";
        public const string BatFileTypeStop = "stop";
        public const string BatFileNameStop = "_stop.bat";
        public const string LogFileInfo = "info.log";
        public const string LogFileError = "error.log";

        public const string DefaultFontFamily = "微软雅黑";
        public const string ActuatorShutdown = "shutdown";

        // 项目运行状态
        public const short AppRunStatFail = -1;
        public const short AppRunStatUnrun = 0;
        public const short AppRunStatRunning = 1;
        public const short AppRunStatSuccess = 2;
        public const short AppRunStatStopping = 3;

        public const short OperateTypeAdd = 1;
        public const short OperateTypeUpdate = 2;
        public const short OperateTypeDetail = 3;

        // 项目批量操作类型
        public const int ProjectOperateTypeStart = 1;
        public const int ProjectOperateTypeStop = 2;

        // 项目属性INI Key名
        public const string IniSectionSystem = "system";
        public const string IniSectionApp = "app";
        public const string IniSectionSet = "set";
        public const string IniSectionTool = "tool";
        public const string IniSectionLog = "log";
        public const string IniSectionMonitor = "monitor";
        public const string IniSectionSend = "send";

        public const string IniKeyFormHeight = "height";
        public const string IniKeyFormWidth = "width";
        public const string IniKeyFormLocationX = "location_x";
        public const string IniKeyFormLocationY = "location_y";
        // 窗口是否以最大化状态关闭（下次启动恢复全屏；最小化关闭按正常状态记录）
        public const string IniKeyFormMaximized = "maximized";
        public const string IniKeyAppSplitRatio = "split_ratio";
        // 项目树组的收缩状态（保存收缩组名单，|分隔）
        public const string IniKeyAppTreeCollapsed = "tree_collapsed";
        // 上次关闭时选中的主Tab索引（0应用管理/1设置/2工具/3帮助）
        public const string IniKeyAppMainTabIndex = "main_tab_index";

        // 工具页PostDog（对应老项目IniKeyTool*；分隔位置改按百分比存储，避免绝对像素在不同窗口尺寸下溢出）
        public const string IniKeyToolSplitPct = "split-pct";
        public const string IniKeyToolSplitPctHead = "split-pct-head";
        public const string IniKeyToolIsTimeout = "is-timeout";
        public const string IniKeyToolTimeout = "timeout";
        public const string IniKeySaveHttpRequest = "save-http-request";

        // 发送记录窗口（对应老项目IniKeySend*）
        public const string IniKeySendSplitDis = "split-dis";

        public const string IniKeySetHeartBeat = "heartbeat";
        public const string IniKeySetIsWarn = "is_warn";
        public const string IniKeySetWarnTo = "warn_to";
        public const string IniKeySetServerName = "server_name";
        public const string IniKeySetProfiles = "profiles";
        public const string IniKeySetJdkpath = "JDKPath";
        public const string IniKeySetLogpath = "LogPath";
        public const string IniKeySetTimeout = "timeout";
        public const string IniKeySetInterval = "interval";
        public const string IniKeySetClosedType = "closed_type";
        // 设置-应用监控组的日志换行（运行日志TextWrapping的系统配置）
        public const string IniKeySetLogWrap = "log_wrap";
        // 数据库累计删除次数（达到阈值后执行VACUUM，保存于System.ini [set]）
        public const string IniKeySetDelCount = "DelCount";

        public const string IniKeyAppType = "type";
        public const string IniKeyAppTitle = "title";
        public const string IniKeyAppPath = "app";
        public const string IniKeyAppStoppath = "stop";
        public const string IniKeyAppJdk = "jdk";
        public const string IniKeyAppPort = "port";
        public const string IniKeyAppPrintLog = "printLog";
        public const string IniKeyAppHeartBeat = "heartBeat";
        public const string IniKeyAppActuator = "actuator";
        public const string IniKeyAppExt = "ext";
        public const string IniKeyAppParam = "param";
        public const string IniKeyAppEnv = "env";
        public const string IniKeyAppSort = "sort";
        public const string IniKeyAppProfiles = "profiles";
        public const string IniKeyAppGroup = "group";
        public const string IniKeyAppIsWarn = "warn";
        public const string IniKeyAppWarnUrl = "warnUrl";
        public const string IniKeyAppRemark = "remark";
        public const string IniKeyAppAutoRun = "auto_run";

        // 项目运行日志换行模式（t_app.log_wrap）：system跟随系统配置 / auto自动换行 / none不换行
        public const string LogWrapSystem = "system";
        public const string LogWrapAuto = "auto";
        public const string LogWrapNone = "none";

        public const string IniKeyWeixinAppId = "appId";
        public const string IniKeyWeixinSecret = "secret";

        public const string AesBase64Key = "Yl+wNn/Be259rsps4D9DkkjK6FcrHPOUsBNVZF39Puo=";
        public const string AesBase64Iv = "fuDv69y3hkjoKs6ncA7Xvg==";

        public const string AppTypeJava = "java";
        public const string AppTypeBat = "bat";
        public const string AppTypeExe = "exe";

        public const string ClosedTypeExit = "exit";
        public const string ClosedTypeMin = "min";

        /// <summary>关闭方式：exit退出 / min最小化（保存于System.ini [set] closed_type）</summary>
        public static string ClosedType = ClosedTypeExit;

        public const string HeartBeatMsg = "success";
        public static bool IsWarn = false;
        public static string WarnAddress;
        public static string ServerName;

        public static bool LogSwitch = false;
        public const string SwitchOn = "on";
        public const string SwitchOff = "off";

        // [set] 系统级日志换行配置（auto/none，默认不换行）：项目换行为“系统”时按此值生效
        public static string LogWrap = LogWrapNone;

        /// <summary>
        /// 项目换行模式解析为实际TextWrapping：空/“system”取系统配置，
        /// “auto”→WrapWithOverflow，其余（含“none”）→NoWrap
        /// </summary>
        public static System.Windows.TextWrapping ResolveLogWrapping(string appLogWrap) {
            var mode = string.IsNullOrEmpty(appLogWrap) || LogWrapSystem.Equals(appLogWrap)
                ? LogWrap
                : appLogWrap;
            return LogWrapAuto.Equals(mode)
                ? System.Windows.TextWrapping.WrapWithOverflow
                : System.Windows.TextWrapping.NoWrap;
        }

        public static int MonitorInterval = 5;
        public const int HttpDefaultTimeout = 15;
        public static int MonitorTimeout = 15;

        // 数据库删除计数：超过阈值后执行VACUUM收缩数据库文件并清零重计（参考XStart2.0的delCount）
        public const int DelCountLimit = 100;
        // 启动时从ini加载的初始计数（主窗口关闭时与DelCount比对，不同才写盘）
        public static int PriDelCount;
        // 运行期计数：删除数据后递增，超限清零并执行VACUUM
        public static int DelCount;

        public static bool SaveHttpRequest = false;

        public const string NodeFlagGroup = "group";
        public const string NodeFlagChild = "child";

        public const string ChildNodeJavaImageKey = "java.ico";
        public const string ChildNodeBatImageKey = "cmd.ico";
        public const string ChildNodeExeImageKey = "exe.ico";
        public const string GroupNodeImageKey = "group.ico";

        public static int Height;
        public static int Width;
        public static int Left;
        public static int Top;
        public static bool WindowMaximized;

        public static int SelectedIndex;

        // —— System.ini 参数缓存：启动时加载到Configs，保存前与缓存比对，值变化才写盘（参考XStart2.0的ConfigIniData方式）——
        // [app]
        public static string TreeSplitRatio; // split_ratio（按保存格式F3缓存）
        public static string TreeCollapsed;  // tree_collapsed（|分隔的收缩组名单）
        // [set]
        public static string HeartBeat;    // heartbeat（区别于默认值常量HeartBeatMsg）
        public static string JdkPath;      // JDKPath
        public static string Profiles;     // profiles
        public static string LogPath;      // LogPath
        public static string TimeoutText;  // timeout（监控超时原文）
        public static string IntervalText; // interval（监控间隔原文）
        // [tool]
        public static int ToolSplitPct;     // split-pct
        public static int ToolSplitPctHead; // split-pct-head
        public static bool ToolIsTimeout;   // is-timeout
        public static string ToolTimeout;   // timeout
        // [send]（发送记录窗口）运行值：主窗口启动时从ini加载，发送记录窗口关闭时回写，
        // 主窗口关闭时与启动初始值比对写盘（初始值快照存MainWindow）
        public static int SendSplitDis;   // split-dis
        public static int SendFormHeight; // height
        public static int SendFormWidth;  // width
        public static int SendFormLeft;   // location_x
        public static int SendFormTop;    // location_y
    }
}
