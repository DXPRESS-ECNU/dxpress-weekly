using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DXPressWeekly
{
    class Restapi
    {
        /// <summary>
        /// The function to process GET
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="dataStr">Get data</param>
        /// <returns></returns>
        public static string HttpGet(string url, string dataStr)
        {
            HttpWebRequest request =
                (HttpWebRequest) WebRequest.Create(url + (dataStr == "" ? "" : "?") + dataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            if (MainFunc.IsDebug)
            {
                MainFunc.Log.Verbose(request.RequestUri.AbsoluteUri);
            }
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream ?? throw new NullReferenceException());
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            if (MainFunc.IsDebug)
            {
                MainFunc.Log.Verbose(retString);
            }
            return retString;
        }

        /// <summary>
        /// The function to process POST
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="postJson">Post JSON</param>
        /// <returns></returns>
        public static string HttpPost(string url, string postJson)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "POST";
            httpWebRequest.Accept = "application/json; charset=utf-8";
            if (MainFunc.IsDebug)
            {
                MainFunc.Log.Verbose(url);
                MainFunc.Log.Verbose(postJson);
            }
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(postJson);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? throw new NullReferenceException()))
                {
                    var result = streamReader.ReadToEnd();
                    if (MainFunc.IsDebug)
                    {
                        MainFunc.Log.Verbose(result);
                    }
                    return result;
                }
            }
        }
    }
}