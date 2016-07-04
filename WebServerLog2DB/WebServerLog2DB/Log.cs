using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace WebServerLog2DB
{
    public class Log
    {
        public static string LogPath = string.Format(ConfigurationManager.AppSettings["LogPath"], DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HH"));
        public static void WriteLog(string s, bool sendMail, string title)
        {
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            }

            string ts = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(LogPath))
            {
                w.WriteLine(ts + " # " + s);
            }

            if (sendMail)
            {
                MailSender.GenerateAndSendMail(ts + " # " + s, title);
            }
        }

        public static void WriteLog(string s)
        {
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            }

            string ts = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(LogPath))
            {
                w.WriteLine(ts + " # " + s);
            }
        }
    }
}
