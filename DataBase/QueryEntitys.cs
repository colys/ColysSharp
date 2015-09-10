using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColysSharp.DataBase
{
    /// <summary>
    /// 数据库字段的用途。
    /// </summary>
    public enum DBFieldUsage
    {
        /// <summary>
        /// 未定义。
        /// </summary>
        None = 0x00,
        /// <summary>
        /// 用于主键。
        /// </summary>
        PrimaryKey = 0x01,
        /// <summary>
        /// 用于唯一键。
        /// </summary>
        ForeignKey = 0x02,
        /// <summary>
        /// 由系统控制该字段的值。
        /// </summary>
        MarkDelete = 0x04,
        /// <summary>
        /// 不是本张表的
        /// </summary>
        NoField=0X08
    }
   
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class DBFieldAttribute : Attribute
    {
        public string FieldName { get; set; }

        public DBFieldUsage Usage { get; set; }

        public string ForeignTable{get;set;}

        public string ForeignTableField { get; set; }

    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class DBTableAttribute : Attribute
    {
        public string TableName { get; set; }

    }

    public class QueryEntityInstance {

        
        public QueryEntityConfig Config { get; set; }

        


        public QueryParam Param { get; set; }       


              
    }

    public class Pagger { public int PageSize; 
        /// <summary>
        /// 页码,从1开始
        /// </summary>
        public int PageIndex; public int Total; }

    /// <summary>
    /// 查询实体，比如：{table:[t1,t2] ,fields:{name:[""a.name"", ""a.name like '{0}' ""]},page:{pageSize:2,pageIndex:1}}
    /// </summary>
    public class QueryEntityConfig
    {
       

        public bool RequreLogin { get; set; }

        public bool RequreAdmin { get; set; }

        public bool RequerSelf { get; set; }

        public bool RequerNotSelf { get; set; }

        /// <summary>
        /// 不允许更新的列的集合
        /// </summary>
        public string[] NotAllows { get; set; }

        /// <summary>
        /// 就算是空值也会更新的列的集合
        /// </summary>
        public string[] IgnoreNull { get; set; }

        /// <summary>
        /// 集合：列的配置，每项为数组[select表达式,where表达式]
        /// </summary>
        public Dictionary<string, string[]> Fields { get; set; }
                

        public string[] Items { get; set; }

        public QueryEntityConfig[] QItems { get; set; }

        public string TableSql { get; set; }
        
        /// <summary>
        /// 创建与实体同名的配置，在末配置的时候使用
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="queryEntity"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public static QueryEntityConfig Create( string whereJson, string permission)
        {            
            QueryEntityConfig qe = new QueryEntityConfig();
            qe.Fields = new Dictionary<string, string[]>();
            //foreach (KeyValuePair<string, string> ki in queryEntity)
            //{
            //    string[] queryValArr = ki.Value.Split(',');
            //    if (queryValArr.Length > 1)
            //        qe.Fields.Add(ki.Key, new string[] { ki.Key, ki.Key + " between '{0}' and '{1}'" });
            //    else
            //        qe.Fields.Add(ki.Key, new string[] { ki.Key, ki.Key + " = '{0}'" });
            //}            
            return qe;
        }

        //public void ParseExec()
        //{
        //    QItems = new QueryEntityConfig[Items.Length];
        //    for (int i = 0; i < Items.Length; i++)
        //    {
        //        QueryEntityConfig qItemEntity = null;
        //        string permission = Items[i];
        //        queryMapping.TryGetValue(permission, out qItemEntity);
        //        if (qItemEntity == null) throw new Exception("");
        //        QItems[i] = qItemEntity;
        //    }


        //}

        


    }
    public class ExecEntity
    {
        public String table;
        public DBAction action;
        public String[] fields;
        public String[] values;
        public String where;
    }
    public enum DBAction { Add = 0, Delete = 2, Update = 1 }


    /// <summary>
    /// 查询参数
    /// </summary>
    public class QueryParam
    {
        public string permission;
        private string _queryField;
        public string queryField{set { _queryField =value; }}
        public Type entityType { get; set; }
        private string _entityName;
        public string entityName { set { _entityName = value; } }
        private string _whereJson;

        public List<QueryField> QueryFields;
        public string whereJson
        {
            get { return _whereJson; }
            set
            {
                _whereJson = value;
                if (ClientWhere != null) throw new ClientException("ClientWhere已经有值，不允许再给whereJson赋值！");
                ClientWhere = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(_whereJson);
            }
        }
        public Dictionary<string, string> ClientWhere;
        public String orderBy;
        public Pagger Page { get; set; }
        /// <summary>
        /// 初始化Type，外部访问都用entityType
        /// </summary>
        /// <param name="format"></param>
        public void InitParam(string format)
        {
            if (entityType == null)
            {
                string typeFullName = string.Format(format, _entityName);
                entityType = Type.GetType(typeFullName);
                if (entityType == null) throw new ClientException(typeFullName + "类型找不到!");
            }
            if (ClientWhere == null && whereJson!=null) ClientWhere = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(whereJson);
            if (QueryFields == null && _queryField != null)
            {
                QueryFields = new List<QueryField>();
                //解析查询列                
                if (string.IsNullOrEmpty(_queryField) || _queryField == "*")//不传查询内容，或者为*
                {                    
                    foreach (DBContext.CustomerProperty cusPro in DBContext.GetTypeCustomerPropertys(entityType))
                    {
                        bool isDbfield = true;
                        if (cusPro.Attr != null && cusPro.Attr.Usage == DBFieldUsage.NoField) isDbfield = false;
                        if (isDbfield && cusPro.Info.PropertyType.Namespace == "System")
                            QueryFields.Add(new QueryField() { Attr = cusPro.Attr, Info = cusPro.Info });
                    }
                }
                else
                {
                    foreach (string fieldName in _queryField.Split(',')) {
                        QueryField qf = new QueryField();
                        DBContext.CustomerProperty cusPro = DBContext.GetCustomerProperty(entityType, fieldName);
                        if (cusPro == null) { qf.CustomValue = fieldName; }//自定义字段，不在实体属性中，比如convert()之类
                        else { qf.Attr = cusPro.Attr; qf.Info = cusPro.Info; }
                        QueryFields.Add(qf);
                    }
                }
            }
            if (QueryFields !=null && QueryFields.Count == 0) throw new ClientException("查询列为空");
            //where
            if (ClientWhere == null) ClientWhere = new Dictionary<string, string>();
            if (ClientWhere.Count == 0)
            {
                //没传where，是下是否有标记删除，有则带入where
                DBContext.CustomerProperty cp = DBContext.GetUsaAttrOnce(entityType, DBFieldUsage.MarkDelete);
                if (cp != null) ClientWhere.Add(cp.Info.Name, "<> -1");
            }
            //order by
            if (!string.IsNullOrEmpty(orderBy))
            {
                string tempStr = orderBy.Replace("asc", "").Replace("desc", "");
                foreach (string orderItem in tempStr.Split(','))
                {
                    string item = orderItem.Trim();
                    DBContext.CustomerProperty cp = DBContext.GetCustomerProperty(entityType, item);
                    if (cp == null) throw new Exception("order by 中的" + item + " 在" + entityType.Name + "中不存在");
                    orderBy = orderBy.Replace(item, cp.GetDBFieldName());
                }
            }
        }
       
        public void SetWhere(object obj)
        {
            ClientWhere = new Dictionary<string, string>();
            foreach (PropertyInfo pi in obj.GetType().GetProperties())
            {
                object val = pi.GetValue(obj, null);
                if (val == null) val = " is null ";
                ClientWhere.Add(pi.Name, val.ToString());
            }
        }

        
    }

    public class QueryField : DBContext.CustomerProperty
    {
        public string CustomValue { get; set; }
    }
}
