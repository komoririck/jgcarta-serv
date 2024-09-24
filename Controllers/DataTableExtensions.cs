using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace hololive_oficial_cardgame_server.Controllers
{
    public static class DataTableExtensions
    {
        public static List<T> ToList<T>(this DataTable dataTable) where T : new()
        {
            var list = new List<T>();

            foreach (DataRow row in dataTable.Rows)
            {
                var obj = new T();
                foreach (DataColumn column in dataTable.Columns)
                {
                    PropertyInfo prop = typeof(T).GetProperty(column.ColumnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null && row[column] != DBNull.Value)
                    {
                        prop.SetValue(obj, Convert.ChangeType(row[column], prop.PropertyType), null);
                    }
                }
                list.Add(obj);
            }

            return list;
        }
    }
}
