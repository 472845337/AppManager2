using AppManager2.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AppManager2.Services
{
    public class HttpSendParamService : TableService<HttpSendParam>
    {
        private static HttpSendParamService _instance;
        public static HttpSendParamService Instance => _instance ??= new HttpSendParamService();

        private HttpSendParamService()
        {
            EnsureTable();
        }

        /// <summary>
        /// 建表后补齐历史库缺失的列（param_type为后加的，老库需ALTER）
        /// </summary>
        public override void EnsureTable()
        {
            base.EnsureTable();
            var ds = _dbHelper.ExecuteDataSet("PRAGMA table_info(http_send_param)", null);
            var hasParamType = false;
            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                {
                    if ("param_type".Equals(Convert.ToString(row["name"]), System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasParamType = true;
                        break;
                    }
                }
            }
            if (!hasParamType)
            {
                _dbHelper.ExecuteNonQuery("ALTER TABLE http_send_param ADD COLUMN param_type VARCHAR", null);
            }
        }

        public List<HttpSendParam> GetByHttpSendId(long? httpSendId)
        {
            if (httpSendId == null) return new List<HttpSendParam>();

            var whereParams = new[] { new SQLiteParameter("http_send_id", httpSendId) };
            return SelectList("http_send_id=@http_send_id", whereParams);
        }

        public bool DeleteByHttpSendId(long? httpSendId)
        {
            if (httpSendId == null) return false;

            var whereParams = new[] { new SQLiteParameter("http_send_id", httpSendId) };
            return _dbHelper.ExecuteNonQuery("DELETE FROM " + GetTableName() + " WHERE http_send_id=@http_send_id", whereParams) >= 0;
        }
    }
}
