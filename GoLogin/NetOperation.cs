using System;  
using System.Collections.Generic;  
using System.Linq;  
using System.Text;  
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;  
using System.DirectoryServices.Protocols;  
using System.ServiceModel.Security;  
using System.Net;  
using System.IO;  
using System.IO.Compression;  
using System.Text.RegularExpressions;
using System.Web;

enum StepType
{
    stepIdle = 0,
    stepLogined,
    stepLoginedRedirToActivityInterface,
    stepGetAndSelActivity,
    stepSignupActivity,
}

namespace GoLogin
{    
    public class NetOperation
    {
        static private CookieCollection loginCookies;
        static private string session;
        static private HttpWebRequest request;

        static private StepType currentStep = StepType.stepIdle;
        static private StepType backupStep = StepType.stepIdle;

        public void init()
        {
            loginCookies = null;
            session = "";
            request = null;
        }

        public bool systemOnline()
        {
            return false;
        }

        private string HashString(string input,string key)
        {
            return MD5String(input + key);
        }

        private string MD5String(string input)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(input));
            StringBuilder passwordMd5 = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                passwordMd5.Append(result[i].ToString("x2"));
            }
            return passwordMd5.ToString();
        }

        private string GetCookieValue(string cookie)
        {
            Regex regex = new Regex("=.*?;");
            Match value = regex.Match(cookie);
            string cookieValue = value.Groups[0].Value;
            return cookieValue.Substring(1, cookieValue.Length - 2);
        }

        private long currentTimeStamp()
        {
            DateTime dtnow = DateTime.Now;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            long timeStamp = (long)(dtnow - startTime).TotalSeconds; // 相差秒数
            return timeStamp;
        }

        private CookieCollection genAccessCookies(string cookiestring)
        {          
            CookieCollection ret = new CookieCollection();
            long timeStamp = currentTimeStamp();
            string formatedTime = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");

            session = GetCookieValue(cookiestring);
            ret.Add(new Cookie("ASP.NET_SessionId", session, "/", "www.shweiqi.org"));
            ret.Add(new Cookie("Hm_lvt_cea027ed183c3538b8429f1f87a894c8", timeStamp.ToString(), "/", ".shweiqi.org"));
            ret.Add(new Cookie("Hm_lpvt_cea027ed183c3538b8429f1f87a894c8", timeStamp.ToString(), "/", ".shweiqi.org"));
           // ret.Add(new Cookie("login_time", HttpUtility.UrlEncode(formatedTime, Encoding.GetEncoding("UTF-8")), "/", ".shweiqi.org"));
            return ret;
        }

        private List<string> GetElementContent(string url, string elementName, string subElement)
        {
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Referer = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationList.aspx?app_id=102&";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0) Gecko/20100101 Firefox/55.0";
            request.KeepAlive = true;
         
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();

            string cookie = response.Headers.Get("Set-Cookie");
            loginCookies = genAccessCookies(cookie);

            StringBuilder regexStr = new StringBuilder("(?is)<");
            regexStr.Append(elementName).Append("[^>]*?").Append(subElement).Append(@"=(['""\s]?)([^'""\s]+)\1[^>]*?>");
            Regex regex = new Regex(regexStr.ToString());
            MatchCollection match = regex.Matches(content);
            List<string> values = new List<string>();
            foreach (Match m in match)
            {
                values.Add(m.Groups[2].Value);
            }
            return values;
        }
        private void UrlEncodeParams(Dictionary<string, string> formParams)
        {
            for (int i = 0, len = formParams.Keys.Count(); i < len; i++)
            {
                string key = formParams.Keys.ElementAtOrDefault(i);
                formParams[key] = HttpUtility.UrlEncode(formParams[key], Encoding.GetEncoding("UTF-8"));
            }
        }

        public bool loginCookiesNotContain(string key)
        {
            foreach(Cookie ck in loginCookies)
            {
                if (ck.Name == key)
                    return false;
            }
            return true;
        }

        public bool startLogin(string userName, string password)
        {
            string loginUrl = "http://www.shweiqi.org/App/Auth/Login.aspx?app_id=1";
            string refUrl = loginUrl;// +rdValue;

            List<string> elementContent = GetElementContent(loginUrl, "input", "value");            
            string __VIEWSTATE = elementContent[0];
            string __VIEWSTATEGENERATOR = elementContent[1];
            string buttonIdChs = elementContent[2];
            string passwordMd5 = MD5String(password);
            string ctl00_ctl00_CPH_CPH_ckey = elementContent[3];
            string chcode = HashString(passwordMd5, ctl00_ctl00_CPH_CPH_ckey);


            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("__EVENTARGUMENT", "");
            parameters.Add("__EVENTTARGET", "");
            parameters.Add("__LASTFOCUS", "");
            parameters.Add("__VIEWSTATE", __VIEWSTATE);
            parameters.Add("__VIEWSTATEGENERATOR", __VIEWSTATEGENERATOR);
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24txbUserID", userName);
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24txbPassword", "");
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24chkRemember", "on");
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24LoginButton", buttonIdChs);           
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24chcode", chcode);
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24ckey", ctl00_ctl00_CPH_CPH_ckey);

            UrlEncodeParams(parameters);

            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded", loginCookies, "POST", false);

            string logincookies = HttpWebResponseUtility.request.Headers.Get("Cookie");

            char[] charSeparators = new char[] { ';' };
            char[] charSeparators2 = new char[] { '=' };
            string[] cookiespairs = logincookies.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (cookiespairs != null)
            {
                foreach(string cookieskv in cookiespairs)
                {
                    string[] kv = cookieskv.Split(charSeparators2, StringSplitOptions.RemoveEmptyEntries);
                    if (kv != null)
                    {
                        string key = kv[0].Trim();
                        string value = kv[1].Trim();
                        if (kv.Length == 3)
                            value = value + "=" + kv[2].Trim();

                        if(loginCookiesNotContain(key))
                        {
                            loginCookies.Add(new Cookie(key, value, "/", "www.shweiqi.org"));
                        }
                    }
                }
            }

            /*
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();
            */  

            if(true)
            {
                //login ok
                currentStep = StepType.stepLogined;
            }

            return true;
        }

        public void getExamListUrl()
        {
            string loginUrl = "http://www.shweiqi.org/App/Center/Default.aspx?app_id=102&";
            string refUrl = "http://www.shweiqi.org/App/Auth/Login.aspx?app_id=1";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded", loginCookies, "GET", false);

            currentStep = StepType.stepLoginedRedirToActivityInterface;           
        }

        public void getExamList()
        {
            string loginUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationList.aspx?app_id=102&";
            string refUrl = "http://www.shweiqi.org/App/Auth/Login.aspx?app_id=1";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, refUrl, parameters, null, null, null, loginCookies, "GET", false);

            currentStep = StepType.stepGetAndSelActivity;
        }

        public void getExamList(string url)
        {
            string loginUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationAjax.aspx?app_id=102&&ajaxtime=" + currentTimeStamp().ToString();
            string refUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationList.aspx?app_id=102&";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("pagerJson", "{ \"SortColumn\": \"\", \"SortMode\":\"asc\", \"PageSize\": \"2147483647\", \"PageNum\":\"1\"}");
            parameters.Add("searchCondtionJson", "{\"dataRange$ignore_search\":\"includeHistory\"}");
            parameters.Add("AjaxMethod", "Search");
            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded; charset=UTF-8", loginCookies, "POST", true);

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();           
        }
        
        public bool trySelExam()
        {
            bool ret = false;

            string loginUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationAjax.aspx?app_id=102&&ajaxtime=" + currentTimeStamp().ToString();
            string refUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationList.aspx?app_id=102&";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("pagerJson", "{ \"SortColumn\": \"\", \"SortMode\":\"asc\", \"PageSize\": \"2147483647\", \"PageNum\":\"1\"}");
            parameters.Add("searchCondtionJson", "{\"dataRange$ignore_search\":\"includeHistory\"}");
            parameters.Add("AjaxMethod", "Search");
            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded", loginCookies, "GET", false);

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();
            return ret;
        }

        public bool startLogout()
        {
            //window.location = "/App/Auth/Login.aspx?app_id=1&LoginAction=logout";
            return true;
        }

        public void selectMyChild()
        {

        }

        public void tryDo()
        {

        }

        public string updateTimer()
        {
            //超速更新
            if (currentStep != backupStep)
            {
                switch (currentStep)
                {
                    case StepType.stepIdle:
                        return "登出操作成功";
                    case StepType.stepLogined:
                        getExamListUrl();
                        return "\n获取Session等数据";
                    case StepType.stepLoginedRedirToActivityInterface:
                        getExamList();
                        return "\n获取赛事信息";
                    case StepType.stepGetAndSelActivity:
                        return null;
                    case StepType.stepSignupActivity:
                        return null;
                }
                backupStep = currentStep;
            }
            return null;
        }

        public string updateSlowTimer()
        {
            //仿人工操作
            switch (currentStep)
            {
                case StepType.stepIdle:
                    return null;
                case StepType.stepLogined:
                    return null;
                case StepType.stepLoginedRedirToActivityInterface:
                    return null;
                case StepType.stepGetAndSelActivity:
                    return null;
                case StepType.stepSignupActivity:
                    return null;
            }
            backupStep = currentStep;
            return null;
        }

    }
    
    /// <summary>  
    /// 有关HTTP请求的辅助类  
    /// </summary>  
    public class HttpWebResponseUtility
    {
        static public HttpWebRequest request;
        /// <summary>  
        /// 创建POST方式的HTTP请求  
        /// </summary>  
        /// <param name="url">请求的URL</param>  
        /// <param name="parameters">随同请求POST的参数名称及参数值字典</param>  
        /// <param name="timeout">请求的超时时间</param>  
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>  
        /// <param name="requestEncoding">发送HTTP请求时所用的编码</param>  
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>  
        /// <returns></returns>  
        public static HttpWebResponse CreatePostHttpResponse(string url, string refUrl, IDictionary<string, string> parameters, 
            int? timeout, string userAgent, string ContentType, CookieCollection cookies, string method, bool getXml)
        {
            Encoding requestEncoding = Encoding.GetEncoding("UTF-8");
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
          
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {           
                request = WebRequest.Create(url) as HttpWebRequest;
            }

            //string cookiestring = "";
            //if (cookies != null)
            //{
            //    int i = 0;
            //    foreach( Cookie ck in cookies)
            //    {
            //        if (i != cookies.Count - 1)
            //            cookiestring += ck.Name + "=" + ck.Value + "; ";
            //        else
            //            cookiestring += ck.Name + "=" + ck.Value;
            //        i++;
            //    }
            //}

            if (method == null)
                request.Method = "POST";
            else
                request.Method = method;

            request.Host = "www.shweiqi.org";
            request.KeepAlive = true;
            if (getXml)
            {
                request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7.2");
            }
            else
            {
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            }
            request.Headers.Add("Accept-Encoding", "gzip,deflate");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Accept-Language","zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3");
            request.ContentType = ContentType;


            //request.ContentLength = 818;


            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            else
            {
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0) Gecko/20100101 Firefox/55.0";
            }

            if (!string.IsNullOrEmpty(refUrl))
                request.Referer = refUrl;

            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            //如果需要POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                    }
                    i++;
                }
                byte[] data = requestEncoding.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            HttpWebResponse hwr = request.GetResponse() as HttpWebResponse;

            // Displays each header and it's key associated with the response.
            for (int i = 0; i < request.Headers.Count; ++i)
                Console.WriteLine("\nHeader Name:{0}, Value :{1}", request.Headers.Keys[i], request.Headers[i]); 

            return hwr;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
    }  
}
