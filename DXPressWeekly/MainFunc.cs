﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

[assembly: AssemblyVersion("1.1.*")]

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

            _workWeChat.Send($"大夏通讯社一周统计\n{DateTime.Now.AddDays(-7).ToShortDateString()} ~ {DateTime.Now.AddDays(-1).ToShortDateString()}{(IsDebug ? "\nDEBUG MODE":"")}");
            log.Info("Head message successfully.");

            SendApprovalData();
            SendUserAnalysis();
            SendReadAnalysis();
        }

        public static void SendApprovalData()
        {
            List<WorkWeChat.ApprovalData> list = _workWeChat.GetApprovalData();
            if (list.Count == 0)
            {
                return;
            }
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
            var newSubscriber = userSummaries.Select(i => i.new_user).Sum();
            var cancelSubscriber = userSummaries.Select(i => i.cancel_user).Sum();
            sendStr += $"本周新增 {newSubscriber} 人，取消 {cancelSubscriber} 人";
            _workWeChat.Send(sendStr);
        }

        private static void SendReadAnalysis()
        {
            // _weChat.GetReadData(out List<WeChat.ReadNum> readNums, out List<WeChat.ArticleReadNum> articleReadNums);
            string sendStr = "阅读统计";
            List<WeChat.ReadNum> readNums = _weChat.GetUserRead(-7, -1);
            List<WeChat.ArticleReadNum> thisWeekArticleReadNums = _weChat.GetArticleRead(-7, -1);
            sendStr += "\nI. 本周每日阅读统计";
            readNums = readNums.OrderBy(i => i.ref_date).ToList();
            foreach (var readNum in readNums)
            {
                sendStr += "\n" + readNum.ref_date.ToString("MM/dd") + " " + readNum.int_page_read_count;
                var dailyPublished = thisWeekArticleReadNums.Where(a => a.ref_date.Date == readNum.ref_date.Date)
                    .Select(a => a.title).ToArray();
                foreach (var articleName in dailyPublished)
                {
                    sendStr += "\n- " + articleName;
                }
            }

            sendStr += "\n\nII. 上周推送七日阅读总量";
            List<WeChat.ArticleReadNum> lastWeekArticleReadNums = _weChat.GetArticleRead(-14, -8);
            lastWeekArticleReadNums = lastWeekArticleReadNums.OrderBy(i => i.ref_date).ThenBy(i => i.title).ToList();
            List<DateTime> dateList = lastWeekArticleReadNums.GroupBy(i => i.ref_date).Select(i => i.Key).ToList();
            foreach (var date in dateList)
            {
                sendStr += "\n" + date.ToString("MM/dd") + " :";
                List<string> articleList = lastWeekArticleReadNums.Where(i => i.ref_date == date).Select(i => i.title).ToList();
                foreach (var title in articleList)
                {
                    sendStr += "\n" + title + " ";
                    List<WeChat.ArticleReadNum.DailyReadTotal> readTotal = lastWeekArticleReadNums.Where(i => i.title == title).Select(i => i.ReadTotals).First();
                    int readmax = readTotal.Select(i => i.int_page_read_count).Max();
                    sendStr += readmax;
                }
            }

            _workWeChat.Send(sendStr);
        }
    }
}