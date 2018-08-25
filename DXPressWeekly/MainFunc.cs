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

[assembly: AssemblyVersion("0.2.*")]

namespace DXPressWeekly
{
    public static class MainFunc
    {
        private static WeChat _weChat;
        private static WorkWeChat _workWeChat;
        public static TraceWriter Log;
        public static bool IsDebug = true;

        [FunctionName("MainFunc")]
        public static void Run([TimerTrigger("0 0 20 * * SUN")] TimerInfo myTimer, TraceWriter log)
        {
            Log = log;
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            if (Environment.GetEnvironmentVariable("DEBUGMODE") == "0")
            {
                IsDebug = false;
            }

            _weChat = new WeChat(Environment.GetEnvironmentVariable("WeChatAPPID"), Environment.GetEnvironmentVariable("WeChatAPPSECRET"));
            log.Info("Get WeChat AccessToken Successfully.");
            _workWeChat = new WorkWeChat(Environment.GetEnvironmentVariable("WorkWeChatCorpID"), Environment.GetEnvironmentVariable("WorkWeChatCorpSECRET"), Environment.GetEnvironmentVariable("WorkWechatApprovalSecret"));
            log.Info("Get WorkWeChat AccessToken Successfully.");

            _workWeChat.Send($"大夏通讯社一周统计 Beta\n{DateTime.Now.AddDays(-7).ToShortDateString()} ~ {DateTime.Now.AddDays(-1).ToShortDateString()}\n{(IsDebug ? "DEBUG MODE":"")}\nVersion. {Assembly.GetExecutingAssembly().GetName().Version}");
            log.Info("Head message successfully.");

            SendApprovalData();
            SendUserAnalysis();
            SendReadAnalysis();
        }

        public static void SendApprovalData()
        {
            List<WorkWeChat.ApprovalData> list = _workWeChat.GetApprovalData();
            string sendStr = "审批统计\n";
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

        private static void SendUserAnalysis()
        {
            _weChat.GetUserData(out List<WeChat.UserSummary> userSummaries, out List<WeChat.UserCumulate> userCumulates);
            string sendStr = "订阅统计\n";
            var totalSubscriber = userCumulates.OrderByDescending(i => i.ref_date).Select(i => i.cumulate_user).First();
            sendStr += $"当前订阅人数： {totalSubscriber.ToString()}\n";
            var newSubscriber = userSummaries.Where(i => i.user_source == 0).Select(i => i.new_user).Sum();
            var cancelSubscriber = userSummaries.Where(i => i.user_source == 0).Select(i => i.cancel_user).Sum();
            sendStr += $"本周新增 {newSubscriber} 人，取消 {cancelSubscriber} 人";
            _workWeChat.Send(sendStr);
        }

        private static void SendReadAnalysis()
        {
            _weChat.GetReadData(out List<WeChat.ReadNum> readNums, out List<WeChat.ArticleReadNum> articleReadNums);
            string sendStr = "阅读统计\n";

            sendStr += "I. 本周每日阅读统计\n";
            readNums = readNums.OrderBy(i => i.ref_date).ToList();
            foreach (var readNum in readNums)
            {
                sendStr += readNum.ref_date.ToString("MM/dd") + " " + readNum.int_page_read_count + "\n";
            }

            sendStr += "II. 上周推送阅读总量\n";
            articleReadNums = articleReadNums.OrderBy(i => i.ref_date).ThenBy(i => i.title).ToList();
            List<DateTime> dateList = articleReadNums.GroupBy(i => i.ref_date).Select(i => i.Key).ToList();
            foreach (var date in dateList)
            {
                sendStr += date.ToString("MM/dd") + " :\n";
                List<string> articleList = articleReadNums.Where(i => i.ref_date == date).Select(i => i.title).ToList();
                foreach (var title in articleList)
                {
                    sendStr += title + " ";
                    List<WeChat.ArticleReadNum.DailyReadTotal> readTotal = articleReadNums.Where(i => i.title == title).Select(i => i.ReadTotals).First();
                    int readmax = readTotal.Select(i => i.int_page_read_count).Max();
                    sendStr += readmax + "\n";
                }
            }

            _workWeChat.Send(sendStr);
        }
    }
}