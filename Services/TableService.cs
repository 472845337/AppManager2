using AppManager2.CustomEnum;
using AppManager2.DataBase;
using AppManager2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace AppManager2.Services
{
    public class TableService<T> where T : TableData, new()
    {
        protected static string DbName = "AppManager.db";
        protected static SQLiteHelper _dbHelper;

        static TableService()
        {
            _dbHelper = DataBase.SQLiteFactory.GetSqLiteHelper(DbName, null);
        }

        /// <summary>
        /// 根据实体上的 Table/TableParam 特性自动建表（表不存在时创建）
        /// </summary>
        public virtual void EnsureTable()
        {
            var buf = new System.Text.StringBuilder();
            buf.Append("CREATE TABLE IF NOT EXISTS ").Append(GetTableName()).Append(" (");

            foreach (var prop in typeof(T).GetProperties())
            {
                var paramAttr = prop.GetCustomAttributes(typeof(TableParam), false).FirstOrDefault() as TableParam;
                if (paramAttr == null) continue;

                buf.Append(paramAttr.Name).Append(" ").Append(paramAttr.Type);
                if (paramAttr.IsPrimaryKey)
                {
                    buf.Append(" PRIMARY KEY AUTOINCREMENT");
                }
                buf.Append(",");
            }
            buf.Remove(buf.Length - 1, 1);
            buf.Append(")");

            _dbHelper.ExecuteNonQuery(buf.ToString(), null);
        }

        public virtual long Insert(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            entity.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            entity.UpdateTime = entity.CreateTime;

            var dict = ObjectToDict(entity);
            return _dbHelper.InsertData(GetTableName(), dict);
        }

        public virtual bool Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.Id == null) throw new ArgumentException("Id cannot be null");

            entity.UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var dict = ObjectToDict(entity);
            var whereParams = new[] { new SQLiteParameter("id", entity.Id) };
            return _dbHelper.Update(GetTableName(), dict, "id=@id", whereParams) > 0;
        }

        public virtual bool Delete(long? id)
        {
            if (id == null) throw new ArgumentException("Id cannot be null");

            var whereParams = new[] { new SQLiteParameter("id", id) };
            return _dbHelper.Delete(GetTableName(), "id=@id", whereParams) > 0;
        }

        public virtual List<T> SelectList(string where = null, SQLiteParameter[] whereParams = null, string orderBy = "id", string orderType = "desc")
        {
            string sql = "SELECT * FROM " + GetTableName();
            if (!string.IsNullOrEmpty(where))
            {
                sql += " WHERE " + where;
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                sql += " ORDER BY " + orderBy + " " + orderType;
            }

            var ds = _dbHelper.ExecuteDataSet(sql, whereParams);
            if (ds == null || ds.Tables.Count == 0)
                return new List<T>();

            var list = new List<T>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                list.Add(RowToObject(row));
            }
            return list;
        }

        public virtual T SelectById(long? id)
        {
            if (id == null) return null;

            var whereParams = new[] { new SQLiteParameter("id", id) };
            var list = SelectList("id=@id", whereParams);
            return list.Count > 0 ? list[0] : null;
        }

        public virtual int Clear()
        {
            return _dbHelper.ExecuteNonQuery("DELETE FROM " + GetTableName(), null);
        }

        public virtual bool Vacuum()
        {
            return _dbHelper.Vacuum();
        }

        protected virtual string GetTableName()
        {
            var type = typeof(T);
            var tableAttr = type.GetCustomAttributes(typeof(Table), false).FirstOrDefault() as Table;
            return tableAttr?.Name ?? type.Name.ToLower();
        }

        protected virtual T RowToObject(DataRow row)
        {
            var obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var paramAttr = prop.GetCustomAttributes(typeof(TableParam), false).FirstOrDefault() as TableParam;
                if (paramAttr == null) continue;

                string columnName = paramAttr.Name;
                if (!row.Table.Columns.Contains(columnName)) continue;

                var value = row[columnName];
                if (value == DBNull.Value) continue;

                try
                {
                    if (prop.PropertyType == typeof(string))
                        prop.SetValue(obj, value.ToString());
                    else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                        prop.SetValue(obj, Convert.ToInt32(value));
                    else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
                        prop.SetValue(obj, Convert.ToInt64(value));
                    else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                        prop.SetValue(obj, Convert.ToBoolean(value));
                    else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
                        prop.SetValue(obj, Convert.ToDouble(value));
                }
                catch { }
            }

            return obj;
        }

        protected virtual Dictionary<string, object> ObjectToDict(T entity)
        {
            var dict = new Dictionary<string, object>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var paramAttr = prop.GetCustomAttributes(typeof(TableParam), false).FirstOrDefault() as TableParam;
                if (paramAttr == null) continue;

                string columnName = paramAttr.Name;
                var value = prop.GetValue(entity);
                
                if (value != null)
                    dict[columnName] = value;
            }

            return dict;
        }
    }
}
