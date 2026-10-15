using AppManager2.CustomEnum;

namespace AppManager2.Models {
    [Table("t_app")]
    public class AppRecord : TableData {
        [TableParam("section", "VARCHAR")]
        public string Section { get; set; }

        [TableParam("type", "VARCHAR")]
        public string Type { get; set; }

        [TableParam("title", "VARCHAR")]
        public string Title { get; set; }

        [TableParam("app_path", "VARCHAR")]
        public string App { get; set; }

        [TableParam("stop_path", "VARCHAR")]
        public string StopApp { get; set; }

        [TableParam("jdk_path", "VARCHAR")]
        public string Jdk { get; set; }

        [TableParam("port", "VARCHAR")]
        public string Port { get; set; }

        [TableParam("heart_beat", "VARCHAR")]
        public string HeartBeat { get; set; }

        [TableParam("actuator", "VARCHAR")]
        public string Actuator { get; set; }

        [TableParam("ext", "VARCHAR")]
        public string Ext { get; set; }

        [TableParam("jvm_param", "VARCHAR")]
        public string Param { get; set; }

        [TableParam("env_param", "VARCHAR")]
        public string Env { get; set; }

        [TableParam("profiles", "VARCHAR")]
        public string Profiles { get; set; }

        [TableParam("group_name", "VARCHAR")]
        public string Group { get; set; }

        [TableParam("is_warn", "BIT")]
        public bool IsWarn { get; set; }

        [TableParam("warn_url", "VARCHAR")]
        public string WarnUrl { get; set; }

        [TableParam("auto_run", "BIT")]
        public bool AutoRun { get; set; }

        [TableParam("is_print_log", "BIT")]
        public bool IsPrintLog { get; set; }

        [TableParam("remark", "VARCHAR")]
        public string Remark { get; set; }

        [TableParam("sort", "INTEGER")]
        public int Sort { get; set; }
    }
}
