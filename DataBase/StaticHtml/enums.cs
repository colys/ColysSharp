using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColysSharp.StaticHtml
{



    public enum HtmlBuildModifyType
    {
        /// <summary>
        /// 修改内容
        /// </summary>
        Detail = 1,
        /// <summary>
        /// 修改分类名称
        /// </summary>
        ReNameClass = 2,

        /// <summary>
        /// 手动生成首页
        /// </summary>
        ManualIndex = 3,

        /// <summary>
        /// 生动生成列表页
        /// </summary>
        ManualList = 4,
        /// <summary>
        /// 手动生成内容页
        /// </summary>
        ManualDetail = 5,

        ///// <summary>
        ///// 修改列表模板
        ///// </summary>
        //ModifyListTmp = 6,

        ///// <summary>
        ///// 修改首页模板
        ///// </summary>
        //ModifyIndexTmp = 7
        /// <summary>
        /// 生成详情页面
        /// </summary>
        Comment = 8
    }





    /// <summary>
    /// 模板类型
    /// </summary>
    public enum TemplateType
    {
        /// <summary>
        /// 首页模板
        /// </summary>
        Index = 7,
        /// <summary>
        /// 列表模板
        /// </summary>
        List = 5,
        /// <summary>
        /// 内容详情模板
        /// </summary>
        Detail = 6,
        /// <summary>
        /// 评论模板
        /// </summary>
        Comment = 8
    }

}
