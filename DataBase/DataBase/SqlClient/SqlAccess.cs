using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColysSharp.DataBase.SqlClient
{
    class SqlAccess:IDBAccess
    {        
        public void Create(string connectionString,bool useTrans= false) {
            conStr = connectionString;
            this.useTrans = useTrans;
        }    

        public DataTable QueryTable(string sql)
        {
            return QueryTable(sql, CommandType.Text);
        }

        string lastSql;
        bool useTrans = false;
        SqlConnection conn;
        SqlTransaction trans;
        SqlCommand cmd;
        string conStr;

        private void OpenSql()
        {
            if (conn == null) conn = new SqlConnection(conStr);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
                if (useTrans) trans = conn.BeginTransaction();                
            }
        }
 

        private void setCommand(string sql,bool query= false)
        {
            if (conn == null) OpenSql();
            if (cmd == null) cmd = new SqlCommand(sql, conn);
            else cmd.CommandText = sql;
            if (useTrans) cmd.Transaction = trans;
            lastSql = sql;
            DBContext.LogSql(sql,query);
        }

        public int ExecuteCommand(string sql)
        {
            return ExecuteCommand(sql, CommandType.Text);
        }

     

        public object ExecScalar(string sql)
        {
            return ExecScalar(sql, CommandType.Text);
        }

        public IDbDataParameter CreateParameter(string name, object value)
        {
            return new SqlParameter("@" + name, value);
        }
        public string IdentitySql
        {
            get { return "select @@identity"; }
        }

        public void Dispose()
        {
            if (conn != null)
            {
                if (conn.State == ConnectionState.Open ){
                    if(trans != null) trans.Rollback();
                    conn.Close();
                }
                conn.Dispose();
                conn = null;
            } 
        }


        public string GetLastSql()
        {
            return lastSql;
        }


        public void Close()
        {
            if (conn != null && conn.State == ConnectionState.Open) {
                if (trans != null && useTrans) { trans.Commit(); trans = null; }
                conn.Close();
            }
        }


        public DataTable PartQuery(string select, string table, string where,string orderby, int start,int end)
        {
            if(orderby==null) throw  new ArgumentNullException("pagger query order by");
            string sql = "select * from (select row_number() over(order by " + orderby + ") rownum, " + select + " from " + table;
            if (!string.IsNullOrEmpty(where)) sql += " where " + where;
            sql+=") t where rownum between " + start + " and " + end;
            return QueryTable(sql);
        }


        public DataTable QueryTable(string text, CommandType cmdType)
        {
            setCommand(text,true);
            cmd.CommandType = cmdType;
            System.Data.DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public int ExecuteCommand(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            setCommand(text);
            if (parameters != null)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddRange(parameters);
            }
            return cmd.ExecuteNonQuery();
        }

        public object ExecScalar(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters)
        {
            setCommand(text);
            if (parameters != null)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddRange(parameters);
            }
            return cmd.ExecuteScalar();
        }
    }
}
