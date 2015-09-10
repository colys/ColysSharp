using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;

namespace ColysSharp.StaticHtml
{
    [DBTable(TableName = "X5_wsModules")]
    public class ModuleItem:IDBEntity
    {
        [DBField(Usage = DBFieldUsage.PrimaryKey)]
        public int Autocode { get; set; }
        public string sModuleName { get; set; }
        public string sLabelName { get; set; }

        public string sType { get; set; }
        public string sContent { get; set; }
        [DBField( Usage= DBFieldUsage.MarkDelete )]
        public int bDel { get; set; }

    }
}
