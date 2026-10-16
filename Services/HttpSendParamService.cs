using AppManager2.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AppManager2.Services {
    public class HttpSendParamService : TableService<HttpSendParam> {
        private static HttpSendParamService _instance;
        public static HttpSendParamService Instance => _instance ??= new HttpSendParamService();

        private HttpSendParamService() {
            EnsureTable();
        }

        public List<HttpSendParam> GetByHttpSendId(long? httpSendId) {
            if (httpSendId == null) return new List<HttpSendParam>();

            var whereParams = new[] { new SQLiteParameter("http_send_id", httpSendId) };
            return SelectList("http_send_id=@http_send_id", whereParams);
        }

        public bool DeleteByHttpSendId(long? httpSendId) {
            if (httpSendId == null) return false;

            var whereParams = new[] { new SQLiteParameter("http_send_id", httpSendId) };
            return _dbHelper.ExecuteNonQuery("DELETE FROM " + GetTableName() + " WHERE http_send_id=@http_send_id", whereParams) >= 0;
        }
    }
}
