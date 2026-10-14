using System;

namespace AppManager2.Config
{
    public static class Configs
    {
        public static string AppPath => AppDomain.CurrentDomain.BaseDirectory;
        public const string AuthorEmail = "1320311706@qq.com";

        public const string PathCharacter = "\\";
        public const string ProjectsIniPath = "ini\\Projects.ini";
        public const string SystemIniPath = "ini\\System.ini";
        public const string RecycleIniPath = "ini\\recycle.ini";
        public const string WeixinIniPath = "ini\\weixin.ini";

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

        public static int MonitorInterval = 5;
        public const int HttpDefaultTimeout = 15;
        public static int MonitorTimeout = 15;

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

        public static int SelectedIndex;
    }
}
