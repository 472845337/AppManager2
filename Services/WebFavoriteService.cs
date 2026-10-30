using AppManager2.Models;
using SqliteUtils;
using System.Collections.Generic;

namespace AppManager2.Services {
    /// <summary>
    /// web_favorite 表的 SQLite 访问服务。
    ///
    /// 通用增删改查由 SqliteUtils.TableService&lt;T&gt; 提供（自动建表/补列、Insert/Update/Delete/Clear/Vacuum），
    /// 这里只保留按 url 的查询与删除。
    /// </summary>
    public class WebFavoriteService : TableService<WebFavorite> {

        /// <summary>
        /// 显式的静态构造函数用来告诉C#编译器在其内容实例化之前不要标记其类型
        /// </summary>
        static WebFavoriteService() { }

        private WebFavoriteService() { }

        public static WebFavoriteService Instance { get; } = new WebFavoriteService();

        /// <summary>
        /// 按 id 倒序查询全部收藏（后收藏的在前）
        /// </summary>
        public List<WebFavorite> GetAll() {
            return SelectList(new WebFavorite { OrderBy = "id desc" });
        }

        /// <summary>
        /// 按url查询收藏记录，未收藏返回null
        /// </summary>
        public WebFavorite GetByUrl(string url) {
            if (string.IsNullOrEmpty(url)) return null;
            return SelectList(new WebFavorite { Url = url }).Find(f => url == f.Url);
        }

        /// <summary>
        /// 按url删除收藏，返回是否删到了数据
        /// </summary>
        public bool DeleteByUrl(string url) {
            if (string.IsNullOrEmpty(url)) return false;
            return Delete(new WebFavorite { Url = url }) > 0;
        }
    }
}
