using System.Collections.Generic;

namespace AppManager2.DataBase
{
    internal static class SQLiteFactory
    {
        private static readonly Dictionary<string, SQLiteHelper> SqLiteHelperDic = new Dictionary<string, SQLiteHelper>();

        public static SQLiteHelper GetSqLiteHelper(string dbName, string password)
        {
            if (SqLiteHelperDic.ContainsKey(dbName))
            {
                return SqLiteHelperDic[dbName];
            }
            else
            {
                var sqliteHelper = string.IsNullOrEmpty(password)
                    ? new SQLiteHelper(dbName)
                    : new SQLiteHelper(dbName, password);
                sqliteHelper.Open();
                SqLiteHelperDic.Add(dbName, sqliteHelper);
                return sqliteHelper;
            }
        }

        public static void CloseAllSqLite()
        {
            foreach (var pair in SqLiteHelperDic)
            {
                pair.Value.Close();
            }
            SqLiteHelperDic.Clear();
        }
    }
}
