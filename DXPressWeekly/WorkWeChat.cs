using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DXPressWeekly
{
    class WorkWeChat
    {
        public static bool Send(string accessToken, string message)
        {
            string touser = "";
#if DEBUG
            touser = Environment.GetEnvironmentVariable("DebugSend");
#endif
            JObject cont = new JObject
            {
                {"content", message}
            };
            JObject json = new JObject
            {
                {"touser", touser == "" ? "@all" : touser},
                {"msgtype", "text"},
                {"agentid", Environment.GetEnvironmentVariable("WorkWeChatAppId")},
                {"text", cont.ToString()}
            };
            string url = "https://qyapi.weixin.qq.com/cgi-bin/message/send?accessToken=" + accessToken;
#if DEBUG
            MainFunc.Log.Verbose(url);
#endif
            string returnjson =
                Restapi.HttpPost(url,
                    json.ToString());
            JObject rj = JObject.Parse(returnjson);
            if ((string) rj["errcode"] == "0")
            {
                return true;
            }
            else
            {
                MainFunc.Log.Error($"Send Message ERR {(string) rj["errcode"]} {(string) rj["errmsg"]}");
                return false;
            }
        }
    }
}