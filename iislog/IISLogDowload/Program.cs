using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISLogDowload.Model;
using Extractor.Extract;
using System.IO;
using System.Data.Sql;
using System.Data;

namespace IISLogDowload
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = new List<string>();

            var marker = FileMarkerManager.Load(123);

            FileTaker taker = new FileTaker(@"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})",

                new AzureBlobStorageFileGetter(),

                new FileFilterFactory().GetFileFilterByName(IncreamentationType.InFileIncreamentation),

                marker,

                new DateTime(2016, 7, 21));

            var errors = taker.ExtractOnce(Test, "", SearchOption.AllDirectories, ".log");
        }

        

        public static int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = (new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }

        static bool Test(IEnumerable<string> input)
        {
            var tb = new DataTable();
            tb.Columns.Add("Id", typeof(int));
            tb.Columns.Add("TimeStamp", typeof(int));
            tb.Columns.Add("URI", typeof(string));
            tb.Columns.Add("HOST", typeof(string));
            tb.Columns.Add("Referer", typeof(string));
            tb.Columns.Add("IP", typeof(string));
            tb.Columns.Add("ai_session", typeof(string));
            tb.Columns.Add("ai_user", typeof(string));
            tb.Columns.Add("status", typeof(string));
            tb.Columns.Add("time_taken", typeof(int));

            foreach (var item in input)
            {
                var sp = item.Split(' ');
                if (sp.Length != 19)
                {
                    continue;
                }

                var user_session = GetSession_Cookie(sp[10]);

                var row = tb.NewRow();
                row["TimeStamp"] = ConvertDateTimeInt(Convert.ToDateTime(sp[0] + " " + sp[1]));
                row["URI"] = sp[4].Length > 1000 ? sp[4].Substring(0, 1000) : sp[4];
                if (!string.IsNullOrEmpty(sp[18]))
                {
                    row["time_taken"] = int.Parse(sp[18]);
                }
                row["status"] = sp[13];
                row["Referer"] = sp[11].Length > 1000 ? sp[11].Substring(0, 1000) : sp[11];
                row["IP"] = sp[8];
                row["HOST"] = sp[12];
                row["ai_user"] = user_session[0];
                row["ai_session"] = user_session[1];
                if (sp[12].Contains("mirror") || sp[12].Contains("lastgood"))
                {
                    continue;
                }
                tb.Rows.Add(row);
            }
            SQLCommon common = new SQLCommon();
            return common.ExecuteSqlBulkCopy(tb, "t_iis_log_dev_test");
        }

        static string[] GetSession_Cookie(string cs_user_cookie)
        {
            var ai_user = "";
            var ai_session = "";
            if (cs_user_cookie.Contains("ai_session=") || cs_user_cookie.Contains("ai_user="))
            {
                foreach (var item in cs_user_cookie.Split(';'))
                {
                    if (item.Contains("ai_user="))
                    {
                        ai_user = item.Split('=')[1];
                    }

                    else if (item.Contains("ai_session="))
                    {
                        ai_session = item.Split('=')[1];
                    }
                }
            }
            return new string[] { ai_user, ai_session };
        }
    }
}
