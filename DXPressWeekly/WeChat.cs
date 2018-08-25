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

        public class ReadNum
        {
            public DateTime ref_date;
            public int int_page_read_count;
        }
        public class ArticleReadNum
        {
            public DateTime ref_date;
            public string title;
            public List<DailyReadTotal> ReadTotals;
            public class DailyReadTotal
            {
                public DateTime stat_date;
                public int int_page_read_count;
            }
        }

        public void GetReadData(out List<ReadNum> readNums, out List<ArticleReadNum> articleReadNums)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            readNums = new List<ReadNum>();
            string urlgetuserread = $"https://api.weixin.qq.com/datacube/getuserread?access_token={_accessToken}";
            for (int i = 1; i < 8; i++)
            {
                string date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                JObject posJObject = new JObject
                {
                    {"begin_date",date },
                    {"end_date",date }
                };
                string outJson = Restapi.HttpPost(urlgetuserread, posJObject.ToString());
                JObject outJObject = JObject.Parse(outJson);
                JArray dataJArray = JArray.FromObject(outJObject["list"]);
                if (dataJArray.Count == 0)
                {
                    continue;
                }
                JObject mainJObject = JObject.FromObject(dataJArray.First);
                ReadNum readNum = new ReadNum
                {
                    ref_date = DateTime.ParseExact((string)mainJObject["ref_date"], "yyyy-MM-dd", provider),
                    int_page_read_count = (int)mainJObject["int_page_read_count"]
                };
                readNums.Add(readNum);
            }

            articleReadNums = new List<ArticleReadNum>();
            string urlgetarticletotal = $"https://api.weixin.qq.com/datacube/getarticletotal?access_token={_accessToken}";
            for (int i = 1; i < 8; i++)
            {
                string date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                JObject posJObject = new JObject
                {
                    {"begin_date",date },
                    {"end_date",date }
                };
                string outJson = Restapi.HttpPost(urlgetarticletotal, posJObject.ToString());
                JObject outJObject = JObject.Parse(outJson);
                JArray dataJArray = JArray.FromObject(outJObject["list"]);
                foreach (var article in dataJArray.Children())
                {
                    JObject articleJObject = JObject.FromObject(article);
                    ArticleReadNum articleReadNum = new ArticleReadNum
                    {
                        ref_date = DateTime.ParseExact((string) articleJObject["ref_date"], "yyyy-MM-dd", provider),
                        title = (string) articleJObject["title"],
                        ReadTotals = new List<ArticleReadNum.DailyReadTotal>()
                    };
                    JArray dailyreadArray = JArray.FromObject(articleJObject["details"]);
                    foreach (var dailydata in dailyreadArray.Children())
                    {
                        JObject dailyreadJObject = JObject.FromObject(dailydata);
                        ArticleReadNum.DailyReadTotal dailyReadTotal = new ArticleReadNum.DailyReadTotal
                        {
                            stat_date = DateTime.ParseExact((string)dailyreadJObject["stat_date"], "yyyy-MM-dd", provider),
                            int_page_read_count = (int)dailyreadJObject["int_page_read_count"],
                        };
                        articleReadNum.ReadTotals.Add(dailyReadTotal);
                    }
                    articleReadNums.Add(articleReadNum);
                }
            }
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

        public void GetUserData(out List<UserSummary> userSummaries, out List<UserCumulate> userCumulates)
        {
            userSummaries = new List<UserSummary>();
            userCumulates = new List<UserCumulate>();
            CultureInfo provider = CultureInfo.InvariantCulture;

            string beginDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
            string endDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

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
