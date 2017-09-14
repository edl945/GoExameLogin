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
using System.Net.Cache;
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
        static private CookieCollection mLoginCookies;
        static private string mSession;
        static private HttpWebRequest mRequest;

        static private StepType currentStep = StepType.stepIdle;
        static private StepType backupStep = StepType.stepIdle;

        public void init()
        {
            mLoginCookies = null;
            mSession = "";
            mRequest = null;
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

            mSession = GetCookieValue(cookiestring);
            ret.Add(new Cookie("ASP.NET_SessionId", mSession, "/", "www.shweiqi.org"));
            ret.Add(new Cookie("Hm_lvt_cea027ed183c3538b8429f1f87a894c8", timeStamp.ToString(), "/", ".shweiqi.org"));
            ret.Add(new Cookie("Hm_lpvt_cea027ed183c3538b8429f1f87a894c8", (timeStamp+2000).ToString(), "/", ".shweiqi.org"));
           // ret.Add(new Cookie("login_time", HttpUtility.UrlEncode(formatedTime, Encoding.GetEncoding("UTF-8")), "/", ".shweiqi.org"));
            return ret;
        }

        private List<string> GetElementContent(string url, string elementName, string subElement)
        {
            mRequest = (HttpWebRequest)WebRequest.Create(url);
            mRequest.Method = "GET";
            mRequest.Referer = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationList.aspx?app_id=102&";
            mRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            mRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0) Gecko/20100101 Firefox/55.0";
            mRequest.KeepAlive = true;
         
            HttpWebResponse response = (HttpWebResponse)mRequest.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();

            string cookie = response.Headers.Get("Set-Cookie");
            mLoginCookies = genAccessCookies(cookie);

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
            foreach(Cookie ck in mLoginCookies)
            {
                if (ck.Name == key)
                    return false;
            }
            return true;
        }

        //登陆请求
        public bool startLogin(string userName, string password)
        {
            string loginUrl = "http://www.shweiqi.org/App/Auth/Login.aspx?app_id=1";
            string refUrl = "http://www.shweiqi.org";

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

            HttpWebResponse response = HttpWebResponseUtility.GetHttpWebResponseNoRedirect(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded", mLoginCookies, "POST", false);

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
                            mLoginCookies.Add(new Cookie(key, value, "/", "www.shweiqi.org"));
                        }
                    }
                }
            }
           
            if (response.StatusCode == HttpStatusCode.Found)//302
                Console.WriteLine("HttpStatusCode.Found");

            response.Close();
            if(true)
            {
                //login ok
                currentStep = StepType.stepLogined;
            }

            return true;
        }

        //登陆成功后先来拉取一下数据 step 2
        public void getExamListUrl()
        {
            string loginUrl = "http://www.shweiqi.org/App/Center/Default.aspx?app_id=102&";
            string refUrl = "http://www.shweiqi.org/";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            HttpWebResponse response = HttpWebResponseUtility.GetHttpWebResponseNoRedirect(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded", mLoginCookies, "GET", false);
            response.Close();
            currentStep = StepType.stepLoginedRedirToActivityInterface;           
        }

        //step 3
        public void getExamList()
        {
            string loginUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationList.aspx?app_id=102&";
            string refUrl = "http://www.shweiqi.org/";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            HttpWebResponse response = HttpWebResponseUtility.GetHttpWebResponseNoRedirect(loginUrl, refUrl, parameters, null, null, null, mLoginCookies, "GET", false);
            response.Close();
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
            HttpWebResponse response = HttpWebResponseUtility.GetHttpWebResponseNoRedirect(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded; charset=UTF-8", mLoginCookies, "POST", true);

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();
            response.Close();
        }
        
        public bool trySelExam()
        {
            bool ret = false;

            string loginUrl = "http://www.shweiqi.org/App/Center/ExamRegistration/ExamRegistrationAjax.aspx?app_id=102&&ajaxtime=" + currentTimeStamp().ToString();
            string refUrl = "http://www.shweiqi.org/";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("pagerJson", "{ \"SortColumn\": \"\", \"SortMode\":\"asc\", \"PageSize\": \"2147483647\", \"PageNum\":\"1\"}");
            parameters.Add("searchCondtionJson", "{\"dataRange$ignore_search\":\"includeHistory\"}");
            parameters.Add("AjaxMethod", "Search");
            HttpWebResponse response = HttpWebResponseUtility.GetHttpWebResponseNoRedirect(loginUrl, refUrl, parameters, null, null, "application/x-www-form-urlencoded", mLoginCookies, "GET", false);

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
                        return "获取赛事信息";
                    case StepType.stepGetAndSelActivity:
                        getExamList("myExam");
                        return "\n获取已参加过的赛事";
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
        public static HttpWebResponse GetHttpWebResponseNoRedirect(string url, string refUrl, IDictionary<string, string> parameters, 
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
                request.AllowAutoRedirect = false;

            }
            else
            {
                request.AllowAutoRedirect = true;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            }
            request.Headers.Add("Accept-Encoding", "gzip,deflate");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Accept-Language","zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3");
            request.ContentType = ContentType;
            request.Timeout = 20000;


            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;

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
            HttpWebResponse hwr = null;
            try
            {
                hwr = request.GetResponse() as HttpWebResponse;
                if (hwr.StatusCode == HttpStatusCode.Found)//302
                    Console.WriteLine("HttpStatusCode.Found");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
                Console.WriteLine(ex.GetBaseException().ToString());
            }

            // Displays each header and it's key associated with the response.
            for (int i = 0; i < request.Headers.Count; ++i)
                Console.WriteLine("\nHeader Name:{0}, Value :{1}", request.Headers.Keys[i], request.Headers[i]);

            request.Abort();
            return hwr;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
    }  
}
