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

namespace GoLogin
{    
    public class NetOperation
    {
        static private string cookieString;
        public void init()
        {
           
        }

        public bool systemOnline()
        {
            return false;
        }


        /*
        function SubmitData() {
            var objUserID = $E("ctl00_ctl00_CPH_CPH_txbUserID");
            var objPassword = $E("ctl00_ctl00_CPH_CPH_txbPassword");
            var objKey = $E("ctl00_ctl00_CPH_CPH_ckey");
            var md5string = MD5String(objPassword.value);
            var chcode = HashString(md5string, objKey.value);
            $E("ctl00_ctl00_CPH_CPH_chcode").value = chcode;
            objPassword.value = "";
        }
        function HashString(value, key) {
            return MD5String(value + key);
        }
         
         */
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

        private List<string> GetElementContent(string url, string elementName, string subElement)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();

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

        public bool startLogin(string userName, string password)
        {
            string loginUrl = "http://www.shweiqi.org/App/Auth/Login.aspx?app_id=1";
            Encoding encoding = Encoding.GetEncoding("UTF-8");

            List<string> elementContent = GetElementContent(loginUrl, "input", "value");            
            string __VIEWSTATE = elementContent[0];
            string __VIEWSTATEGENERATOR = elementContent[1];

            string passwordMd5 = MD5String(password);
            string ctl00_ctl00_CPH_CPH_ckey = "cc5988b1-4773-4bfd-b9b5-775161fb6daf";
            string chcode = HashString(passwordMd5, ctl00_ctl00_CPH_CPH_ckey);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("__EVENTARGUMENT", "");
            parameters.Add("__EVENTTARGET", "");
            parameters.Add("__LASTFOCUS", "");
            parameters.Add("__VIEWSTATE", __VIEWSTATE);
            parameters.Add("__VIEWSTATEGENERATOR", __VIEWSTATEGENERATOR);
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24chcode", chcode);

            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24chkRemember", "on");
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24ckey", ctl00_ctl00_CPH_CPH_ckey);
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24LoginButton", "立即登录");
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24txbPassword", "");
            parameters.Add("ctl00%24ctl00%24CPH%24CPH%24txbUserID", userName);

            UrlEncodeParams(parameters);

            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, parameters, null, null, encoding, null);
            cookieString = response.Headers["Set-Cookie"];


            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();
            return true;
        }

        public bool startLogout()
        {
            //window.location = "/App/Auth/Login.aspx?app_id=1&LoginAction=logout";
            return true;
        }

        public void selectMyChild()
        {
            string loginUrl = "http://home.51cto.com/index.php?s=/Index/doLogin";
            string userName = "userName";
            string password = "password";

            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("email", userName);
            parameters.Add("passwd", password);
            HttpWebResponse response = HttpWebResponseUtility.CreatePostHttpResponse(loginUrl, parameters, null, null, Encoding.UTF8, null);  
        }

        public void tryDo()
        {

        }

        public void updateTimer()
        {

        }
    }
    
    /// <summary>  
    /// 有关HTTP请求的辅助类  
    /// </summary>  
    public class HttpWebResponseUtility
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        /// <summary>  
        /// 创建GET方式的HTTP请求  
        /// </summary>  
        /// <param name="url">请求的URL</param>  
        /// <param name="timeout">请求的超时时间</param>  
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>  
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>  
        /// <returns></returns>  
        public static HttpWebResponse CreateGetHttpResponse(string url, int? timeout, string userAgent, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.UserAgent = DefaultUserAgent;
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            return request.GetResponse() as HttpWebResponse;
        }
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
        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters, int? timeout, string userAgent, Encoding requestEncoding, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            if (requestEncoding == null)
            {
                throw new ArgumentNullException("requestEncoding");
            }
            HttpWebRequest request = null;
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
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            else
            {
                request.UserAgent = DefaultUserAgent;
            }

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
            return request.GetResponse() as HttpWebResponse;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
    }  
}
