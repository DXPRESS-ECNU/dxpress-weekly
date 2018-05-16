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
        /// <summary>
        /// Connect the server to get eACCESS_TOKEN
        /// </summary>
        public static string GetAccessToken(string corpid, string secret)
        {
            string backjson = Restapi.HttpGet("https://qyapi.weixin.qq.com/cgi-bin/gettoken",
                $"corpid={corpid}&corpsecret={secret}");
            JObject rjson = JObject.Parse(backjson);
            string st = (string) rjson["access_token"];
            if (st != "")
            {
                return st;
            }
            else
            {
                throw new Exception((string) rjson["errmsg"]);
            }
        }

        /// <summary>
        /// Send Message to Work WeChat
        /// </summary>
        /// <param name="accessToken">the accesstoken of app to send message</param>
        /// <param name="message">the message body</param>
        public static void Send(string accessToken, string message)
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
            string url = "https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token=" + accessToken;
#if DEBUG
            MainFunc.Log.Verbose(url);
            MainFunc.Log.Verbose(json.ToString());
#endif
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
        public static List<ApprovalData> GetApprovalData(string copid, string secret, int timelength = 7)
        {
            string ak = GetAccessToken(copid, secret);
            //DataTable data = new DataTable();
            //data.Columns.Add("spname");
            //data.Columns.Add("sp_num");
            //data.Columns.Add("apply_name");
            //data.Columns.Add("apply_user_id");
            //data.Columns.Add("apply_org");
            //data.Columns.Add("sp_status");
            //data.Columns.Add("apply_time");
            List<ApprovalData> list = new List<ApprovalData>();

            string url = @"https://qyapi.weixin.qq.com/cgi-bin/corp/getapprovaldata?access_token=" + ak;
            string requestJson = new JObject
            {
                {"starttime", (DateTime.Now.AddDays(1 - timelength).Date.ToUniversalTime().Ticks - 621355968000000000) / 10000000},
                {"endtime", (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000}
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
                //DataRow row = data.NewRow();
                //row["spname"] = (string) itemJObject["spname"];
                //row["apply_name"] = (string) itemJObject["apply_name"];
                //row["apply_user_id"] = (string) itemJObject["apply_user_id"];
                //row["apply_org"] = (string) itemJObject["apply_org"];
                //row["sp_status"] = (ApprovalStatus) (int) itemJObject["sp_status"];
                //row["sp_num"] = (int) itemJObject["sp_num"];
                //row["apply_time"] = (int) itemJObject["apply_time"];
                //data.Rows.Add(row);
                ApprovalData data = new ApprovalData();
                data.apply_name = (string)itemJObject["apply_name"];
                data.apply_org = (string)itemJObject["apply_org"];
                data.apply_time = (int)itemJObject["apply_time"];
                data.apply_user_id = (string)itemJObject["apply_user_id"];
                data.sp_num = (ulong)itemJObject["sp_num"];
                data.sp_status = (ApprovalStatus)(int)itemJObject["sp_status"];
                data.spname = (string)itemJObject["spname"];
                list.Add(data);
            }
            //return data;
            return list;
        }
    }
}