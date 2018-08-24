using System;
using System.Collections.Generic;
using System.Globalization;
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

        public class UserSummary
        {
            public DateTime ref_date;
            public int user_source;
            public int new_user;
            public int cancel_user;
        }
        public class UserCumulate
        {
            public DateTime ref_date;
            public int cumulate_user;
        }

        public void GetUserData(out List<UserSummary> userSummaries, out List<UserCumulate> userCumulates, int timeLength = 7)
        {
            userSummaries = new List<UserSummary>();
            userCumulates = new List<UserCumulate>();
            CultureInfo provider = CultureInfo.InvariantCulture;

            string beginDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            string endDate = DateTime.Now.AddDays(-timeLength).ToString("yyyy-MM-dd");

            JObject posJObject = new JObject
            {
                {"begin_date",beginDate },
                {"end_date",endDate }
            };
            string urlGetusersummary = $"https://api.weixin.qq.com/datacube/getusersummary?access_token={_accessToken}";
            string urlGetusercumulate = $"https://api.weixin.qq.com/datacube/getusercumulate?access_token={_accessToken}";

            string outGetusersummary = Restapi.HttpPost(urlGetusersummary, posJObject.ToString());
            JObject outGetusersummaryJObject = JObject.Parse(outGetusersummary);
            JArray usersummaryJArray = JArray.FromObject(outGetusersummaryJObject["list"]);
            foreach (var usersummary in usersummaryJArray.Children())
            {
                JObject itemJObject = JObject.FromObject(usersummary);
                UserSummary userSummary = new UserSummary
                {
                    ref_date = DateTime.ParseExact((string)itemJObject["ref_date"],"yyyy-MM-dd", provider),
                    user_source = (int)itemJObject["user_source"],
                    new_user = (int)itemJObject["new_user"],
                    cancel_user = (int)itemJObject["cancel_user"]
                };
                userSummaries.Add(userSummary);
            }

            string outGetusercumulate = Restapi.HttpPost(urlGetusercumulate, posJObject.ToString());
            JObject outGetusercumulateJObject = JObject.Parse(outGetusercumulate);
            JArray usercumulateJArray = JArray.FromObject(outGetusercumulateJObject["list"]);
            foreach (var usercumulate in usercumulateJArray.Children())
            {
                JObject itemJObject = JObject.FromObject(usercumulate);
                UserCumulate userCumulate = new UserCumulate
                {
                    ref_date = DateTime.ParseExact((string)itemJObject["ref_date"], "yyyy-MM-dd", provider),
                    cumulate_user = (int)itemJObject["cumulate_user"]
                };
                userCumulates.Add(userCumulate);
            }

        }
    }
}
