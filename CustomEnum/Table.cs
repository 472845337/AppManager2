using System;

namespace AppManager2.CustomEnum
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Table : Attribute
    {
        public string Name { get; }

        public Table(string name)
        {
            Name = name;
        }
    }
}
