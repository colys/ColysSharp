using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebSite.Controllers
{
    /// <summary>
    /// 新闻类控制器
    /// </summary>
    public class NewsController:ColysSharp.Mvc.StaticHtmlController
    {
        public ActionResult TreeEntity()
        {
            return View();
        }

        public ActionResult SiteManage() {
            return View();
        }
    }
}