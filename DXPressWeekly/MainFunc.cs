using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: AssemblyVersion("0.1.*")]

namespace DXPressWeekly
{
    public static class MainFunc
    {
        /// <summary>
        /// The wACCESS_TOKEN using to connect the wechat server.
        /// </summary>
        private static string _wAccessToken;

        /// <summary>
        /// The eACCESS_TOKEN using to connect the work-wechat server.
        /// </summary>
        private static string _eAccessToken;

        public static TraceWriter Log;

        [FunctionName("MainFunc")]
        public static void Run([TimerTrigger("0 0 20 * * SUN")] TimerInfo myTimer, TraceWriter log)
        {
            Log = log;
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            _wAccessToken = WeChat.GetAccessToken();
            log.Info("Get WeChat AccessToken Successfully.");
            _eAccessToken = WorkWeChat.GetAccessToken(Environment.GetEnvironmentVariable("WorkWeChatCorpID"), Environment.GetEnvironmentVariable("WorkWeChatCorpSECRET"));
            log.Info("Get WorkWeChat AccessToken Successfully.");

            WorkWeChat.Send(_eAccessToken, $"大夏通讯社一周统计 Beta\n{DateTime.Now.AddDays(-6).ToShortDateString()} ~ {DateTime.Now.ToShortDateString()}\n{IsDebug()}\nVersion. {Assembly.GetExecutingAssembly().GetName().Version}");
            log.Info("Head message successfully.");

            SendApprovalData();
        }

        private static string IsDebug()
        {
#if DEBUG
            return "DEBUG MODE";
#endif
            return "";
        }

        public static void SendApprovalData()
        {
            List<WorkWeChat.ApprovalData> list = WorkWeChat.GetApprovalData(Environment.GetEnvironmentVariable("WorkWeChatCorpID"),
                Environment.GetEnvironmentVariable("WorkWechatApprovalSecret"), 7);
            string sendStr = string.Empty;
            sendStr += "审批统计\n";
            sendStr += $"共有 {list.Count} 条申请项\n";
            // Count Approval
            var countSpName = from sp in list
                orderby sp.spname descending 
                group sp by sp.spname
                into g
                select new {g.Key, count = g.Count() };
            foreach (var item in countSpName)
            {
                sendStr += "\n" + item.Key + " 共 " + item.count;
                //var countPassSp = from sp in list
                //    where sp.spname == item.Key && sp.sp_status == WorkWeChat.ApprovalStatus.已通过
                //    select list.Count();
                int countPassSp = list.Count(w => w.spname == item.Key && w.sp_status == WorkWeChat.ApprovalStatus.已通过);
                sendStr += " 已通过 " + countPassSp + "\n";
                var deptcount = from sp in list
                    where sp.spname == item.Key
                    orderby sp.apply_org descending
                    group sp by sp.apply_org
                    into g
                    select new {g.Key, count = g.Count()};
                foreach (var deptitem in deptcount)
                {
                    string deptname = deptitem.Key;
                    string depttotal = deptitem.count.ToString();
                    string deptpass = list.Count(i =>
                        i.spname == item.Key && i.apply_org == deptitem.Key &&
                        i.sp_status == WorkWeChat.ApprovalStatus.已通过).ToString();
                    sendStr += $"- {deptname} 共{depttotal}过{deptpass}\n";
                }
            }

            //sendStr += "\n分部门统计\n";
            //var countSpOrg = from sp in list
            //    orderby sp.spname descending ,sp.apply_org descending 
            //    group sp by new {sp.spname, sp.apply_org}
            //    into g
            //    select new {g.Key.spname, g.Key.apply_org, count = g.Count()};
            //foreach (var item in countSpOrg)
            //{
            //    sendStr += $"{item.spname} {item.apply_org} {item.count} 人次\n";
            //}

            WorkWeChat.Send(_eAccessToken, sendStr);
            Log.Info("Finish SendApprovalData.");
        }

        
    }
}