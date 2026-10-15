using AppManager2.CustomEnum;

namespace AppManager2.Models
{
    public abstract class TableData
    {
        [TableParam("id", "INTEGER", true)]
        public long? Id { get; set; }

        [TableParam("create_time", "VARCHAR")]
        public string CreateTime { get; set; }

        [TableParam("update_time", "VARCHAR")]
        public string UpdateTime { get; set; }

        public string OrderBy { get; set; }
    }
}
