using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ColysSharp
{
    public class MyHttpUtility
    {

        private const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1";


        Dictionary<string, CookieContainer> hostCookies = new Dictionary<string, CookieContainer>();

        private void BugFix_CookieDomain(CookieContainer cookieContainer)
        {
            System.Type _ContainerType = typeof(CookieContainer);
            Hashtable table = (Hashtable)_ContainerType.InvokeMember("m_domainTable",
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.GetField |
                                       System.Reflection.BindingFlags.Instance,
                                       null,
                                       cookieContainer,
                                       new object[] { });
            ArrayList keys = new ArrayList(table.Keys);
            foreach (string keyObj in keys)
            {
                string key = (keyObj as string);
                if (key[0] == '.')
                {
                    string newKey = key.Remove(0, 1);
                    table[newKey] = table[keyObj];
                }
            }
        }

        private String GetMid(String input, String s, String e)
        {
            int pos = input.IndexOf(s);
            if (pos == -1)
            {
                return "";
            }

            pos += s.Length;


            int pos_end = 0;
            if (e == "")
            {
                pos_end = input.Length;
            }
            else
            {
                pos_end = input.IndexOf(e, pos);
            }


            if (pos_end == -1)
            {
                return "";
            }
            return input.Substring(pos, pos_end - pos);
        }



        public String DoGet(String url, bool reandomNum = true)
        {
            HttpWebResponse res;
            return DoGet(url, reandomNum, out res);
        }

        private void SetHostCookie(Uri uri, CookieCollection cookies)
        {
            CookieContainer cc = hostCookies[uri.Authority];
            cc.Add(new Uri("https://119.4.99.217:7300"), cookies);
            Console.WriteLine("设置" + uri.Authority + "cookie");
            foreach (Cookie c in cookies)
            {
                Console.WriteLine(c.Name + ":" + c.Value);
            }
        }
        private CookieContainer GetHostCookieContainer(Uri uri)
        {
            CookieContainer cc;
            if (hostCookies.TryGetValue(uri.Authority, out cc))
            {
                CookieCollection cs = cc.GetCookies(uri);
                if (cc.Count > 0) Console.WriteLine(uri + "使用已有cookie");
                foreach (Cookie c in cs)
                {
                    Console.WriteLine(c.Name + ":" + c.Value);
                }
                return cc;
            }
            else
            {
                cc = new CookieContainer();
                Console.WriteLine(uri + "初始化cookie");
                hostCookies.Add(uri.Authority, cc);
                return cc;
            }
        }

        public String DoGet(String url, bool reandomNum, out HttpWebResponse webResponse)
        {
            if (reandomNum) url = UrlAddRandom(url);
            String html = "";
            StreamReader reader = null;
            HttpWebRequest webReqst;
            Uri uri = new Uri(url);
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                webReqst = WebRequest.Create(url) as HttpWebRequest;
                webReqst.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                webReqst = WebRequest.Create(url) as HttpWebRequest;
            }
            webReqst.Method = "GET";
            webReqst.UserAgent = DefaultUserAgent;
            webReqst.KeepAlive = true;
            webReqst.CookieContainer = GetHostCookieContainer(uri);
            webReqst.Timeout = 30000;
            webReqst.ReadWriteTimeout = 30000;
            webResponse = (HttpWebResponse)webReqst.GetResponse();
            if (webResponse.Cookies.Count > 0) SetHostCookie(uri, webResponse.Cookies);
            if (webResponse.StatusCode == HttpStatusCode.OK && webResponse.ContentLength < 1024 * 1024)
            {
                Stream stream = webResponse.GetResponseStream();
                stream.ReadTimeout = 30000;
                if (webResponse.ContentEncoding == "gzip")
                {
                    reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress), Encoding.Default);
                }
                else
                {
                    reader = new StreamReader(stream, Encoding.Default);
                }
                html = reader.ReadToEnd();
            }
            else
            {
                throw new Exception("server error:" + webResponse.StatusCode);
            }
            return html;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }


        public void Download(string url, string savePath, string rootDir, string referer = null)
        {
            Uri uri = new Uri(url);
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            //属性配置   
            webRequest.AllowWriteStreamBuffering = true;
            //创建证书文件
            X509Certificate objx509 = new X509Certificate(rootDir + "\\123.cer");
            //添加到请求里
            webRequest.ClientCertificates.Add(objx509);
            webRequest.CookieContainer = GetHostCookieContainer(uri);
            webRequest.MaximumResponseHeadersLength = -1;
            webRequest.Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
            webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; Maxthon; .NET CLR 1.1.4322)";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "GET";
            webRequest.Headers.Add("Accept-Language", "zh-cn");
            webRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
            webRequest.KeepAlive = true;
            if (referer != null) webRequest.Referer = referer;
            //获取服务器返回的资源   
            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                if (webResponse.Cookies.Count > 0) SetHostCookie(uri, webResponse.Cookies);
                using (Stream sream = webResponse.GetResponseStream())
                {
                    List<byte> list = new List<byte>();
                    while (true)
                    {
                        int data = sream.ReadByte();
                        if (data == -1)
                            break;
                        list.Add((byte)data);
                    }
                    File.WriteAllBytes(savePath, list.ToArray());
                }
            }

        }

        public String DoPostHttps(String url, string postDataStr, string rootDir, string referer = null)
        {
            StringBuilder content = new StringBuilder();
            Uri uri = new Uri(url);
            //这一句一定要写在创建连接的前面。使用回调的方法进行证书验证。
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            // 与指定URL创建HTTP请求
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowWriteStreamBuffering = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 UBrowser/5.2.3937.21 Safari/537.36";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Timeout = 30000;
            if (referer != null) request.Referer = referer;
            request.ReadWriteTimeout = 30000;
            request.KeepAlive = true;
            request.Accept = "application/json, text/javascript, */*; q=0.01";

            //创建证书文件
            X509Certificate objx509 = new X509Certificate(rootDir + "\\123.cer");
            //添加到请求里
            request.ClientCertificates.Add(objx509);
            request.CookieContainer = GetHostCookieContainer(uri);
            byte[] buffer = Encoding.UTF8.GetBytes(postDataStr);
            request.ContentLength = buffer.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(buffer, 0, buffer.Length);

            // 获取对应HTTP请求的响应
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.Cookies.Count > 0)
            {
                if (response.Cookies[0].Name != "SPRING_SECURITY_REMEMBER_ME_COOKIE") SetHostCookie(uri, response.Cookies);
            }
            BugFix_CookieDomain(GetHostCookieContainer(uri));
            // 获取响应流
            Stream responseStream = response.GetResponseStream();
            // 对接响应流(以"GBK"字符集)
            StreamReader sReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            // 开始读取数据
            Char[] sReaderBuffer = new Char[256];
            int count = sReader.Read(sReaderBuffer, 0, 256);
            while (count > 0)
            {
                String tempStr = new String(sReaderBuffer, 0, count);
                content.Append(tempStr);
                count = sReader.Read(sReaderBuffer, 0, 256);
            }
            // 读取结束
            sReader.Close();

            return content.ToString();

        }
        public string UrlAddRandom(string url)
        {
            Random r = new Random();
            int randomNum = r.Next(100000, 999999);
            var randomPar = "randomNum=" + randomNum;
            int pos = url.IndexOf("?");
            if (pos > 0)
            {
                if (pos != url.Length - 1) url += "&" + randomPar;
                else url = randomPar;
            }
            else url += "?" + randomPar;
            return url;
        }
        public String DoPost(String url, byte[] data)
        {
            //url = UrlAddRandom(url);
            Uri uri = new Uri(url);
            string html = "";
            StreamReader reader = null;
            HttpWebRequest webReqst = null;
            //如果是发送HTTPS请求 &nbsp;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                webReqst = WebRequest.Create(url) as HttpWebRequest;
                webReqst.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                webReqst = WebRequest.Create(url) as HttpWebRequest;
            }
            webReqst.Method = "POST";
            webReqst.UserAgent = DefaultUserAgent;
            webReqst.ContentType = "application/x-www-form-urlencoded";
            webReqst.ContentLength = data.Length;
            webReqst.CookieContainer = GetHostCookieContainer(uri);
            webReqst.Timeout = 30000;
            webReqst.ReadWriteTimeout = 30000;
            webReqst.AllowWriteStreamBuffering = false;
            //byte[] data = Encoding.Default.GetBytes(Content);
            Stream stream = webReqst.GetRequestStream();
            stream.Write(data, 0, data.Length);


            HttpWebResponse webResponse = (HttpWebResponse)webReqst.GetResponse();
            if (webResponse.Cookies.Count > 0) SetHostCookie(uri, webResponse.Cookies);
            BugFix_CookieDomain(GetHostCookieContainer(uri));
            if (webResponse.StatusCode == HttpStatusCode.OK && webResponse.ContentLength < 1024 * 1024)
            {
                stream = webResponse.GetResponseStream();
                stream.ReadTimeout = 30000;
                if (webResponse.ContentEncoding == "gzip")
                {
                    reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress), Encoding.UTF8);
                }
                else
                {
                    reader = new StreamReader(stream, Encoding.UTF8);
                }
                html = reader.ReadToEnd();
            }
            else
            {
                throw new Exception("server error:" + webResponse.StatusCode);
            }
            return html;
        }

    }

}
