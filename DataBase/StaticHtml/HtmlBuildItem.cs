using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ColysSharp.StaticHtml
{
    public class HtmlBuildItem
    {
        public string Path { get; set; }
        public HtmlBuildItem()
        {
            if (HttpContext.Current != null)
                Host = HttpContext.Current.Request.Url.Authority;
        }

        public HtmlBuildModifyType type { get; set; }

        public string CompanyCode { get; set; }

        public string UserCode { get; set; }

        /// <summary>
        /// 反正是触发生成的对象，可能是news类，也可能是NewCls类
        /// </summary>
        public object Target { get; set; }

        public SiteInfo Site { get; set; }

        public string UrlPageName { get; set; }

        public String[] UrlPageValues { get; set; }

        public System.Collections.Specialized.NameValueCollection QueryString { get; set; }
        public string Host { get; set; }

    }
}