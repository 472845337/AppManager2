using SqliteUtils;

namespace AppManager2.Models {
    /// <summary>
    /// 浏览器URL收藏：地址栏五角星一键收藏/取消，按url整条匹配（一条url只收藏一次）
    /// </summary>
    [Table("web_favorite")]
    public class WebFavorite : TableData {
        [TableParam("url", "VARCHAR")]
        public string Url { get; set; }

        /// <summary>收藏时的页面标题（可为空，仅用于列表悬浮提示辅助识别）</summary>
        [TableParam("title", "VARCHAR")]
        public string Title { get; set; }
    }
}
