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
        private readonly string _accessToken;
        public WeChat(string appid, string secret)
        {
            _accessToken = GetAccessToken(appid, secret);
        }
        /// <summary>
        /// Connect the server to get ACCESS_TOKEN
        /// </summary>
        public string GetAccessToken(string appid, string secret)
        {
            string backjson = Restapi.HttpGet("https://api.weixin.qq.com/cgi-bin/token",
                $"grant_type=client_credential&appid={appid}&secret={secret}");
            JObject rjson = JObject.Parse(backjson);
            string st = (string)rjson["access_token"];
            if (st != "")
            {
                return st;
            }
            throw new Exception((string) rjson["errmsg"]);
        }

        public void GetReadData()
        {
            // TODO Add WeChat Read Data Analysis

        }

        public void GetUserData()
        {
            // TODO Add User Data Analysis
        }
    }
}
