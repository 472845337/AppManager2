using AppManager2.Models;
using SqliteUtils;
using System.Collections.Generic;

namespace AppManager2.Services {
    /// <summary>
    /// http_send_param 表的 SQLite 访问服务。
    ///
    /// 通用增删改查由 SqliteUtils.TableService&lt;T&gt; 提供，这里只保留按 http_send_id 的查询与删除。
    /// </summary>
    public class HttpSendParamService : TableService<HttpSendParam> {

        /// <summary>
        /// 显式的静态构造函数用来告诉C#编译器在其内容实例化之前不要标记其类型
        /// </summary>
        static HttpSendParamService() { }

        private HttpSendParamService() { }

        public static HttpSendParamService Instance { get; } = new HttpSendParamService();

        /// <summary>
        /// 按所属发送记录 id 查询参数明细
        /// </summary>
        public List<HttpSendParam> GetByHttpSendId(long? httpSendId) {
            if (httpSendId == null) return new List<HttpSendParam>();
            return SelectList(new HttpSendParam { HttpSendId = httpSendId });
        }

        /// <summary>
        /// 按所属发送记录 id 删除参数明细
        /// </summary>
        /// <returns>SQL 正常执行返回 true（无匹配数据时同样为 true，与删除前的行为一致）</returns>
        public bool DeleteByHttpSendId(long? httpSendId) {
            if (httpSendId == null) return false;
            return Delete(new HttpSendParam { HttpSendId = httpSendId }) >= 0;
        }
    }
}
