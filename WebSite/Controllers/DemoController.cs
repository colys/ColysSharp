using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebSite.Controllers
{
    public class DemoController:ColysSharp.Mvc.BaseController
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


        public ActionResult EntityTable()
        {
            return View();
        }



        public ActionResult Query()
        {
            return View();
        }

        public ActionResult JsonTable()
        {
            return View();
        }

        public ActionResult TreeEntityCustom()
        {
            return View();
        }


        public ActionResult DBContext()
        {
            return View();
        }


        public ActionResult BaseController()
        {
            return View();
        }
    }
}