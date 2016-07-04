using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace AzureStorageCleanup
{
    public class Log
    {
        public string LogPath = string.Empty;
        public static string LogPathForParserBlobsAndInsertDB = string.Format(Program.ISGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], DateTime.Now.ToString("yyyyMMdd"), "LogPathForParserBlobsAndInsertDB");
        public void WriteLog(string s, bool sendMail, string title)
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

        public static void WriteLogForParserBlobsAndInsertDB(string s, bool sendMail, string title)
        {
            if (!Directory.Exists(LogPathForParserBlobsAndInsertDB))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPathForParserBlobsAndInsertDB));
            }

            string ts = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(LogPathForParserBlobsAndInsertDB))
            {
                w.WriteLine(ts + " # " + s);
            }

            if (sendMail)
            {
                MailSender.GenerateAndSendMail(ts + " # " + s, title);
            }
        }

        public static void WriteLogForParserBlobsAndInsertDB(string s)
        {
            if (!Directory.Exists(LogPathForParserBlobsAndInsertDB))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPathForParserBlobsAndInsertDB));
            }

            string ts = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

            using (StreamWriter w = File.AppendText(LogPathForParserBlobsAndInsertDB))
            {
                w.WriteLine(ts + " # " + s);
            }

        }

        public void WriteLog(string s)
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
