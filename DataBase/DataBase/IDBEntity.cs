using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ColysSharp.DataBase
{
    public class IDBEntity
    {
        public object GetPrimaryValue()
        {
            DBContext.CustomerProperty property = DBContext.GetUsaAttrOnce(this.GetType(), DBFieldUsage.PrimaryKey);
            return property.Info.GetValue(this, null);
        }

        public void ParseFromTable(DataTable dt)
        {
            if (dt == null) throw new Exception("ParseFromTable dt is null");
            if (dt.Rows.Count == 0) return;
            DataRow dr = dt.Rows[0];
            foreach (DBContext.CustomerProperty cp in DBContext.GetTypeCustomerPropertys(this.GetType()))
            {
                string fieldName = cp.GetDBFieldName();
                if (dt.Columns.IndexOf(fieldName) > -1) SetValueFromDB(cp.Info, dr[fieldName]);
            }
        }

        public void ParseFromTableRow(DataRow dr)
        {                       
            foreach (DBContext.CustomerProperty cp in DBContext.GetTypeCustomerPropertys(this.GetType()))
            {
                string fieldName = cp.GetDBFieldName();
                if (dr.Table.Columns.IndexOf(fieldName) > -1) SetValueFromDB(cp.Info, dr[fieldName]);
            }
        }


        public void SetValueFromDB(PropertyInfo info, object dbVal)
        {
            if (DBNull.Value.Equals(dbVal)) return;
            if (!info.PropertyType.IsGenericType)
            {
                info.SetValue(this, Convert.ChangeType(dbVal, info.PropertyType), null);
            }
            else
            {
                //泛型Nullable<>
                Type genericTypeDefinition = info.PropertyType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>))
                {
                    info.SetValue(this, Convert.ChangeType(dbVal, Nullable.GetUnderlyingType(info.PropertyType)), null);
                }
            }
            //info.SetValue(this, convertVal, null);
        }
    }

}
