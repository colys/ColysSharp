using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ColysSharp.DataBase;

namespace WebSite
{
    // 注意: 有关启用 IIS6 或 IIS7 经典模式的说明，
    // 请访问 http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            log4net.Config.XmlConfigurator.Configure();
            ConnectionStringSettings connSet = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            string configFile = Server.MapPath("~/Content/PermissionQuery.json");
            //DBContext.LoadConfigFromFile(configFile, new DBContextConfig(connSet, "WebSite.Models.{0}, WebSite") { LogSql = 1 });
            DBContext.LoadConfigFromFile(configFile, new DBContextConfig(connSet, "ColysSharp.StaticHtml.{0}, ColysSharp.DataBase") { LogSql = 1 });
        }
    }
}