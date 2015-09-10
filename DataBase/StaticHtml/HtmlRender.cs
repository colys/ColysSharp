using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using ColysSharp.DataBase;

namespace ColysSharp.StaticHtml
{
    public class HtmlRender : IDisposable
    {
        public static string StaticHtmlPath = "html";
        public string CompanyCode { get; set; }
        DBContext dbContext;
        public List<string> BuildPageList = new List<string>();
        static Dictionary<string, System.Threading.Timer> builderTimers = new Dictionary<string, System.Threading.Timer>();
        public HtmlRender()
        {
            dbContext = new DBContext();
        }

        /// <summary>
        /// http响应入口
        /// </summary>
        /// <param name="context"></param>
        public void RenderRequest(HttpContext context)
        {

            string path = "";
            try
            {
                path = context.Request.Url.AbsolutePath;
                string pattern = @"^/" + StaticHtmlPath + "/([a-zA-Z0-9]+)(/([a-zA-Z0-9]+)_?([0-9_]+)?.html)?$";
                Regex regex = new Regex(pattern);
                Match m = regex.Match(path);
                if (!m.Success || m.Groups.Count < 2)
                {
                    context.Response.Write("sorry ! I can't find this Page,are you include the site name,and the page is correct"); return;
                }
                string siteName = m.Groups[1].Value;
                string pageName = null;


                string[] pageValArray = null;
                if (m.Groups.Count > 3)
                {
                    pageName = m.Groups[3].Value;
                    if (m.Groups.Count < 5 || m.Groups[4].Value == "")
                    {
                        path = string.Format("/{0}/{1}/{2}.html", StaticHtmlPath, siteName, pageName);
                    }
                    else
                    {
                        pageValArray = m.Groups[4].Value.Split('_');
                        path = string.Format("/{0}/{1}/{2}_{3}.html", StaticHtmlPath, siteName, pageName, pageValArray[0]);
                    }
                    //比如列表页面，static html表只保存到分类，不保存分页，所以要处理path，把页码移除
                }
                string result = GetHtmlByPath(path);
                if (result == null)
                {
                    int waitCount = 20;//如果已经有生成的线程，等待
                    while (waitCount > 0 && BuildPageList.Contains(path))
                    {
                        waitCount--;
                        System.Threading.Thread.Sleep(500);
                    }
                    BuildPageList.Add(path);

                    //获取站点信息
                    SiteInfo siteInfo = GetSiteByDir(siteName);
                    if (siteInfo == null) throw new Exception("site " + siteName + " is not exists");
                    HtmlBuildItem notifyItem = new HtmlBuildItem() { Site = siteInfo, QueryString = context.Request.QueryString };
                    if (m.Groups.Count > 3)
                    {
                        if (m.Groups.Count < 5) throw new Exception("输入地址不正确");
                        notifyItem.UrlPageName = pageName;
                        notifyItem.UrlPageValues = pageValArray;
                    }
                    else notifyItem.UrlPageName = "Index";
                    if (notifyItem.UrlPageValues == null || notifyItem.UrlPageValues.Length < 1) throw new Exception("url错误");
                    switch (notifyItem.UrlPageName.ToLower())
                    {
                        case "news":
                            //如果不动态生成详情页，请在这里直接返回，或者throw 文章不存在的异常
                            notifyItem.type = HtmlBuildModifyType.Detail;
                            NewsItem news = GetNews(Convert.ToInt32(notifyItem.UrlPageValues[0]));
                            if (news == null) throw new Exception("文章不存在或者已删除！");
                            notifyItem.Target = news;
                            BuildHtml2(notifyItem);
                            break;
                        case "list":
                            notifyItem.type = HtmlBuildModifyType.ManualList;
                            notifyItem.Target = new NewsCls() { SClsCode = notifyItem.UrlPageValues[0] };
                            BuildHtml2(notifyItem);
                            break;
                        case "index":
                            notifyItem.type = HtmlBuildModifyType.ManualIndex;
                            BuildHtml2(notifyItem);
                            break;
                        case "comment":
                            //评论模板，每个站点可配置
                            notifyItem.type = HtmlBuildModifyType.Comment;
                            BuildHtml2(notifyItem);
                            break;
                        default:
                            context.Response.Write("Can not find this Page"); return;

                    }

                    BuildPageList.Remove(path);
                    result = GetHtmlByPath(path);
                    if (result == null)
                    {
                        context.Response.Write("生成html失败...."); return;
                    }
                }
                string contentType = "text/html";
                string contentEncoding = "";
                bool isCompressed = CanGZip(context.Request, out contentEncoding);
                using (MemoryStream memoryStream = new MemoryStream(5000))
                {
                    using (Stream writer = isCompressed ?
                                (Stream)(new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Compress))
                               : memoryStream)
                    {
                        byte[] fileBytes = new System.Text.UTF8Encoding().GetBytes(result);
                        writer.Write(fileBytes, 0, fileBytes.Length);
                    }
                    byte[] responseBytes = memoryStream.ToArray();
                    HtmlModule.WriteBytes(responseBytes, context, isCompressed, contentType, contentEncoding);
                }
                //Func<string, bool, Result> func = _htmlBuildService.CreateHomePage;
                //func.BeginInvoke("", false, null, func);
            }
            catch (Exception ex)
            {
                context.Response.Write("Can not find this Page :" + ex.Message);
            }
        }


