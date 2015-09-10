using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebSite.Controllers
{
    public class HomeController : ColysSharp.Mvc.BaseController
    {
        protected override string EntityFormat
        {
            get
            {
                return "CirclesFinancial.Modals.{0},CirclesFinancial.Modals";
            }
        }

        public ActionResult Index()
        {
            //RedirectIfNoLogin();
            return View();
        }


    }
}
