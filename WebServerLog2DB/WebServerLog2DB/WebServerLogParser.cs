using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebServerLog2DB
{
    class WebServerlogParser
    {
        private static string[] ignoreExt = { ".css",".js",".png",".jpg",".woff",".gif",".ttf",".ico",".svg",".mp4",".mp3", ".eot"};
        public static List<Record> Parse(string content)
        {
            try
            {
                List<Record> records = new List<Record>();
                string pattern = "#.*";
                string replacement = "";
                Regex rgx = new Regex(pattern);
                content = rgx.Replace(content, replacement);
                string[] recordStrings = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var s in recordStrings)
                {
                    if (s.Trim() == String.Empty)
                        continue;
                    Record record = new Record();
                    string[] paramStrings = s.Split(' ');
                    if (paramStrings.Count() <= 12)
                    {
                        continue;
                    }

                    try
                    {
                        // we want date,time,ip,url,refurl,status,timetaken
                        bool shouldIgnore = false;
                        foreach (var ext in ignoreExt)
                        {
                            if (paramStrings[4].ToLower().IndexOf(ext) != -1)
                            {
                                shouldIgnore = true;
                                break;
                            }
                        }
                        if (shouldIgnore)
                        {
                            continue;
                        }


                        var date = paramStrings[0];
                        var time = paramStrings[1];
                        var ip = paramStrings[8];
                        var refurl = paramStrings[11];
                        var url = paramStrings[12] + paramStrings[4];
                        var status = paramStrings[13];
                        var timeTaken = paramStrings[18];

                        if (paramStrings[9].Contains("+AppInsights"))
                            continue;
                        record.url = url;
                        record.refurl = refurl;
                        record.status = Convert.ToInt32(status);
                        record.actionTime = Convert.ToInt32(timeTaken);
                        record.timeString = Convert.ToDateTime(date + ' ' + time).Ticks / 10000;
                        record.dayTime = Convert.ToDateTime(Convert.ToDateTime(date + ' ' + time).ToString("yyyy-MM-dd 00:00:00")).Ticks / 10000;
                        record.hourTime = Convert.ToDateTime(Convert.ToDateTime(date + ' ' + time).ToString("yyyy-MM-dd HH:00:00")).Ticks / 10000;
                        record.ip = ip;
                        if (ip.Contains('.'))
                        {
                            records.Add(record);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace + "\r\nOriginal Text: " + s);
                        throw;
                    }
                }
                return records;
            }
            catch (Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
        }
    }
}
