using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;

namespace ColysSharp.StaticHtml
{
    [DBTable(TableName = "X5_wsSites")]
    public class SiteInfo:IDBEntity
    {
        [DBField(Usage = DBFieldUsage.PrimaryKey)]
        public int AutoCode { get; set; }

        public string sName { get; set; }

        public string sDirectory { get; set; }

        public string lCompanyCode { get; set; }

        public int lIndexTemplate { get; set; }
    }
}


