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
        private static WeChat _weChat;
        private static WorkWeChat _workWeChat;
        public static TraceWriter Log;

        [FunctionName("MainFunc")]
        public static void Run([TimerTrigger("0 0 20 * * SUN")] TimerInfo myTimer, TraceWriter log)
        {
            Log = log;
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            _weChat = new WeChat(Environment.GetEnvironmentVariable("WeChatAPPID"), Environment.GetEnvironmentVariable("WeChatAPPSECRET"));
            log.Info("Get WeChat AccessToken Successfully.");
            _workWeChat = new WorkWeChat(Environment.GetEnvironmentVariable("WorkWeChatCorpID"), Environment.GetEnvironmentVariable("WorkWeChatCorpSECRET"));
            log.Info("Get WorkWeChat AccessToken Successfully.");

            _workWeChat.Send($"大夏通讯社一周统计 Beta\n{DateTime.Now.AddDays(-6).ToShortDateString()} ~ {DateTime.Now.ToShortDateString()}\n{IsDebug()}\nVersion. {Assembly.GetExecutingAssembly().GetName().Version}");
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
            List<WorkWeChat.ApprovalData> list = _workWeChat.GetApprovalData(7);
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

            _workWeChat.Send(sendStr);
            Log.Info("Finish SendApprovalData.");
        }

        
    }
}