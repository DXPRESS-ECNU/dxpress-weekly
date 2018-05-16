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
        /// <param name="postDataStr">Get data</param>
        /// <returns></returns>
        public static string HttpGet(string url, string postDataStr)
        {
            HttpWebRequest request =
                (HttpWebRequest) WebRequest.Create(url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        /// <summary>
        /// The function to process POST
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="json">Post JSON</param>
        /// <returns></returns>
        public static string HttpPost(string url, string json)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "POST";
            httpWebRequest.Accept = "application/json; charset=utf-8";
#if DEBUG
            MainFunc.Log.Verbose(httpWebRequest.Address.AbsoluteUri);
#endif
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    return result;
                }
            }
        }
    }
}