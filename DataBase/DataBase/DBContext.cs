using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColysSharp.DataBase
{
    public class DBContext : IDisposable
    {
        private static DBContextConfig GlobalDBContextConfig;


        private DBContextConfig currentDBContextConfig;

        public DBContextConfig GetConfig()
        {

            DBContextConfig config = currentDBContextConfig == null ? GlobalDBContextConfig : currentDBContextConfig;
            if (config == null) throw new ClientException("没有相关配置，全局请使用DBContext.LoadConfig");
            return config;
        }

        /// <summary>
        /// 记录sql语句
        /// </summary>
        /// <param name="sql">语句</param>
        /// <param name="isQuery">是否仅是查询，如果只查询就不记</param>
        public static void LogSql(string sql, bool isQuery)
        {
            bool isLog = false;
            switch (GlobalDBContextConfig.LogSql)
            {
                case 2:
                    isLog = true;
                    break;
                case 1:
                    if (sql.Substring(0, sql.Length > 10 ? 10 : sql.Length).ToLower().IndexOf("select") > -1) isLog = false;
                    else isLog = true;
                    break;
            }
            if (isLog)
            {
                log4net.ILog log = log4net.LogManager.GetLogger("DataBase");
                log.Info(sql);
            }
            System.Diagnostics.Debug.WriteLine(sql);
        }

        /// <summary>
        /// 载入配置
        /// </summary>
        /// <param name="json">配置json值，可以从文件中读取完传入</param>
        /// <param name="entityTypeFormater">全局用：实体类全名格式化,比如Nd.CheckList.ArgumentDiscuss.{0}, CheckListService</param>
        public static void LoadConfig(string json, DBContextConfig config)
        {
            GlobalDBContextConfig = config;
            queryMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, QueryEntityConfig>>(json);
        }
        public static void LoadConfigFromFile(string filePath, DBContextConfig config)
        {
            string json = ReadFile(filePath);
            GlobalDBContextConfig = config;
            if (string.IsNullOrEmpty(json)) queryMapping = new Dictionary<string, QueryEntityConfig>();
            else queryMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, QueryEntityConfig>>(json);
        }

        public static string ReadFile(string file)
        {

            try
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(file, Encoding.UTF8);
                string str = reader.ReadToEnd();
                reader.Close();
                return str;
            }
            catch (Exception ex)
            {
                throw new ClientException("读取页面权限配置文件出错" + ex.Message);
            }

        }


        public static Dictionary<string, QueryEntityConfig> queryMapping;

        QueryEntityConfig qEntity = null;

        static Dictionary<Type, List<CustomerProperty>> typeCustomerPropertyDic = new Dictionary<Type, List<CustomerProperty>>();
        /// <summary>
        /// 获取实体类型的自定义属性集合
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static List<CustomerProperty> GetTypeCustomerPropertys(Type t)
        {
            List<CustomerProperty> lst;
            if (!typeCustomerPropertyDic.TryGetValue(t, out lst))
            {
                lst = new List<CustomerProperty>();
                foreach (PropertyInfo proInfo in t.GetProperties())
                {
                    if (proInfo.PropertyType.Namespace != "System") continue;
                    object[] fieldAttrs = proInfo.GetCustomAttributes(typeof(DBFieldAttribute), true);
                    if (fieldAttrs.Length > 0) lst.Add(new CustomerProperty() { Info = proInfo, Attr = fieldAttrs[0] as DBFieldAttribute });
                    else lst.Add(new CustomerProperty() { Info = proInfo });
                }
                if (lst.Count == 0) throw new ClientException(t.Name + "没有定义任何属性");
                typeCustomerPropertyDic.Add(t, lst);
            }
            return lst;
        }
        /// <summary>
        /// 获取类型的某个属性及自定义属性
        /// </summary>
        /// <param name="t">类型</param>
        /// <param name="name">属性名</param>
        /// <returns></returns>
        public static CustomerProperty GetCustomerProperty(Type t, string name)
        {
            foreach (CustomerProperty per in GetTypeCustomerPropertys(t))
            {
                if (per.Info.Name == name) return per;
            }
            return null;
        }

      
        IDBAccess dbAccess;

        //internal Type GetEntityType(string name)
        //{
        //    string typeName = string.Format(GetConfig().EntityTypeFormat, name);
        //    Type t = Type.GetType(typeName);
        //    if (t == null) throw new ClientException(typeName + "类型找不到!");
        //    return t;
        //}

        /// <summary>
        /// 数据操作类
        /// </summary>
        /// <param name="dbType">数据库类型(sqlserver,mysql,oracle)</param>
        /// <param name="conn">连接串</param>
        /// <param name="formater">实体类全名的格式化，空则使用全局的</param>
        public DBContext(string dbType, string conn, string formater = null)
        {
            currentDBContextConfig = new DBContextConfig
            {
                ConnectionString = conn,
                DatabaseType = dbType,
                EntityTypeFormat = formater == null ? GlobalDBContextConfig.EntityTypeFormat : formater
            };
            CreateDBAccess(false);
        }

        public DBContext(bool useTrans, string formater = null)
        {
            if (formater != null) currentDBContextConfig = new DBContextConfig
                        {
                            ConnectionString = GlobalDBContextConfig.ConnectionString,
                            DatabaseType = GlobalDBContextConfig.DatabaseType,
                            EntityTypeFormat = formater == null ? GlobalDBContextConfig.EntityTypeFormat : formater
                        };
            CreateDBAccess(useTrans);
        }

        public DBContext(string formater = null)
        {

            if (formater != null) currentDBContextConfig = new DBContextConfig
            {
                ConnectionString = GlobalDBContextConfig.ConnectionString,
                DatabaseType = GlobalDBContextConfig.DatabaseType,
                EntityTypeFormat = formater == null ? GlobalDBContextConfig.EntityTypeFormat : formater
            };
            CreateDBAccess(false);
        }

        private void CreateDBAccess(bool useTrans)
        {
            switch (GetConfig().DatabaseType.ToLower())
            {
                case "mysql":
                case "mysql.data.mysqlclient":
                    dbAccess = new SqlClient.MySqlAccess();
                    break;
                default:
                    dbAccess = new SqlClient.SqlAccess();
                    break;
            }
            dbAccess.Create(GetConfig().ConnectionString, useTrans);
        }

        string stepMsg = null;
        /// <summary>
        /// 获取parent字段的数据库列名,客户端树加载用
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public CustomerProperty GetParentCustomerProperty(QueryParam param)
        {            
            param.InitParam(GetConfig().EntityTypeFormat);            
            DBTableAttribute tableAttr = GetTableAttr(param.entityType);
            foreach (CustomerProperty pro in GetUsaAttr(param.entityType, DBFieldUsage.ForeignKey))
            {
                if (pro.Attr.ForeignTable == tableAttr.TableName)
                {
                    return pro;
                }
            }
            throw new ClientException("类型" + param.entityType.Name + "没有指定parent外键");
        }
        public IDBEntity DoGetOne(string permission, string entityName, string primarkVal)
        {
            try
            {
                string typeFullName = string.Format(GetConfig().EntityTypeFormat, entityName);
                Type entityType = Type.GetType(typeFullName);
                QueryParam param = new QueryParam() { entityType = entityType, permission = permission, queryField = "*" };                
                CustomerProperty attr = GetUsaAttrOnce(param.entityType, DBFieldUsage.PrimaryKey);                
                param.whereJson = "{" + attr.Info.Name + ":'" + primarkVal + "'}";
                DataTable dt = DoQuery(param);
                if (dt.Rows.Count == 0) return null;
                stepMsg = "查询结束，Create " + param.entityType.Name + " Instance";
                IDBEntity entity = (IDBEntity)System.Activator.CreateInstance(param.entityType, false);
                stepMsg = "ParseFromTable";
                entity.ParseFromTable(dt);
                return entity;
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        [Obsolete("请使用QuerySingle")]
        public T DoGetOne<T>(string primarkVal, string permission = "") where T : IDBEntity
        {
            return (T)DoGetOne(permission, typeof(T).Name, primarkVal);
        }
        [Obsolete("请使用QuerySingle")]
        public T DoGetOne<T>(int primarkVal, string permission = "") where T : IDBEntity
        {
            return (T)DoGetOne(permission, typeof(T).Name, primarkVal.ToString());
        }

        public T QuerySingle<T>(string primarkVal, string permission = "") where T : IDBEntity
        {
            return (T)DoGetOne(permission, typeof(T).Name, primarkVal);
        }

        public T QuerySingle<T>(int primarkVal, string permission = "") where T : IDBEntity
        {
            return (T)DoGetOne(permission, typeof(T).Name, primarkVal.ToString());
        }

        public T QuerySingle<T>(object where, string permission = "") where T : IDBEntity
        {
            if (where == null) return null;
            string whereJson = Newtonsoft.Json.JsonConvert.SerializeObject(where);
            try
            {
                Type entityType = typeof(T);
                CustomerProperty attr = GetUsaAttrOnce(entityType, DBFieldUsage.PrimaryKey);
                QueryParam param = new QueryParam() { entityType = entityType, permission = permission, queryField = "*", whereJson = whereJson };
                DataTable dt = DoQuery(param);
                if (dt.Rows.Count == 0) return null;
                stepMsg = "查询结束，Create " + entityType.Name + " Instance";
                T entity = System.Activator.CreateInstance(entityType, false) as T;
                stepMsg = "ParseFromTable";
                entity.ParseFromTable(dt);
                return entity;
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        QueryEntityInstance queryInstance;
        public QueryEntityInstance GetInstance() { return queryInstance; }

        private void SetQueryEntityConfig(QueryParam param)
        {
            if (queryMapping == null) throw new ClientException("未初始化,请调用DBContext.LoadConfig");
            if (param.permission != null) queryMapping.TryGetValue(param.permission, out qEntity);
            if (qEntity == null) { stepMsg = "Create A New QueryEntity"; qEntity = QueryEntityConfig.Create(param.whereJson, param.permission); }
            if (qEntity.Fields == null) qEntity.Fields = new Dictionary<string, string[]>();
            param.InitParam(GetConfig().EntityTypeFormat);
            if (queryInstance == null || queryInstance.Param != param) queryInstance = new QueryEntityInstance() { Config = qEntity, Param = param };
        }

        /// <summary>
        /// 通用查询,多表关联以及条件表达式请在json文件中配置
        /// </summary>
        /// <param name="param">查询参数（permission:权限和表达式配置,queryField:查询的字段，客户端实体的名称，逗号隔开,entityType:查询的对象,whereField：查询条件json，比如{aa:'1',dtime:'1,2'} ）</param>        
        public DataTable DoQuery(QueryParam param)
        {
            try
            {
                stepMsg = "解析QueryParam";
                SetQueryEntityConfig(param);                
                return ParseQuery(queryInstance, param.entityType);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        public List<IDBEntity> DoQueryEntity(QueryParam param)
        {
            try
            {
                stepMsg = "解析QueryParam";
                SetQueryEntityConfig(param);                
                DataTable dt= ParseQuery(queryInstance, param.entityType);
                List<IDBEntity> lst = new List<IDBEntity>();
                foreach (DataRow dr in dt.Rows)
                {
                    IDBEntity entity = (IDBEntity)System.Activator.CreateInstance(param.entityType, false);
                    entity.ParseFromTableRow(dr);
                    lst.Add(entity);
                }
                return lst;
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        public DataTable QueryTable<T>(object where, string permission = null) where T : IDBEntity
        {
            try
            {
                stepMsg = "解析where json";
                string whereJson = Newtonsoft.Json.JsonConvert.SerializeObject(where);
                QueryParam param = new QueryParam() { entityType = typeof(T), permission = permission, whereJson = whereJson };
                stepMsg = "解析QueryParam";
                SetQueryEntityConfig(param);                
                return ParseQuery(queryInstance, param.entityType);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        public DataTable QueryTable(string entityName, object where, string permission = null)
        {
            try
            {
                stepMsg = "解析where json";
                string whereJson = Newtonsoft.Json.JsonConvert.SerializeObject(where);
                QueryParam param = new QueryParam() { entityName = entityName, permission = permission, whereJson = whereJson };
                stepMsg = "解析QueryParam";
                SetQueryEntityConfig(param);                
                return ParseQuery(queryInstance, param.entityType);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }


        public List<T> QueryMany<T>(object where) where T : IDBEntity
        {
            try
            {
                stepMsg = "解析where json";
                string whereJson = Newtonsoft.Json.JsonConvert.SerializeObject(where);
                QueryParam param = new QueryParam() { entityType = typeof(T), permission = null, whereJson = whereJson };
                return QueryMany<T>(param);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 查询表内容，返回树弄列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where">查询条件</param>
        /// <param name="parentVal">父节点的值，空则查询全部</param>
        /// <returns></returns>
        public List<T> QueryTree<T>(string permission, object where, string orderBy, object parentVal = null) where T : ITreeDBEntity
        {
            QueryParam param = new QueryParam() { permission = permission, queryField = "*", entityType = typeof(T), orderBy = orderBy };
            param.SetWhere(where);

            ColysSharp.DataBase.DBContext.CustomerProperty cp = GetParentCustomerProperty(param);
            if (string.IsNullOrEmpty(cp.Attr.ForeignTableField)) throw new ClientException(cp.Info.Name + "没有定义ForeignTableField");
            string parentField = cp.GetDBFieldName();

            if (parentVal == null || parentVal.Equals("0") || parentVal.Equals(0))
            {
                parentVal = "($field is null or $field = 0)";
            }
            if (param.ClientWhere.ContainsKey(parentField)) throw new ClientException("查询条件中不能包含" + parentField);
            param.ClientWhere.Add(parentField, parentVal.ToString());
            List<IDBEntity> lst = DoQueryEntity(param);
            foreach (IDBEntity entity in lst)
            {
                SetTreeChildren(entity, cp);

            }
            return lst as List<T>;
        }
        /// <summary>
        /// 查询表内容，返回树弄列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where">查询条件</param>
        /// <param name="parentVal">父节点的值，空则查询全部</param>
        /// <returns></returns>
        public List<IDBEntity> QueryTree(QueryParam param, object parentVal = null)
        {
            
            ColysSharp.DataBase.DBContext.CustomerProperty cp = GetParentCustomerProperty(param);
            if (string.IsNullOrEmpty(cp.Attr.ForeignTableField)) throw new ClientException(cp.Info.Name + "没有定义ForeignTableField");
            string parentField = cp.GetDBFieldName();

            if (parentVal == null || parentVal.Equals("0") || parentVal.Equals(0))
            {
                parentVal = "($field is null or $field = 0)";
            }
            if (param.ClientWhere.ContainsKey(parentField)) throw new ClientException("查询条件中不能包含" + parentField);
            param.ClientWhere.Add(parentField, parentVal.ToString());
            List<IDBEntity> lst = DoQueryEntity(param);
            foreach (IDBEntity entity in lst)
            {
                SetTreeChildren(entity, cp);

            }
            return lst;
        }

        //查询子级树
        private void SetTreeChildren(IDBEntity entity, ColysSharp.DataBase.DBContext.CustomerProperty cp)
        {
            ColysSharp.DataBase.DBContext.CustomerProperty primaryCodeCP = GetPropertyByDBName(cp.Attr.ForeignTableField);
            object primaryCode = primaryCodeCP.Info.GetValue(entity, null);
            ITreeDBEntity treeEntity = entity as ITreeDBEntity;
            if (treeEntity == null) throw new ClientException(entity.GetType().Name + "不是ITreeDBEntity类型，或者查询数据转换为空");
            queryInstance.Param.ClientWhere[cp.GetDBFieldName()] = primaryCode.ToString();
            treeEntity.Children = DoQueryEntity(queryInstance.Param);
            foreach (IDBEntity sub in treeEntity.Children)
            {
                SetTreeChildren(sub, cp);
            }
        }

        public bool Exists(string permission, string entityName, object where)
        {
            try
            {
                string whereJson = Newtonsoft.Json.JsonConvert.SerializeObject(where);
                QueryParam param = new QueryParam() { entityName = entityName, whereJson = whereJson, permission = permission, queryField = "count(0) cc" };
                DataTable dt = DoQuery(param);
                if (dt.Rows[0][0] == DBNull.Value) return false;
                return Convert.ToInt32(dt.Rows[0][0]) > 0;
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 通用查询列表,多表关联以及条件表达式请在json文件中配置
        /// </summary>
        /// <param name="param">查询参数（permission:权限和表达式配置,queryField:查询的字段，客户端实体的名称，逗号隔开,entityType:查询的对象,whereField：查询条件json，比如{aa:'1',dtime:'1,2'} ）</param>        
        public List<T> QueryMany<T>(QueryParam param) where T : IDBEntity
        {
            List<T> lst = new List<T>();
            DataTable dt = DoQuery(param);
            List<CustomerProperty> lstP = GetTypeCustomerPropertys(this.GetType());
            foreach (DataRow dr in dt.Rows)
            {
                T entity = (T)System.Activator.CreateInstance(param.entityType, false);
                foreach (CustomerProperty cp in lstP)
                {
                    entity.SetValueFromDB(cp.Info, dr[cp.GetDBFieldName()]);
                }
                lst.Add(entity);
            }
            return lst;
        }

        public string GetTableAliasName(string table, QueryEntityConfig qEntity)
        {
            stepMsg = "获取查询表的别名";
            if (qEntity.TableSql == null) throw new ClientException("TableSql");
            if (table == null) throw new ClientException("GetTableAliasName must have a parameter with value(table)");
            int pos = qEntity.TableSql.IndexOf(table);
            if (pos == -1) throw new ClientException(table + "没定义在TableSql中");
            pos += table.Length + 1;
            if (qEntity.TableSql.Length <= pos) return qEntity.TableSql;
            string[] strArr = qEntity.TableSql.Substring(pos).Split(' ');
            if (strArr[0] == "as")
            {
                return strArr[1];
            }
            else
            {
                return strArr[0];
            }
        }

        private string[] GetTables(QueryEntityConfig qEntity)
        {

            string strTables = qEntity.TableSql;
            int pos = strTables.IndexOf("join");
            ArrayList arrlist = new ArrayList();
            while (pos > -1)
            {
                strTables = strTables.Substring(pos + 5);
                string tableName;
                int pos2 = strTables.IndexOf(' ');
                if (pos2 == -1) tableName = strTables;
                else tableName = strTables.Substring(0, pos2);
                arrlist.Add(tableName);
                pos = strTables.IndexOf("join");
            }
            return arrlist.ToArray(typeof(string)) as string[];
        }

        public DataTable ParseQuery(QueryEntityInstance qei, Type entityType)
        {

            stepMsg = "进入解析查询";
            DBTableAttribute tableAttr = GetTableAttr(entityType);
            if (qei.Config.TableSql == null) qei.Config.TableSql = tableAttr.TableName;
            string tableAliasName = GetTableAliasName(tableAttr.TableName, qei.Config);
            string[] tables = GetTables(qei.Config);
            string WhereSql = "";
            stepMsg = "组织where的语句";
            foreach (KeyValuePair<string, string> ki in qei.Param.ClientWhere)
            { 
                if (string.IsNullOrEmpty(ki.Value)) continue;
                stepMsg = "解析" + ki.Key;
                string[] whereFieldValArr = ki.Value.Split(',');
                string[] configArr;
                qei.Config.Fields.TryGetValue(ki.Key, out configArr);//查找查询条件表达式的配置，没有则使用客户端

                if (configArr == null || configArr.Length < 2 || string.IsNullOrEmpty(configArr[1]))
                {                    
                    stepMsg = "解析客户端查询表达式";
                    string tempStr = ki.Value;
                    //移除引号内容，检测是否客户端直接传表达式
                    string patten = "[\"|'](.*?)[\"|']";
                    Regex regex = new Regex(patten);
                    Match m = regex.Match(tempStr);
                    while (m.Success)
                    {
                        tempStr = tempStr.Replace(m.Value, "");
                        m = m.NextMatch();
                    }
                    patten = @">|<|like\s+|=|is\s+|between\s+";
                    regex = new Regex(patten);
                    string dbFieldName = null;
                    PropertyInfo pinfo = null;
                    if (ki.Key.IndexOf('.') > -1)
                    {
                        //客户端where直接添加的表前缀,比如a.id
                        dbFieldName = ki.Key;
                    }
                    else
                    {
                        if (configArr!=null && configArr.Length > 0) dbFieldName = configArr[0];//配置文件中有定义列名
                        else
                        {
                            //没定义则从实体属性获取对应的数据库字段                      
                            CustomerProperty fieldCp = GetCustomerProperty(queryInstance.Param.entityType, ki.Key);
                            if (fieldCp == null) throw new ClientException("查询条件表达式错误：" + entityType.Name + "并不包含" + ki.Key);
                            dbFieldName = fieldCp.GetDBFieldName();
                            pinfo = fieldCp.Info;
                        }
                    }
                    if (regex.IsMatch(tempStr))
                    {//客户端有传表达式         
                        
                        string exp;
                        if (ki.Value.IndexOf("$field") > -1) exp = ki.Value.Replace("$field", dbFieldName);
                        else exp = dbFieldName + " " + ki.Value;
                        configArr = new string[] { dbFieldName, exp };
                    }
                    else
                    {
                        string formatStr = null;

                        if (pinfo != null)
                        {
                            switch (pinfo.PropertyType.Name.ToLower())
                            {
                                case "int":
                                case "int32":
                                case "float":
                                case "single":
                                case "double":
                                case "decimal":
                                case "long":
                                    formatStr = " = {0}";
                                    break;
                            }
                        }
                        if (formatStr == null) formatStr = " = '{0}' ";
                        configArr = new string[] { dbFieldName, dbFieldName + formatStr };
                    }
                }
                if (configArr.Length < 2) configArr = new string[] { configArr[0], configArr[0] + " = '{0}' " };
                WhereSql += string.Format(" and " + configArr[1], whereFieldValArr);
            }
            if (WhereSql.Length > 0) WhereSql = WhereSql.Substring(5);


            
            string QueryFieldSql = "";
            stepMsg = "组织selete前面的语句";  
            foreach (QueryField qf in queryInstance.Param.QueryFields)
            {
                QueryFieldSql += ",";                
                if (qf.CustomValue != null) {
                    QueryFieldSql += qf.CustomValue;
                }
                else
                {
                    string[] configArr;
                    qei.Config.Fields.TryGetValue(qf.Info.Name, out configArr);
                    if (configArr != null && configArr.Length > 0) QueryFieldSql += configArr[0] + " " + qf.Info.Name;
                    else QueryFieldSql += qf.GetDBFieldName() + " " + qf.Info.Name;
                }                
            }
            QueryFieldSql = QueryFieldSql.Substring(1);
            stepMsg = "执行查询语句";
            string sql;
            if (qei.Param.Page != null)
            {
                int startIndex = (qei.Param.Page.PageIndex - 1) * qei.Param.Page.PageSize + 1;
                int endIndex = startIndex + qei.Param.Page.PageSize - 1;
                if (startIndex == 1)//只在第一页查询count
                {
                    stepMsg = "分页查询count";
                    string queryTotalSql = "select count(0) from " + qei.Config.TableSql;
                    if (!string.IsNullOrEmpty(WhereSql)) queryTotalSql += " where " + WhereSql;
                    object countScalar = dbAccess.ExecScalar(queryTotalSql);
                    if (countScalar == null || countScalar == DBNull.Value) throw new ClientException("分页查询total结果返回为空");
                    qei.Param.Page.Total = Convert.ToInt32(countScalar);
                }
                stepMsg = "执行分页查询";
                if (string.IsNullOrEmpty(qei.Param.orderBy))
                {//没有orderby 则使用主键
                    CustomerProperty cp = DBContext.GetUsaAttrOnce(qei.Param.entityType, DBFieldUsage.PrimaryKey);
                    qei.Param.orderBy = cp.GetDBFieldName();
                    string[] configArr;
                    qei.Config.Fields.TryGetValue(qei.Param.orderBy, out configArr);
                    if (configArr != null && configArr.Length > 0) qei.Param.orderBy = configArr[0];//如果字段有配置
                }
                return dbAccess.PartQuery(QueryFieldSql, qei.Config.TableSql, WhereSql, qei.Param.orderBy, startIndex, endIndex);
            }
            else
            {
                sql = "select " + QueryFieldSql + " from " + qei.Config.TableSql;
                if (!string.IsNullOrEmpty(WhereSql)) sql += " where " + WhereSql;
                if (!string.IsNullOrEmpty(qei.Param.orderBy)) sql += " order by " + qei.Param.orderBy;
                return dbAccess.QueryTable(sql);
            }

        }
        /// <summary>
        /// 保存对象对表，可以配置文件中设定不允许修改的列NotAllow
        /// </summary>
        /// <param name="permission">权限和表达式配置</param>
        /// <param name="entity">要保存的对象</param>
        public void DoSave(string permission, IDBEntity entity)
        {
            try
            {
                SetQueryEntityConfig(new QueryParam() { permission = permission, entityName = entity.GetType().Name });
                SaveEntity(entity, null);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }
        /// <summary>
        /// 保存对象对表，可以配置文件中设定不允许修改的列NotAllow
        /// </summary>
        /// <param name="permission">权限和表达式配置</param>
        /// <param name="entityType">类型</param>
        /// <param name="entityJson">对象json</param>
        /// <returns></returns>
        public IDBEntity DoSave(string permission, string entityName, string entityJson)
        {
            try
            {
                SetQueryEntityConfig(new QueryParam() { entityName = entityName, permission = permission });
                if (!queryInstance.Param.entityType.IsSubclassOf(typeof(IDBEntity))) throw new ClientException(queryInstance.Param.entityType.Name + "类型没有继承IDBEntity");
                stepMsg = "解析保存对象json";
                object main = Newtonsoft.Json.JsonConvert.DeserializeObject(entityJson, queryInstance.Param.entityType, new Newtonsoft.Json.JsonSerializerSettings());
                if (main == null) throw new ClientException("json序列化为对象异常，结果为空，可能类型不对");
                SaveEntity(main, null);
                return main as IDBEntity;
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        public IDBEntity DeserializeEntity(string permission, string entityName, string entityJson)
        {
            SetQueryEntityConfig(new QueryParam() { entityName = entityName, permission = permission });
            if (!queryInstance.Param.entityType.IsSubclassOf(typeof(IDBEntity))) throw new ClientException(queryInstance.Param.entityType.Name + "类型没有继承IDBEntity");
            stepMsg = "解析保存对象json";
            object main = Newtonsoft.Json.JsonConvert.DeserializeObject(entityJson, queryInstance.Param.entityType, new Newtonsoft.Json.JsonSerializerSettings());
            if (main == null) throw new ClientException("json序列化为对象异常，结果为空，可能类型不对");
            return main as IDBEntity;
        }
        public DBTableAttribute GetTableAttr()
        {
            if (queryInstance == null) throw new ClientException("尚未初始化参数");
            return GetTableAttr(queryInstance.Param.entityType);
        }
        private DBTableAttribute GetTableAttr(Type t)
        {
            if (t == null) throw new ClientException("找不到该类型的实体");
            object[] attrs = t.GetCustomAttributes(typeof(DBTableAttribute), true);
            if (attrs.Length < 1)
            {
                return new DBTableAttribute() { TableName = t.Name };
            }
           DBTableAttribute attr= (DBTableAttribute)attrs[0];
           if (attr.TableName == null) attr.TableName = t.Name;
            //throw new ClientException(t.Name + "没有定义DBTableAttribute");
           return attr;
        }

        public class ParseInfo
        {
            public bool notUpdate = false;//当有editflag属性，并且为空或者0的时候不操作数据  
            public string primaryKey = null;
            public object primaryKeyVal = 0;
            public PropertyInfo primaryKeyPro = null;
            public DBTableAttribute tableAttr;
            public Dictionary<CustomerProperty, object> proListExcludePrimary = new Dictionary<CustomerProperty, object>();
            public Dictionary<string, object> proItemsList = new Dictionary<string, object>();
            public Dictionary<string, Type> proItemsDefList = new Dictionary<string, Type>();
            public bool PrimaryKeyIsNull()
            {
                if (primaryKeyVal == null) return true;
                if (primaryKeyPro == null) throw new ClientException("primaryKeyPro is null when check PrimaryKeyIsNull ");
                return primaryKeyVal.Equals(0);
            }
        }

        private CustomerProperty getProCustomerAttr(Type entityType, PropertyInfo proInfo)
        {
            CustomerProperty cus = GetCustomerProperty(entityType, proInfo.Name);
            if (cus == null)
            {
                //获取自定义属性
                object[] fieldAttrs = proInfo.GetCustomAttributes(typeof(DBFieldAttribute), true);
                DBFieldAttribute attrField = null;
                if (fieldAttrs.Length > 0)//有自定
                {
                    attrField = fieldAttrs[0] as DBFieldAttribute;
                }
                return new CustomerProperty() { Info = proInfo, Attr = attrField };
            }
            else return cus;
        }

        private ParseInfo ParseCustomerAttr(Type entityType, object entityObj)
        {
            stepMsg = "Parse " + entityType.Name + " CustomerAttr";
            ParseInfo parseInfo = new ParseInfo();
            parseInfo.tableAttr = GetTableAttr(entityType);
            foreach (PropertyInfo proInfo in entityType.GetProperties())
            {
                if (proInfo.GetGetMethod() == null) continue;
                CustomerProperty cusPro = getProCustomerAttr(entityType, proInfo);
                object proVal = entityObj == null ? null : proInfo.GetValue(entityObj, null);
                string fieldName = cusPro.GetDBFieldName();
                if (cusPro.IsPrimaryKey())
                {
                    parseInfo.primaryKey = fieldName;
                    parseInfo.primaryKeyPro = proInfo;
                    parseInfo.primaryKeyVal = proVal;
                    continue;
                }
                Type proinfoType = proInfo.PropertyType;
                switch (proinfoType.Namespace)
                {
                    case "System"://简单类型
                        if (proVal != null && !DateTime.MinValue.Equals(proVal))
                        {
                            if (proInfo.Name.ToLower() == "editflag") { if (proVal == null || proVal.Equals(0)) parseInfo.notUpdate = true; }
                            else parseInfo.proListExcludePrimary.Add(cusPro, proVal);
                        }
                        else
                        {
                            if (queryInstance.Config.IgnoreNull != null)
                            {
                                foreach (string ignoreField in queryInstance.Config.IgnoreNull)
                                {
                                    if (ignoreField != null && ignoreField.Equals(proInfo.Name))
                                    {
                                        parseInfo.proListExcludePrimary.Add(cusPro, null);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    case "System.Collections.Generic":
                        Type type = GetIDBEntityListItemType(proinfoType);
                        if (type != null)
                        {
                            if (proVal != null) parseInfo.proItemsList.Add(proInfo.Name, proVal);
                            parseInfo.proItemsDefList.Add(proInfo.Name, type);
                        }
                        break;
                    default:
                        if (proinfoType.IsSubclassOf(typeof(IDBEntity))) parseInfo.proItemsDefList.Add(proInfo.Name, proinfoType);
                        break;
                }
            }
            return parseInfo;
        }

        private Type GetIDBEntityListItemType(Type proinfoType)
        {
            Type[] typeArr = proinfoType.GetGenericArguments();
            if (typeArr.Length > 0 && typeArr[0].IsSubclassOf(typeof(IDBEntity)))
                return typeArr[0];
            else return null;
        }

        private Type GetIDBEntityListItemType(object proinfoType)
        {
            Type[] typeArr = proinfoType.GetType().GetGenericArguments();
            if (typeArr.Length > 0 && typeArr[0].IsSubclassOf(typeof(IDBEntity)))
                return typeArr[0];
            else return null;
        }

        /// <summary>
        /// 获取指定Usage的属性和自定义属性
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="usa"></param>
        /// <param name="property"></param>
        /// <param name="dbField"></param>
        public static List<CustomerProperty> GetUsaAttr(Type entityType, DBFieldUsage usa)
        {
            return (List<CustomerProperty>)GetTypeCustomerPropertys(entityType).FindAll(x => x.Attr != null && x.Attr.Usage == usa);
        }

        /// <summary>
        /// 获取指定Usage的属性和自定义属性
        /// </summary>
        public List<CustomerProperty> GetUsaAttrs(DBFieldUsage usa)
        {
            if (queryInstance == null) throw new ClientException("尚未初始化参数");
            return (List<CustomerProperty>)GetTypeCustomerPropertys(queryInstance.Param.entityType).FindAll(x => x.Attr != null && x.Attr.Usage == usa);
        }
        /// <summary>
        /// 获取指定Usage的属性和自定义属性
        /// </summary>
        public CustomerProperty GetUsaAttr(DBFieldUsage usa)
        {
            if (queryInstance == null) throw new ClientException("尚未初始化参数");
            List<CustomerProperty> lst= (List<CustomerProperty>)GetTypeCustomerPropertys(queryInstance.Param.entityType).FindAll(x => x.Attr != null && x.Attr.Usage == usa);
            if (lst.Count > 0) return lst[0];
            return null;
        }
        /// <summary>
        /// 通过数据库字段获取属性
        /// </summary>
        /// <param name="dbFieldName"></param>
        /// <returns></returns>
        public CustomerProperty GetPropertyByDBName(string dbFieldName)
        {
            if (queryInstance == null) throw new ClientException("尚未初始化参数");
            if (dbFieldName == null) throw new ClientException("GetPropertyByDBName dbfieldName is not allow null");
            List<CustomerProperty> lst= (List<CustomerProperty>)GetTypeCustomerPropertys(queryInstance.Param.entityType).FindAll(x => x.GetDBFieldName() == dbFieldName);
            if (lst.Count > 0) return lst[0];
            else throw new ClientException(queryInstance.Param.entityType.Name + "不包含数据字段" + dbFieldName + "对应的属性定义!");
        }
        //public CustomerProperty GetUsaAttrOnce(DBFieldUsage usa)
        //{

        //}

        public static CustomerProperty GetUsaAttrOnce(Type entityType, DBFieldUsage usa)
        {
            foreach (PropertyInfo proInfo in entityType.GetProperties())
            {
                object[] fieldAttrs = proInfo.GetCustomAttributes(typeof(DBFieldAttribute), true);
                if (fieldAttrs.Length > 0)
                {
                    DBFieldAttribute attrField = fieldAttrs[0] as DBFieldAttribute;
                    if (attrField.Usage == usa)
                    {
                        if (string.IsNullOrEmpty(attrField.FieldName)) attrField.FieldName = proInfo.Name;
                        return new CustomerProperty() { Attr = attrField, Info = proInfo };
                    }
                }
            }
            return null;
        }

        public class CustomerProperty
        {
            public PropertyInfo Info { get; set; }
            public DBFieldAttribute Attr { get; set; }

            public string GetDBFieldName()
            {
                if (Attr == null || string.IsNullOrEmpty(Attr.FieldName)) return Info.Name;
                else
                {
                    return Attr.FieldName;
                }
            }

            public bool IsPrimaryKey()
            {
                return Attr != null && Attr.Usage == DBFieldUsage.PrimaryKey;
            }
        }

        private string FormatValueForDB(PropertyInfo info, object value)
        {
            if (value == null) return "null";
            Type t;
            if (!info.PropertyType.IsGenericType)
            {
                t = info.PropertyType;
            }
            else
            {
                //泛型Nullable<>
                Type genericTypeDefinition = info.PropertyType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>))
                {
                    t = Nullable.GetUnderlyingType(info.PropertyType);
                }
                else return "null";
            }
            switch (t.Name)
            {
                case "Int32":
                case "Int64":
                case "Single":
                case "Double":
                case "Decimal":
                    return value.ToString();
                case "Boolean":
                    return value.Equals(true) ? "1" : "0";
                default:
                    return "'" + value + "'";
            }
        }

        private void SaveEntity(object obj, ParseInfo rootParseInfo)
        {
            Type entityType = obj.GetType();
            stepMsg = "开始保存" + entityType.Name;
            ParseInfo parseInfo = ParseCustomerAttr(entityType, obj);
            //if (parseInfo.proListExcludePrimary.Count == 0 ) throw new ClientException("没有要操作的列");
            if (parseInfo.notUpdate) return;
            if (string.IsNullOrEmpty(parseInfo.primaryKey) || parseInfo.primaryKeyPro == null) throw new ClientException(obj.GetType().Name + "的DBTableAttribute属性没有定义主键");
            string sql = "";
            if (parseInfo.proListExcludePrimary.Count > 0)
            {
                bool isNew = parseInfo.PrimaryKeyIsNull();
                List<IDbDataParameter> parameters = new List<IDbDataParameter>();
                if (isNew)
                {
                    stepMsg = "生成插入语句";
                    sql = "insert into " + parseInfo.tableAttr.TableName + " (";
                    foreach (KeyValuePair<CustomerProperty, object> kv in parseInfo.proListExcludePrimary) sql += kv.Key.GetDBFieldName() + ",";
                    sql = sql.Substring(0, sql.Length - 1);
                    sql += ") values(";
                    foreach (KeyValuePair<CustomerProperty, object> kv in parseInfo.proListExcludePrimary)
                    {
                        // FormatValueForDB(kv.Key.Info, kv.Value)
                        IDbDataParameter parameter = dbAccess.CreateParameter(kv.Key.GetDBFieldName(), kv.Value);
                        sql += parameter.ParameterName + ",";
                        parameters.Add(parameter);
                    }
                    sql = sql.Substring(0, sql.Length - 1);
                    sql += ")";
                    if (parseInfo.primaryKeyPro.PropertyType == typeof(int))
                    {
                        sql += ";" + dbAccess.IdentitySql;
                        stepMsg = "执行插入语句";
                        object scalar = dbAccess.ExecScalar(sql, CommandType.Text, parameters.ToArray());
                        if (scalar == null || scalar == DBNull.Value) throw new ClientException("自动获取主键失败");
                        parseInfo.primaryKeyVal = Convert.ToInt32(scalar);
                        parseInfo.primaryKeyPro.SetValue(obj, Convert.ToInt32(scalar), null);
                    }
                    else
                    {
                        stepMsg = "执行插入语句";
                        dbAccess.ExecuteCommand(sql);
                    }
                }
                else
                {
                    stepMsg = "生成更新语句";
                    sql = "update " + parseInfo.tableAttr.TableName + " set ";
                    foreach (KeyValuePair<CustomerProperty, object> kv in parseInfo.proListExcludePrimary)
                    {
                        string fieldName = kv.Key.GetDBFieldName();
                        IDbDataParameter parameter = dbAccess.CreateParameter(fieldName, kv.Value);
                        sql += fieldName + "=" + parameter.ParameterName + ",";
                        parameters.Add(parameter);
                    }
                    sql = sql.Substring(0, sql.Length - 1);
                    sql += " where " + parseInfo.primaryKey + " = '" + parseInfo.primaryKeyVal + "'";
                    stepMsg = "执行更新语句";
                    dbAccess.ExecuteCommand(sql, CommandType.Text, parameters.ToArray());
                }
            }
            if (rootParseInfo == null) rootParseInfo = parseInfo;
            object mainPrimaryVal = parseInfo.primaryKeyPro.GetValue(obj, null);
            foreach (KeyValuePair<string, object> kv in parseInfo.proItemsList)
            {
                if (kv.Value is IEnumerable)
                {
                    foreach (IDBEntity en in ((IEnumerable)kv.Value))
                    {
                        SetForeignVal(en, obj, parseInfo, rootParseInfo);
                        SaveEntity(en, rootParseInfo);
                    }
                }
                else
                {
                    SetForeignVal(kv.Value, obj, parseInfo, rootParseInfo);
                    SaveEntity(kv.Value, rootParseInfo);
                }
            }
        }

        private void SetForeignVal(object obj, object foreignObj, ParseInfo parseInfo, ParseInfo rootParseInfo)
        {
            if (obj == null) throw new ClientException("SetForeignVal object is null");
            stepMsg = "设置" + obj.GetType().Name + "外键的值";
            //设置外键的值,rootParseInfo主要是解决明细表中有多级，子级对应的主表外键值也要赋值
            CustomerProperty cusPro = getForeignProperry(obj, parseInfo);
            if (cusPro == null) throw new ClientException(obj.GetType().Name + "没有定义外键，注意：如果是多级，则父编号也要定义为外键！");
            stepMsg = "设置外键" + cusPro.Info.Name;
            cusPro.Info.SetValue(obj, parseInfo.primaryKeyVal, null);
            CustomerProperty rootCusPro = getForeignProperry(obj, rootParseInfo);
            if (rootCusPro != null)
            {
                stepMsg = "设置外键" + rootCusPro.Info.Name;
                rootCusPro.Info.SetValue(obj, rootParseInfo.primaryKeyVal, null);
            }

        }

        private CustomerProperty getForeignProperry(object en, ParseInfo foreignParse)
        {
            CustomerProperty cusPro = null;
            foreach (CustomerProperty pro in GetUsaAttr(en.GetType(), DBFieldUsage.ForeignKey))
            {
                if (pro.Attr.ForeignTable == foreignParse.tableAttr.TableName)
                {
                    cusPro = pro;
                    if (string.IsNullOrEmpty(pro.Attr.ForeignTableField)) pro.Attr.ForeignTableField = foreignParse.primaryKey;
                    break;

                }
            }
            return cusPro;
        }

      
        /// <summary>
        /// 获取执行的信息
        /// </summary>
        /// <returns></returns>
        public string GetStepMsg()
        {
            return "step:" + stepMsg + " \n lastSql:" + dbAccess.GetLastSql();
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="permission">权限配置名</param>
        /// <param name="entityName">对象类型</param>
        /// <param name="value">主键值</param>
        /// <returns></returns>
        public int DoDelete(string permission, string entityName, string value)
        {
            try
            {
                SetQueryEntityConfig(new QueryParam() { entityName = entityName, permission = permission });
                return DoDelete(queryInstance.Param.entityType, null, value);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="permission">权限配置名</param>        
        /// <param name="value">主键值</param>
        /// <returns></returns>
        public int DoDelete<T>(string value, string permission = null)
        {
            try
            {
                SetQueryEntityConfig(new QueryParam() { entityType = typeof(T), permission = permission });
                return DoDelete(queryInstance.Param.entityType, null, value);
            }
            catch (Exception ex)
            {
                throw new ClientException(stepMsg + ":" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 删除对象，如果对象中有定义标记删除的属性，则标记，否则delete
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="entityName"></param>
        /// <param name="primarkeyVal"></param>
        private int DoDelete(Type entityType, string key, string value)
        {
            int effectRows = 0;
            stepMsg = "开始删除" + entityType.Name;
            if (string.IsNullOrEmpty(value)) throw new ClientException("删除的条件表达式值不能为空");

            ParseInfo parseInfo = ParseCustomerAttr(entityType, null);
            if (key == null) key = parseInfo.primaryKey;
            string sql;
            string[] primaryKeyValArr;
            if (key == parseInfo.primaryKey) primaryKeyValArr = new string[] { value };
            else
            {
                //根据外键删除时，查询要删除的内容的主键值
                stepMsg = "查询要删除的外键值数组";
                sql = "select " + parseInfo.primaryKey + " from " + parseInfo.tableAttr.TableName + " where " + key + "='" + value + "'";
                DataTable dt = dbAccess.QueryTable(sql);
                primaryKeyValArr = new string[dt.Rows.Count];
                for (int i = 0; i < dt.Rows.Count; i++) primaryKeyValArr[i] = dt.Rows[i][0].ToString();
            }
            if (primaryKeyValArr.Length == 0) return 0;
            stepMsg = "获取是否标记删除";
            List<CustomerProperty> lstTemp = GetUsaAttr(entityType, DBFieldUsage.MarkDelete);

            if (lstTemp.Count > 0)//标记删除            
                sql = "update " + parseInfo.tableAttr.TableName + " set " + lstTemp[0].GetDBFieldName() + "= -1  where " + parseInfo.primaryKey + "='" + value + "'";
            else sql = "delete from " + parseInfo.tableAttr.TableName + " where " + parseInfo.primaryKey + "='" + value + "'";
            stepMsg = "执行删除语句";
            effectRows = dbAccess.ExecuteCommand(sql);
            //子元素，按外键删除
            foreach (KeyValuePair<string, Type> items in parseInfo.proItemsDefList)
            {
                CustomerProperty cusPro = getForeignProperry(items.Value, parseInfo);
                if (cusPro == null) continue;
                string fieldName = cusPro.Attr.FieldName;
                foreach (string val in primaryKeyValArr)
                {
                    effectRows += DoDelete(entityType, fieldName, val);
                }
            }
            return effectRows;
        }


        #region DBACCESS
        /// <summary>
        /// 数据访问
        /// </summary>
        [Obsolete("请直接使用DBContext相应的方法")]
        public IDBAccess DbAccess
        {
            get { return dbAccess; }
        }

        public DataTable QueryTable(string text, CommandType cmdType)
        {
            return dbAccess.QueryTable(text, cmdType);
        }
        public DataTable QueryTable(string sql)
        {
            return dbAccess.QueryTable(sql);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="select"></param>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="orderby"></param>
        /// <param name="start">起始位置，从1开始</param>
        /// <param name="end">结束位置</param>
        /// <returns></returns>
        public DataTable PartQuery(string select, string table, string where, string orderby, int start, int end)
        {
            return dbAccess.PartQuery(select, table, where, orderby, start, end);
        }

        public int ExecuteCommand(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            return dbAccess.ExecuteCommand(text, cmdType, parameters);
        }
        public int ExecuteCommand(string sql)
        {
            return dbAccess.ExecuteCommand(sql);
        }

        public object ExecScalar(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            return dbAccess.ExecScalar(text, cmdType,parameters);
        }
        public object ExecScalar(string sql)
        {
            return dbAccess.ExecScalar(sql);
        }
        /// <summary>
        /// 创建参数
        /// </summary>
        /// <returns></returns>
        public IDbDataParameter CreateParameter(string name, object value) {
            return dbAccess.CreateParameter(name, value);
        }

        /// <summary>
        /// 关闭连接，如果有事务则提交
        /// </summary>
        /// <returns></returns>
        public void Close() {
            dbAccess.Close();
        }

        #endregion
        /// <summary>
        /// 释放，如果有未提交的事务，则rollback，有未关闭的连接则close
        /// </summary>
        public void Dispose()
        {
            dbAccess.Dispose();
        }
    }
}