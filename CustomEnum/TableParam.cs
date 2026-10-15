using System;

namespace AppManager2.CustomEnum {
    [AttributeUsage(AttributeTargets.Property)]
    public class TableParam : Attribute {
        public string Name { get; }
        public string Type { get; }
        public bool IsPrimaryKey { get; }

        public TableParam(string name, string type, bool isPrimaryKey = false) {
            Name = name;
            Type = type;
            IsPrimaryKey = isPrimaryKey;
        }
    }
}
