using AppManager2.Models;
using System.Collections.Generic;

namespace AppManager2.Services
{
    public class HttpSendService : TableService<HttpSend>
    {
        private static HttpSendService _instance;
        public static HttpSendService Instance => _instance ??= new HttpSendService();

        private HttpSendService()
        {
            EnsureTable();
        }

        public bool Save(HttpSend httpSend)
        {
            if (httpSend == null) return false;

            long id = Insert(httpSend);

            if (id <= 0) return false; // 主表插入失败时不写子表，避免产生 http_send_id=0 的孤儿参数

            if (httpSend.ParamList != null && httpSend.ParamList.Count > 0)
            {
                foreach (var param in httpSend.ParamList)
                {
                    param.HttpSendId = id;
                    HttpSendParamService.Instance.Insert(param);
                }
            }

            return id > 0;
        }

        public List<HttpSend> GetAllWithParams()
        {
            var list = SelectList();
            foreach (var httpSend in list)
            {
                httpSend.ParamList = HttpSendParamService.Instance.GetByHttpSendId(httpSend.Id);
            }
            return list;
        }
    }
}