        public void BuildHtml2(HtmlBuildItem item)
        {

            switch (item.type)
            {
                case HtmlBuildModifyType.ManualDetail:
                case HtmlBuildModifyType.Detail:
                    NewsItem news = item.Target as NewsItem;
                    if (news == null) { throw new ArgumentNullException("News Target"); }
                    NewsTemplate template = GetTemplate(item.Site.AutoCode, news.SClsCode, TemplateType.Detail);
                    BuildHtml(template.SContent, "/" + StaticHtmlPath + "/" + item.Site.sDirectory + "/news_" + news.Autocode + ".html", item, template);
                    break;
                case HtmlBuildModifyType.ManualList:
                case HtmlBuildModifyType.ReNameClass:
                    {
                        NewsCls newsCls = item.Target as NewsCls;
                        if (newsCls == null) { throw new ArgumentNullException("NewsCls Target"); }
                        NewsTemplate newsClsTemplate = GetTemplate(item.Site.AutoCode, newsCls.SClsCode, TemplateType.List);
                        BuildHtml(newsClsTemplate.SContent, "/" + StaticHtmlPath + "/" + item.Site.sDirectory + "/list_" + newsCls.SClsCode + ".html", item, newsClsTemplate);
                    }
                    break;
                case HtmlBuildModifyType.ManualIndex:
                    {
                        NewsTemplate indexTemplate = GetTemplate(item.Site.AutoCode, null, TemplateType.Index);
                        BuildHtml(indexTemplate.SContent, "/" + StaticHtmlPath + "/" + item.Site.sDirectory + "/index.html", item, indexTemplate);
                        break;
                    }
                case HtmlBuildModifyType.Comment:
                    {
                        //生成评论页面模板staticHtml                        
                        NewsTemplate newsClsTemplate = GetTemplate(item.Site.AutoCode, null, TemplateType.Comment);
                        BuildHtml(newsClsTemplate.SContent, "/" + StaticHtmlPath + "/" + item.Site.sDirectory + "/comment.html", item, newsClsTemplate);
                        break;
                    }
                default:
                    break;

            }
        }

