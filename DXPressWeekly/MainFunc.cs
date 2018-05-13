using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace DXPressWeekly
{
    public static class MainFunc
    {
        /// <summary>
        /// The wACCESS_TOKEN using to connect the wechat server.
        /// </summary>
        private static string wACCESS_TOKEN;
        /// <summary>
        /// The eACCESS_TOKEN using to connect the work-wechat server.
        /// </summary>
        private static string eACCESS_TOKEN;

        [FunctionName("MainFunc")]
        public static void Run([TimerTrigger("0 0 20 * * SUN")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            if (GetWeChatAccessToken())
                log.Info("Get WeChat AccessToken Successfully.");
            else
                log.Error("Error When Get WeChat AccessToken.");
            if (GetWorkWeChatAccessToken())
                log.Info("Get WorkWeChat AccessToken Successfully.");
            else
                log.Error("Error When Get WorkWeChat AccessToken.");
        }

        /// <summary>
        /// Connect the server to get wACCESS_TOKEN
        /// </summary>
        private static bool GetWeChatAccessToken()
        {
            string backjson = Restapi.HttpGet("https://api.weixin.qq.com/cgi-bin/token", $"grant_type=client_credential&appid={ConfigurationManager.AppSettings["WeChatAPPID"]}&secret={ConfigurationManager.AppSettings["WeChatAPPSECRET"]}");
            JObject rjson = JObject.Parse(backjson);
            string st = (string)rjson["access_token"];
            if (st != "")
            {
                wACCESS_TOKEN = st;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Connect the server to get eACCESS_TOKEN
        /// </summary>
        private static bool GetWorkWeChatAccessToken()
        {
            string backjson = Restapi.HttpGet("https://qyapi.weixin.qq.com/cgi-bin/gettoken", $"corpid={ConfigurationManager.AppSettings["WorkWeChatCorpID"]}&corpsecret={ConfigurationManager.AppSettings["WorkWeChatCorpSECRET"]}");
            JObject rjson = JObject.Parse(backjson);
            string st = (string)rjson["access_token"];
            if (st != "")
            {
                eACCESS_TOKEN = st;
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
