using ColysSharp.StaticHtml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace ColysSharp.Mvc
{
    public class StaticHtmlController : BaseController
    {
        protected override string EntityFormat
        {
            get
            {
                return "ColysSharp.StaticHtml.{0},ColysSharp.DataBase";
            }
        }
        public void RenderHtmlRequest(string site, string page, string pageVal)
        {
            HtmlRender render = new HtmlRender();
            render.RenderRequest(System.Web.HttpContext.Current);
        }

        //public override string DoQuery(string permission, string queryField, string entityName, string whereField, string orderBy)
        //{
        //    return base.DoQuery(permission, queryField, entityName, whereField, orderBy);
        //}
    }
}