        private bool CanGZip(HttpRequest request, out string contentEncoding)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (acceptEncoding == null || (request.UserAgent != null && request.UserAgent.IndexOf("MSIE 6") > -1))
                acceptEncoding = "";
            contentEncoding = "";
            if (acceptEncoding.IndexOf("gzip") != -1)
            {
                contentEncoding = "gzip";
            }
            else if (acceptEncoding.IndexOf("deflate") != -1)
            {
                contentEncoding = "deflate";
            }
            if (acceptEncoding != null && acceptEncoding != "" &&
                contentEncoding != "")
                return true;
            return false;
        }

        public NewsTemplate GetTemplate(int siteCode, string classCode, TemplateType templateType)
        {
            object scalar = null;
            int templateId = 0;
            switch (templateType)
            {
                case TemplateType.Index:
                    //获取首页
                    scalar = dbContext.ExecScalar("select lIndexTemplate from X5_Sites Where AutoCode=" + siteCode);
                    break;

                default:
                    //获取列表页或者内容详情页                
                    string fieldName = templateType == TemplateType.List ? "lListTemplate" : "lDetailTemplate";
                    scalar = dbContext.ExecScalar("select " + fieldName + " from X5_NewsCls Where sClsCode=" + classCode);
                    break;
            }
            if (scalar != null && scalar != DBNull.Value) templateId = Convert.ToInt32(scalar);
            NewsTemplate template = null;
            if (templateId == 0)
            {
                //取默认配置
                template = dbContext.QuerySingle<NewsTemplate>(new { lSiteCode = siteCode, BDefault = 1, lType = (int)templateType });
            }
            else
            {
                template = dbContext.QuerySingle<NewsTemplate>(templateId);
            }

            if (template == null)
            {
                if (templateType == TemplateType.Index) throw new Exception("没有配置站点" + siteCode + "的首页模板，也没有可用的默认首页模板");
                else throw new Exception("没有配置分类" + classCode + "的模板，也没有可用的默认模板");
            }
            return template;
        }


        public void BuildHtml(string content, string path, HtmlBuildItem item, NewsTemplate template)
        {
            //文件内容和脚本内容
            StringBuilder str = new StringBuilder();
            StringBuilder script = new StringBuilder();
            //单个标签内容和单个脚本内容
            string label = "";
            string tempScript = "";
            //第一个标签开始和结束
            int left = content.IndexOf("┣");
            int right = content.IndexOf("┫");
            //当标签存在，生成标签内容
            while (left >= 0 && right >= 0)
            {
                //标签前面内容直接输出
                str.Append(content.Substring(0, left));
                //得到标签名称
                label = content.Substring(left + 1, right - left - 1);
                //获取标签真实内容，和所需要脚本
                str.Append(GetModuleByLabel2(item, label, out tempScript));//lvcunku修改，原来是用GetModuleByLabel
                //暂存所有需要脚本
                script.Append(tempScript + "\r\n");
                //模板中已生成的内容截取掉
                content = content.Substring(right + 1);
                //重新设置第一个标签的开始和结束0 
                left = content.IndexOf("┣");
                right = content.IndexOf("┫");
            }
            //找到body前的最后位置，安置脚本
            left = content.IndexOf("</body>");
            content = str.ToString() + content;

            str.AppendFormat("<!--LastCreateTime:{0}-->", DateTime.Now);

            UpdateStaticHtml(path, content, template);
        }




        private string GetModuleByLabel2(HtmlBuildItem item, string label, out string script)
        {
            if (item == null) throw new ArgumentNullException("HtmlBuildModifyItem");
            script = "";
            string result = "";
            DataTable modules = GetCacheModules();
            if (modules != null && modules.Rows.Count > 0)
            {
                string pattern = @"^([a-z0-9A-Z_]+)(\((.+)\))?$";
                Regex regex = new Regex(pattern);
                Match m = regex.Match(label);
                if (!m.Success) throw new Exception("模板label配置错误,label名称公使用字母数字及下划线，参数括号为半角！");
                string labelNoParams = m.Groups[1].Value;
                string[] labelParamArr = null;
                if (m.Groups.Count >

3) labelParamArr = m.Groups[3].Value.Split(',');
                DataRow[] drs = modules.Select("slabelname='" + labelNoParams + "'");
                if (drs.Length > 0)
                {
                    string type = drs[0]["stype"].ToString().ToLower();
                    string content = null;
                    switch (type)
                    {
                        case "attr":
                            //result += GetTargetAttrValue(item, drs[0]["scontent"].ToString());
                            break;
                        case "html":
                            content = HttpUtility.HtmlDecode(drs[0]["scontent"].ToString());
                            result += System.Web.HttpUtility.HtmlDecode(FormatLabelValue(content, item, labelParamArr));

                            break;
                        case "url":
                            string url = HttpUtility.HtmlDecode(drs[0]["scontent"].ToString());
                            url = FormatLabelValue(url, item, labelParamArr);
                            if (url.IndexOf("?") > 0) url += "&";
                            else url += "?";
                            url += "builder=1&company=" + CompanyCode;
                            WebClient webClient = new WebClient();
                            byte[] bytes = webClient.DownloadData(url);
                            result += Encoding.UTF8.GetString(bytes);
                            break;
                    }
                }
                else
                {
                    result += string.Format("<!--系统未设置此模块({0})-->", label);
                }
            }
            else
            {
                result += "<!--系统未设置任何模块-->";
            }
            return result;
        }




        private string FormatLabelValue(string labelParams, HtmlBuildItem item, string[] labelParamArr)
        {
            //格式化参数
            string pattern = @"(\$[a-zA-Z0-9_]+)+";
            Regex regex = new Regex(pattern);
            Match paraMatch = regex.Match(labelParams);
            if (paraMatch.Success)
            {
                while (paraMatch.Success)
                {
                    string valueStr = "";
                    string[] flagArr = paraMatch.Value.Split('_');
                    string str = flagArr[0];
                    for (int i = 1; i < flagArr.Length - 1; i++) { str += "_" + flagArr[i]; }
                    string flagVal = null;
                    if (flagArr.Length > 1) flagVal = flagArr[flagArr.Length - 1];
                    if (item.UrlPageValues == null) throw new Exception("item.UrlPageValues is null"); switch (str.ToLower())
                    {
                        case "$url_page_value":
                            if (flagVal == null) throw new Exception("模块" + paraMatch.Value + "配置有误，必需包含$和_");
                            int valIndex;
                            Int32.TryParse(flagVal, out valIndex);
                            if (valIndex < 1) throw new Exception("模块配置有误,$url_page_value必需指定int值的索引，比如$url_page_value_1");
                            valIndex--;
                            if (item.UrlPageValues.Length > valIndex) valueStr = item.UrlPageValues[valIndex];
                            break;
                        case "$url_param":

                            if (flagVal == null) throw new Exception("模块" + paraMatch.Value + "配置有误，必需包含$和_");
                            if (item.QueryString == null) throw new Exception("HtmlBuildItem的QueryString为空");
                            valueStr = item.QueryString[flagVal];
                            break;
                        case "$host":
                            valueStr = item.Host;
                            break;
                        case "$module_param":
                            if (flagVal == null) throw new Exception("模块" + paraMatch.Value + "配置有误，必需包含$和_");
                            int paramIndex;
                            Int32.TryParse(flagVal, out paramIndex);
                            if (paramIndex < 1) throw new Exception("模块配置有误,$module_param必需指定int值的索引，比如$module_param_1");
                            paramIndex--;

                            if (labelParamArr.Length > paramIndex) valueStr = labelParamArr[paramIndex];
                            break;
                        case "$attr":
                            if (flagVal == null) throw new Exception("模块" + paraMatch.Value + "配置有误，必需包含$和_");
                            valueStr = GetTargetAttrValue(item, flagVal);
                            break;
                        default:
                            break;
                    }
                    labelParams = labelParams.Replace(paraMatch.Value, valueStr);
                    paraMatch = paraMatch.NextMatch();
                }

            }


            return labelParams;
        }




        private string GetTargetAttrValue(HtmlBuildItem item, string attr)
        {
            if (item.Target == null) throw new Exception("使用对象属性模块，但对象为空");

            Type t = item.Target.GetType();
            try
            {
                var attrArr = attr.Split(',');
                object obj = t.InvokeMember(attrArr[0], System.Reflection.BindingFlags.GetProperty, null, item.Target, null);
                if (obj == null) return "";
                if (obj is DateTime && attrArr.Length > 1) obj = ((DateTime)obj).ToString(attrArr[1]);
                return HttpUtility.HtmlDecode(obj.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("获取" + t.Name + "对象属性" + attr + "失败:" + ex.Message);
            }
        }


        public void NoticeBuild(HtmlBuildItem item)
        {
            if (item == null) throw new ArgumentNullException("HtmlBuildItem");
            if (item.type == (HtmlBuildModifyType.ManualDetail | HtmlBuildModifyType.ManualIndex | HtmlBuildModifyType.ManualList))
            {
                Builder(item);
                return;
            }
            CompanyCode = item.CompanyCode;
            if (CompanyCode == "0" || string.IsNullOrEmpty(CompanyCode)) throw new Exception("companycode is zero");
            string key = GetKey(item);
            int delaye = 20000;//这里主要防止在短期内重生成同一个对象的html，所以添加一个Delaye处理
            if (System.Configuration.ConfigurationManager.AppSettings["static_html_delay"] != null)
            {
                Int32.TryParse(System.Configuration.ConfigurationManager.AppSettings["static_html_delay"], out delaye);
            }
            if (!builderTimers.ContainsKey(key)) builderTimers.Add(key, new System.Threading.Timer(Builder, item, delaye, -1));
            else
            {
                builderTimers[key].Change(delaye, -1);
            }
        }


        public void Builder(object stateinfo)
        {
            try
            {
                HtmlBuildItem item = (HtmlBuildItem)stateinfo;
                string key = GetKey(item);
                builderTimers[key].Dispose();
                builderTimers.Remove(key);
                BuildHtml2(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine("生成html失败：" + ex.Message);
            }
        }

        public string GetKey(HtmlBuildItem item)
        {
            string key = item.Site.AutoCode.ToString();
            if (item.Target != null) key += "-" + item.Target.GetType().Name + "-" + item.Target.ToString();
            return key;
        }

        private void UpdateStaticHtml(string path, string content, NewsTemplate template) {
            string sql = "update X5_wsStaticHtml set sContent=@content where sUrl='" + path + "'";
            System.Data.SqlClient.SqlParameter  par= new System.Data.SqlClient.SqlParameter("@content", content);
            int effect= dbContext.ExecuteCommand(sql, CommandType.Text, par);
            if (effect == 0) {
                sql = "insert into X5_wsStaticHtml(sUrl,sContent,lTemplateCode) values('"+path+"',@content,"+template.AutoCode+")";
                dbContext.ExecuteCommand(sql, CommandType.Text, par);
            }
        }

        public DataTable GetCacheModules()
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            object obj= objCache.Get("modules");
            if (obj != null)
            {
                return obj as DataTable;
            }
            else
            {
                DataTable dt = dbContext.QueryTable("modules", new { bDel = 1 });
                objCache.Add("modules", dt, null, DateTime.Now.AddMinutes(1), TimeSpan.Zero, System.Web.Caching.CacheItemPriority.Default, null);
                return dt;
            }
        }

        

        public NewsItem GetNews(int id)
        {
            return dbContext.QuerySingle<NewsItem>(id);
        }
        public string GetHtmlByPath(string path)
        {
            object scalar = dbContext.ExecScalar("select sContent from X5_wsStaticHtml where sUrl=" + path);
            if (scalar == null || scalar == DBNull.Value) return null;
            else return scalar.ToString();
        }

        public SiteInfo GetSiteByDir(string siteCode)
        {
            return dbContext.QuerySingle<SiteInfo>(new { sDirectory = siteCode });
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }
    }
}
