using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ColysSharp.DataBase
{
    public class DBContextConfig {
        public string DatabaseType; 
        public string EntityTypeFormat; 
        public String ConnectionString; 
        /// <summary>
        /// 记录sql语句的方式，0：不记录，1：记录除select之外的，2：全部
        /// </summary>
        public int LogSql;
        public DBContextConfig() { }
        public DBContextConfig(ConnectionStringSettings connSet,string format) {
            if (connSet == null || string.IsNullOrEmpty(connSet.ConnectionString)) throw new Exception("数据库连接串没配置或者值为空");
            if (string.IsNullOrEmpty(connSet.ProviderName)) connSet.ProviderName = "System.Data.SqlClient";
            DatabaseType = connSet.ProviderName;// == "System.Data.SqlClient" ? "sqlserver" : "mysql";
            ConnectionString = connSet.ConnectionString;
            EntityTypeFormat = format;
        }
    }
}
