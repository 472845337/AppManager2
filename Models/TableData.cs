using SqliteUtils;
using System;

namespace AppManager2.Models {
    /// <summary>
    /// 数据库实体的公共基类。
    /// 继承通用库 SqliteUtils.SqliteTableData（提供 OrderBy / KeyColumn / KeyValue / OnBeforeInsert / OnBeforeUpdate）。
    /// </summary>
    public abstract class TableData : SqliteTableData {
        [TableParam(true, "id", "INTEGER")]
        public long? Id { get; set; }

        [TableParam("create_time", "VARCHAR")]
        public string CreateTime { get; set; }

        [TableParam("update_time", "VARCHAR")]
        public string UpdateTime { get; set; }

        /// <summary>
        /// 主键值：主键列名沿用通用库默认的 "id"，值取 Id
        /// </summary>
        public override object KeyValue => Id;

        /// <summary>
        /// 插入前回调（由 SqliteUtils.TableService.Insert 调用）：写入创建时间与更新时间
        /// </summary>
        public override void OnBeforeInsert() {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CreateTime = now;
            UpdateTime = now;
        }

        /// <summary>
        /// 更新前回调（由 SqliteUtils.TableService.Update 调用）：刷新更新时间
        /// </summary>
        public override void OnBeforeUpdate() {
            UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
