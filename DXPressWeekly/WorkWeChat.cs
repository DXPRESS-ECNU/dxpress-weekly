using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DXPressWeekly
{
    class WorkWeChat
    {
        private readonly string _accessToken;
        public WorkWeChat(string corpid, string secret)
        {
            _accessToken = GetAccessToken(corpid, secret);
        }
        /// <summary>
        /// Connect the server to get ACCESS_TOKEN
        /// </summary>
        private string GetAccessToken(string corpid, string secret)
        {
            string backjson = Restapi.HttpGet("https://qyapi.weixin.qq.com/cgi-bin/gettoken",
                $"corpid={corpid}&corpsecret={secret}");
            JObject rjson = JObject.Parse(backjson);
            string st = (string) rjson["access_token"];
            if (st != "")
            {
                return st;
            }
            throw new Exception((string) rjson["errmsg"]);
        }

        /// <summary>
        /// Send Message to Work WeChat
        /// </summary>
        /// <param name="message">the message body</param>
        public void Send(string message)
        {
            string touser = "";
#if DEBUG
            touser = Environment.GetEnvironmentVariable("DebugSend");
#endif
            JObject json = new JObject
            {
                {"touser", touser == "" ? "@all" : touser},
                {"msgtype", "text"},
                {"agentid", Environment.GetEnvironmentVariable("WorkWeChatAppId")},
                {"text", new JObject {{"content", message}}}
            };
            string url = "https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token=" + _accessToken;
            string returnjson =
                Restapi.HttpPost(url,
                    json.ToString());
            JObject rj = JObject.Parse(returnjson);
            if ((string) rj["errcode"] != "0")
            {
                throw new Exception($"Send Message ERR {(string) rj["errcode"]} {(string) rj["errmsg"]}");
            }
        }

        public enum ApprovalStatus
        {
            审批中 = 1,
            已通过 = 2,
            已驳回 = 3,
            已取消 = 4,
            通过后撤销 = 6
        }

        public class ApprovalData
        {
            public string spname;
            public ulong sp_num;
            public string apply_name;
            public string apply_user_id;
            public string apply_org;
            public ApprovalStatus sp_status;
            public int apply_time;
        }
        public List<ApprovalData> GetApprovalData(int timelength = 7)
        {
            List<ApprovalData> list = new List<ApprovalData>();

            string url = @"https://qyapi.weixin.qq.com/cgi-bin/corp/getapprovaldata?access_token=" + _accessToken;
            string requestJson = new JObject
            {
                {"starttime", (DateTime.Now.AddDays(-timelength).Date.ToUniversalTime().Ticks - 621355968000000000) / 10000000},
                {"endtime", (DateTime.Now.AddDays(-1).ToUniversalTime().Ticks - 621355968000000000) / 10000000}
            }.ToString();
            string returnjson =
                Restapi.HttpPost(url,
                    requestJson);
            JObject returnJObject = JObject.Parse(returnjson);
            if ((int)returnJObject["errcode"] != 0)
            {
                throw new Exception((string) returnJObject["errmsg"]);
            }
            JArray itemsArray = JArray.FromObject(returnJObject["data"]);
            foreach (var item in itemsArray.Children())
            {
                JObject itemJObject = JObject.Parse(item.ToString());
                ApprovalData data = new ApprovalData
                {
                    apply_name = (string)itemJObject["apply_name"],
                    apply_org = (string)itemJObject["apply_org"],
                    apply_time = (int)itemJObject["apply_time"],
                    apply_user_id = (string)itemJObject["apply_user_id"],
                    sp_num = (ulong)itemJObject["sp_num"],
                    sp_status = (ApprovalStatus)(int)itemJObject["sp_status"],
                    spname = (string)itemJObject["spname"]
                };
                list.Add(data);
            }
            //return data;
            return list;
        }
    }
}