using AppManager2.CustomEnum;

namespace AppManager2.Models
{
    [Table("http_send_param")]
    public class HttpSendParam : TableData
    {
        public const string TYPE_HEAD = "head";
        public const string TYPE_BODY = "body";

        [TableParam("http_send_id", "INTEGER")]
        public long? HttpSendId { get; set; }

        [TableParam("type", "VARCHAR")]
        public string Type { get; set; }

        [TableParam("param_key", "VARCHAR")]
        public string ParamKey { get; set; }

        [TableParam("param_type", "VARCHAR")]
        public string ParamType { get; set; }

        [TableParam("param_value", "VARCHAR")]
        public string ParamValue { get; set; }
    }
}
