using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ColysSharp.StaticHtml
{
    public class HtmlModule:System.Web.IHttpModule
    {

        public void Dispose()
        {
            
        }

        public void Init(System.Web.HttpApplication context)
        {
            context.PreRequestHandlerExecute+=context_PreRequestHandlerExecute;
        }

        private void context_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            string extentName = System.IO.Path.GetExtension(HttpContext.Current.Request.Url.AbsolutePath).ToLower();
            if (extentName == ".html" || extentName == ".htm") {
                string physicalPath = HttpContext.Current.Server.MapPath(HttpContext.Current.Request.Url.AbsolutePath);
                if (File.Exists(physicalPath))
                {
                    byte[] fileBytes = null;
                    using (System.IO.Stream stream = File.OpenRead(physicalPath))
                    {
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);
                        fileBytes = bytes;
                    }
                    WriteBytes(fileBytes, HttpContext.Current, true, "text/html", "UTF-8");
                }
                else {
                    HtmlRender render = new HtmlRender();
                    render.RenderRequest(HttpContext.Current);
                }
            }
        }


        public static void WriteBytes(byte[] bytes, HttpContext context,
            bool isCompressed, string contentType, string contentEncoding)
        {
            HttpResponse response = context.Response;

            response.AppendHeader("Content-Length", bytes.Length.ToString());
            response.ContentType = contentType;
            if (isCompressed)
                response.AppendHeader("Content-Encoding", contentEncoding);
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Flush();
        }
    }
}
