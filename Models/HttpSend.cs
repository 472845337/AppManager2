using AppManager2.CustomEnum;
using System.Collections.Generic;

namespace AppManager2.Models {
    [Table("http_send")]
    public class HttpSend : TableData {
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

        /// <summary>是否收藏（常用请求钉住，去重/清空时不删除）；非空置bool，老记录NULL读出为false</summary>
        [TableParam("is_favorite", "BIT")]
        public bool IsFavorite { get; set; }

        /// <summary>收藏的功能名称（可为空，列表回退显示Url；取消收藏时保留，便于再次收藏回填）</summary>
        [TableParam("favorite_name", "VARCHAR")]
        public string FavoriteName { get; set; }

        public List<HttpSendParam> ParamList { get; set; }
    }
}
