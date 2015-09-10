using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;

namespace ColysSharp.StaticHtml
{

    [DBTable(TableName = "X5_wsNewCls")]
    public class NewsCls : ITreeDBEntity
    {
        [DBField(Usage = DBFieldUsage.PrimaryKey)]
        public int AutoCode { get; set; }
        public string SClsCode { get; set; }
        [DBField(Usage = DBFieldUsage.ForeignKey, ForeignTable = "X5_wsNewCls", ForeignTableField = "SClsCode")]
        public string SClsFCode { get; set; }

        public string LSiteCode { get; set; }

        public string SClsName { get; set; }

        public override string ToString()
        {
            return SClsCode;
        }
    }
}
