using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;

namespace ColysSharp.StaticHtml
{
    [DBTable(TableName = "X5_wsNews")]
    public class NewsItem:IDBEntity
    {
        [DBField(Usage = DBFieldUsage.PrimaryKey)]
        public int Autocode { get; set; }

        public string STitle { get; set; }

        public string SClsCode { get; set; }

        public string SContent { get; set; }

        public string SSummary { get; set; }

        public string SAuthor { get; set; }

        public string DTimeAdd { get; set; }

        public override string ToString()
        {
            return Autocode.ToString();
        }
        [DBField(Usage=DBFieldUsage.NoField)]
        public string SClsName { get; set; }

        public int lParentCode { get; set; }

        public int lHit { get; set; }

        //public string sSource { get; set; }
        public string lComment { get; set; }

        public string STag { get; set; }

        public int lZan { get; set; }
    }
}
