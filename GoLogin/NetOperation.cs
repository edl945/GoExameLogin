using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.IO;

namespace GoLogin
{
    public class NetOperation
    {
        public void init()
        {
            CookieContainer cc = new CookieContainer();
            string url = "http://mailbeta.263.net/xmweb";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = cc;
            string user = "user"; //用户名
            string pass = "pass"; //密码
            string data = "func=login&usr=" + HttpUtility.UrlEncode(user) + "&sel_domain=263.net&domain=263.net&pass=" + HttpUtility.UrlEncode(pass) + "&image2.x=0&image2.y=0&verifypcookie=&verifypip=";
            request.ContentLength = data.Length;
            StreamWriter writer = new
            StreamWriter(request.GetRequestStream(), Encoding.ASCII);
            writer.Write(data);
            writer.Flush();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string encoding = response.ContentEncoding;
            if (encoding == null || encoding.Length < 1)
            {
                encoding = "UTF-8"; //默认编码
            }

            StreamReader reader = new StreamReader(response.GetResponseStream(),
            Encoding.GetEncoding(encoding));
            data = reader.ReadToEnd();
            Console.WriteLine(data);
            response.Close();
            int index = data.IndexOf("sid=");
            string sid = data.Substring(index + 4, data.IndexOf("&", index) - index - 4);
            Console.WriteLine(sid);
            url = "http://wm11.263.net/xmweb?func=mlst&act=show&usr=" + user + "&sid=" + sid + "&fid=1&desc=1&pg=1&searchword=&searchtype=&searchsub=&searchfd=&sort=4";
            request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = cc;
            foreach (Cookie cookie in response.Cookies)
            {
                cc.Add(cookie);
            }

            response = (HttpWebResponse)request.GetResponse();
            encoding = response.ContentEncoding;
            if (encoding == null || encoding.Length < 1)
            {
                encoding = "UTF-8"; //默认编码                
            }

            reader = new StreamReader(response.GetResponseStream(),
                Encoding.GetEncoding(encoding));
            data = reader.ReadToEnd();
            Console.WriteLine(data);
            response.Close();
            /*
            这段代码的意思是，模拟登陆263邮箱，别列出收件箱内容（html代码）
            Related posts: 
            1.C# WebRequest处理Https请求
            2.C# WebRequest处理Https请求之使用客户端证书
            */

        }
    }
}
