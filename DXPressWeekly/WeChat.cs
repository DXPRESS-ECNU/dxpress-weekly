using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DXPressWeekly
{
    class WeChat
    {
        /// <summary>
        /// Connect the server to get ACCESS_TOKEN
        /// </summary>
        public static string GetAccessToken()
        {
            string backjson = Restapi.HttpGet("https://api.weixin.qq.com/cgi-bin/token",
                $"grant_type=client_credential&appid={Environment.GetEnvironmentVariable("WeChatAPPID")}&secret={Environment.GetEnvironmentVariable("WeChatAPPSECRET")}");
            JObject rjson = JObject.Parse(backjson);
            string st = (string)rjson["access_token"];
            if (st != "")
            {
                return st;
            }
            else
            {
                throw new Exception((string) rjson["errmsg"]);
            }
        }
    }
}
