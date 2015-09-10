using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebSite
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            var staticHtml = new RouteValueDictionary { { "site", "main" }, { "page", "index" },{"pageVal","0"} };
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            routes.MapRoute("staticHtml", "Html/{site}/{page}/{pageVal}", new { controller = "News", action = "RenderHtmlRequest" }); 

        }
    }
}