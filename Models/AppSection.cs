namespace AppManager2.Models
{
    public class AppSection
    {
        public string Type { get; set; }
        public string Section { get; set; }
        public string Title { get; set; }
        public string App { get; set; }
        public string StopApp { get; set; }
        public string Jdk { get; set; }
        public bool IsPrintLog { get; set; }
        public string Port { get; set; }
        public string HeartBeat { get; set; }
        public bool IsRunning { get; set; }
        public short RunStat { get; set; }
        public string Actuator { get; set; }
        public string Ext { get; set; }
        public string Param { get; set; }
        public string Env { get; set; }
        public string Profiles { get; set; }
        public string Group { get; set; }
        public bool IsWarn { get; set; }
        public string WarnUrl { get; set; }
        public bool AutoRun { get; set; }
        public string Remark { get; set; }
        public int Sort { get; set; }
    }
}
