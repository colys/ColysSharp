using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColysSharp.DataBase
{
    public interface IDBAccess:IDisposable
    {
        
        void Create(string connectionString,bool useTrans=false);

        DataTable QueryTable(string text, CommandType cmdType);
        DataTable QueryTable(string sql);

        DataTable PartQuery(string select, string table, string where, string orderby, int start, int end);

        int ExecuteCommand(string text,CommandType cmdType,params System.Data.IDbDataParameter[] parameters);
        int ExecuteCommand(string sql);

        IDbDataParameter CreateParameter(string name, object value);

        object ExecScalar(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters);
        object ExecScalar(string sql);
        string IdentitySql { get; }

        void Close();

        string GetLastSql();
    }
}
