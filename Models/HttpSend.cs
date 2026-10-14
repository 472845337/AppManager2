using AppManager2.CustomEnum;
using System.Collections.Generic;

namespace AppManager2.Models
{
    [Table("http_send")]
    public class HttpSend : TableData
    {
        [TableParam("url", "VARCHAR")]
        public string Url { get; set; }

        [TableParam("type", "VARCHAR")]
        public string Type { get; set; }

        [TableParam("expect_100_continue", "BIT")]
        public bool? Expect100Continue { get; set; }

        [TableParam("content_type", "VARCHAR")]
        public string ContentType { get; set; }

        [TableParam("result", "VARCHAR")]
        public string Result { get; set; }

        [TableParam("code", "VARCHAR")]
        public string Code { get; set; }

        public List<HttpSendParam> ParamList { get; set; }
    }
}
