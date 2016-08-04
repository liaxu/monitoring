using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace GetErrorAndResponseMetrics
{
    public class Log
    {
        public string HistoryLog = string.Empty;
        public string LastFinished = string.Empty;
        public string ErrorLog = string.Empty;
        public void WriteLog(string s, bool sendMail, string title)
        {
            if (!Directory.Exists(HistoryLog))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryLog));
            }

            string ts = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(HistoryLog))
            {
                w.WriteLine(ts + " # " + s);
            }

            if (sendMail)
            {
                MailSender.GenerateAndSendMail(ts + " # " + s, title);
            }
        }

        public void WriteErrorLog(string s, bool sendMail, string title)
        {

            if (!Directory.Exists(ErrorLog))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorLog));
            }

            string ts = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(ErrorLog))
            {
                w.WriteLine(ts + " # " + s);
            }

            if (sendMail)
            {
                MailSender.GenerateAndSendMail(ts + " # " + s, title);
            }
        }

        public void WriteErrorLog(string s)
        {
            if (!Directory.Exists(ErrorLog))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorLog));
            }

            string ts = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(ErrorLog))
            {
                w.WriteLine(ts + " # " + s);
            }

        }

        public void WriteLog(string s)
        {
            if (!Directory.Exists(HistoryLog))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryLog));
            }

            string ts = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(HistoryLog))
            {
                w.WriteLine(ts + " # " + s);
            }
        }

        public void WriteDownloadLog(string s)
        {
            if (!Directory.Exists(LastFinished))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastFinished));
            }

            string ts = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(LastFinished))
            {
                w.WriteLine(ts + " # " + s);
            }
        }
    }
}
