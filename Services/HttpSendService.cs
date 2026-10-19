using AppManager2.Models;
using SqliteUtils;
using System.Collections.Generic;

namespace AppManager2.Services {
    /// <summary>
    /// http_send 表的 SQLite 访问服务。
    ///
    /// 通用增删改查由 SqliteUtils.TableService&lt;T&gt; 提供（自动建表/补列、Insert/Update/Delete/Clear/Vacuum），
    /// 这里只保留「主记录 + 参数明细」的组合保存与查询。
    /// </summary>
    public class HttpSendService : TableService<HttpSend> {

        /// <summary>
        /// 显式的静态构造函数用来告诉C#编译器在其内容实例化之前不要标记其类型
        /// </summary>
        static HttpSendService() { }

        private HttpSendService() { }

        public static HttpSendService Instance { get; } = new HttpSendService();

        /// <summary>
        /// 按 id 倒序查询全部发送记录（最新在前）
        /// </summary>
        public List<HttpSend> GetAll() {
            return SelectList(new HttpSend { OrderBy = "id desc" });
        }

        /// <summary>
        /// 按主键删除单条发送记录
        /// </summary>
        public bool DeleteById(long? id) {
            if (id == null) return false;
            return Delete(id.Value.ToString()) > 0;
        }

        /// <summary>
        /// 保存发送记录及其参数明细
        /// </summary>
        public bool Save(HttpSend httpSend) {
            if (httpSend == null) return false;

            // Insert 内部会回调实体的 OnBeforeInsert 写入创建/更新时间，返回自增主键
            long id = Insert(httpSend);

            if (id <= 0) return false; // 主表插入失败时不写子表，避免产生 http_send_id=0 的孤儿参数

            if (httpSend.ParamList != null && httpSend.ParamList.Count > 0) {
                foreach (var param in httpSend.ParamList) {
                    param.HttpSendId = id;
                    HttpSendParamService.Instance.Insert(param);
                }
            }

            return true;
        }

        /// <summary>
        /// 查询全部发送记录，并带出各自的参数明细
        /// </summary>
        public List<HttpSend> GetAllWithParams() {
            var list = GetAll();
            foreach (var httpSend in list) {
                httpSend.ParamList = HttpSendParamService.Instance.GetByHttpSendId(httpSend.Id);
            }
            return list;
        }
    }
}
