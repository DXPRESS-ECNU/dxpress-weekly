using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public static TraceWriter Log;

        [FunctionName("MainFunc")]
        public static void Run([TimerTrigger("0 0 20 * * SUN")] TimerInfo myTimer, TraceWriter log)
        {
            Log = log;
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            wACCESS_TOKEN = WeChat.GetAccessToken();
            log.Info("Get WeChat AccessToken Successfully.");
            eACCESS_TOKEN = WorkWeChat.GetAccessToken(Environment.GetEnvironmentVariable("WorkWeChatCorpID"), Environment.GetEnvironmentVariable("WorkWeChatCorpSECRET"));
            log.Info("Get WorkWeChat AccessToken Successfully.");

            WorkWeChat.Send(eACCESS_TOKEN, $"大夏通讯社一周统计\n{DateTime.Now.AddDays(-6).ToShortDateString()} ~ {DateTime.Now.ToShortDateString()}");
            log.Info("Head message successfully.");

            SendApprovalData();
        }

        public static void SendApprovalData()
        {
            List<WorkWeChat.ApprovalData> list = WorkWeChat.GetApprovalData(Environment.GetEnvironmentVariable("WorkWeChatCorpID"),
                Environment.GetEnvironmentVariable("WorkWechatApprovalSecret"), 7);
            string sendStr = string.Empty;
            sendStr += "审批统计\n\n";
            sendStr += $"共有 {list.Count} 条申请项\n";
            // Count Approval
            var countSpName = from sp in list
                orderby sp.spname descending 
                group sp by sp.spname
                into g
                select new {g.Key, count = g.Count() };
            foreach (var item in countSpName)
            {
                sendStr += item.Key + " 共 " + item.count;
                //var countPassSp = from sp in list
                //    where sp.spname == item.Key && sp.sp_status == WorkWeChat.ApprovalStatus.已通过
                //    select list.Count();
                int countPassSp = list.Count(w => w.spname == item.Key && w.sp_status == WorkWeChat.ApprovalStatus.已通过);
                sendStr += " 已通过 " + countPassSp + "\n";
            }

            sendStr += "\n分部门统计\n";
            var countSpOrg = from sp in list
                orderby sp.spname descending ,sp.apply_org descending 
                group sp by new {sp.spname, sp.apply_org}
                into g
                select new {g.Key.spname, g.Key.apply_org, count = g.Count()};
            foreach (var item in countSpOrg)
            {
                sendStr += $"{item.spname} {item.apply_org} {item.count} 人次\n";
            }

            WorkWeChat.Send(eACCESS_TOKEN, sendStr);
            Log.Info("Finish SendApprovalData.");
        }

        
    }
}