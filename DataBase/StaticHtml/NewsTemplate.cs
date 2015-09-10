using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;

namespace ColysSharp.StaticHtml
{
    [DBTable(TableName = "X5_wsNewsTemplate")]
    public class NewsTemplate:IDBEntity
    {
        [DBField(Usage = DBFieldUsage.PrimaryKey)]
        public int AutoCode { get; set; }
        /// <summary>
        /// 类型 （1/2/3/4/5,新闻模板/栏目首页模板/栏目分页模板/首页模板/列表模板）
        /// </summary>
        public int LType { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string SName { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string SContent { get; set; }
        /// <summary>
        /// 新增时间
        /// </summary>
        public DateTime DTimeAdd { get; set; }
        /// <summary>
        /// 默认模板
        /// </summary>
        public int BDefault { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string SMemo { get; set; }

        /// <summary>
        /// 站点编号
        /// </summary>
        public int LSiteCode { get; set; }
    }

   
}


