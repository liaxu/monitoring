using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Threading;
using System.Text;

namespace AILogInsertDBService
{
    class ParserAndInsertDB
    {
        string ParserApplicationInsightName(string filePath)
        {
            string applicationInsightName = Path.GetFileNameWithoutExtension(filePath);
            Regex r = new Regex(@"(.*)_[0-9a-f]{32}$", RegexOptions.IgnoreCase);
            Match m = r.Match(applicationInsightName);
            if (m.Success && m.Groups.Count == 2)
            {
                applicationInsightName = m.Groups[1].ToString();
            }
            return applicationInsightName.ToLower();
        }

        Incident InitialIncident(Availability availability, string applicationInsightName)
        {
            Incident incident = new Incident();
            incident.WebTestName = availability.testName;
            incident.Location = availability.runLocation;
            incident.AlertTime = availability.testTimestamp.ToUniversalTime().Ticks / 10000;
            incident.AlertMessage = availability.message;
            incident.ApplicationInsightName = applicationInsightName;
            return incident;
        }

        private bool generateMail(Dictionary<string, Tuple<bool, Incident>> flag)
        {
            #region GenrateMail
            List<Incident> sendIncidents = new List<Incident>();

            long now = DateTime.UtcNow.Ticks;
            foreach (var item in flag.Values)
            {
                if (item.Item2.WebTestName.ToLower().StartsWith("s0") || item.Item2.WebTestName.ToLower().StartsWith("s1"))
                {
                    if (new TimeSpan(now - item.Item2.AlertTime * 10000).TotalMinutes >= 15)
                    {
                        sendIncidents.Add(item.Item2);
                    }
                }
                else
                {
                    if (new TimeSpan(now - item.Item2.AlertTime * 10000).TotalHours >= 1)
                    {
                        sendIncidents.Add(item.Item2);
                    }
                }
            }

            if (sendIncidents.Count > 0)
            {
                string temp = @"<table class=MsoNormalTable border=0 cellspacing=0 cellpadding=0 width=1232
 style='width:924.0pt;margin-left:-1.15pt;border-collapse:collapse;mso-yfti-tbllook:
 1184;mso-padding-alt:0cm 5.4pt 0cm 5.4pt'>
 <tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;height:15.75pt'>";

                string[] fields = { "Id", "AlertTime(UTC)", "WebTestName", "ApplicationInsightName", "Location", "AlertMessage" };
                foreach (var field in fields)
                {
                    temp += string.Format(@"<th valign=top style='width:109.0pt;border:none;border-top:solid #F79646 1.0pt;
  background:#F79646;padding:0cm 5.4pt 0cm 5.4pt;height:15.75pt'>
  <p class=MsoNormal align=left style='text-align:left;mso-pagination:widow-orphan'><b><span
  lang=EN-US style='font-size:11.0pt;mso-ascii-font-family:Calibri;mso-fareast-font-family:
  SimSun;mso-hansi-font-family:Calibri;mso-bidi-font-family:SimSun;color:white;
  mso-font-kerning:0pt'>{0}<o:p></o:p></span></b></p>
  </th>", field);
                }

                temp += "<//tr>";

                string tdstr = @"<td valign=top style='width:350.0pt;border:solid #F79646 1.0pt;
  border-left:none;mso-border-top-alt:solid #F79646 1.0pt;mso-border-bottom-alt:
  solid #F79646 1.0pt;mso-border-right-alt:solid #F79646 .5pt;padding:0cm 5.4pt 0cm 5.4pt;
  height:60.75pt'>
  <p class=MsoNormal align=left style='text-align:left;mso-pagination:widow-orphan'><span
  lang=EN-US style='font-size:11.0pt;mso-ascii-font-family:Calibri;mso-fareast-font-family:
  SimSun;mso-hansi-font-family:Calibri;mso-bidi-font-family:SimSun;color:black;
  mso-font-kerning:0pt'>{0}<o:p></o:p></span></p>
  </td>";
                foreach (var item in sendIncidents)
                {
                    temp += "<tr style='mso-yfti-irow:0;mso-yfti-firstrow:yes;height:15.75pt'>";
                    temp += string.Format(@"<td valign=top style='width:103.0pt;border-top:solid #F79646 1.0pt;
  border-left:none;border-bottom:solid #F79646 1.0pt;border-right:none;
  padding:0cm 5.4pt 0cm 5.4pt;height:60.75pt'>
  <p class=MsoNormal align=left style='text-align:left;mso-pagination:widow-orphan'><span
  lang=EN-US style='font-size:11.0pt;mso-ascii-font-family:Calibri;mso-fareast-font-family:
  SimSun;mso-hansi-font-family:Calibri;mso-bidi-font-family:SimSun;color:black;
  mso-font-kerning:0pt'><a
  href=""{0}"">{1}</a><o:p></o:p></span></p>", "", item.Id);
                    temp += string.Format(tdstr, new DateTime(item.AlertTime * 10000).ToString());
                    temp += string.Format(tdstr, item.WebTestName);
                    temp += string.Format(tdstr, item.ApplicationInsightName);
                    temp += string.Format(tdstr, item.Location);
                    temp += string.Format(tdstr, item.AlertMessage);

                    temp += "<//tr>";
                }

                temp += "<//table>";
                if (sendIncidents.Count > 0)
                {
                    MailSender.GenerateAndSendMail(temp, "Dev: S0- S1 Incidents have not been fixed more than 15 minutes or S2- S3 Incidents have not been fixed more than 1 hour");
                    return true;
                }
            }

            return false;
            #endregion
        }

        public void ParserAILogAndInsertDB(object isGraphObj)
        {

            bool isGraph = (bool)isGraphObj;
            Log log = new Log();
            log.ErrorLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "InsertDBErrorLog", "error");
            log.HistoryLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DBLog", "log");
            log.LastFinished = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DBLogSpentTime", "log");

            try
            {
                SQLCommon sqlCommon = new SQLCommon();
                string AINames = isGraph ? ConfigurationManager.AppSettings["AINamesForGraph"] : ConfigurationManager.AppSettings["AINamesForDev"];
                string OutputFolder = isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"];
                string connStr = ConfigurationManager.AppSettings["ConnStr"];
                string incidentsTableName = isGraph ? "IncidentsForGraph" : "IncidentsForDev";
                bool sendmail = ConfigurationManager.AppSettings["SendMail"].ToLower() == "true" ? true : false;
                int sleepTime = Convert.ToInt32(ConfigurationManager.AppSettings["SleepTime"]);
                if (sqlCommon.conn.State == ConnectionState.Closed)
                {
                    sqlCommon.conn.Open();
                    Thread.Sleep(1);
                }

                #region Query DependServices.
                string cmdText = string.Format(@"SELECT [WebTestName],[ApplicationInsightName],[DependServiceId] FROM [dbo].[WebTest] WHERE [BelongsToGraph]={0}", isGraph ? "1" : "0");
                int result2;
                DataTable dt2 = sqlCommon.Query(cmdText, out result2);
                Dictionary<string, int> serviceCaseMapping = new Dictionary<string, int>();
                if (result2 >= 0)
                {
                    foreach (DataRow item in dt2.Rows)
                    {
                        string key = item["WebTestName"].ToString().ToLower() + "_" + item["ApplicationInsightName"].ToString().ToLower();

                        if (serviceCaseMapping.ContainsKey(key))
                        {
                            continue;
                        }
                        else
                        {
                            serviceCaseMapping.Add(key, Convert.ToInt32(item["DependServiceId"]));
                        }
                    }
                }
                #endregion

                Dictionary<string, Tuple<bool, Incident>> flag = new Dictionary<string, Tuple<bool, Incident>>();

                #region Query Incidents which HealthTime is null.

                cmdText = string.Format(@"SELECT [{0}].[Id], [{0}].[WebTestName], [Location], [AlertTime], [AlertMessage], [{0}].[ApplicationInsightName] FROM [dbo].[{0}], [dbo].[WebTest]
                                            WHERE [HealthTime] IS NULL AND
                                                [{0}].[WebTestName] = [WebTest].[WebTestName] AND 
                                                [{0}].[ApplicationInsightName] = [WebTest].[ApplicationInsightName] AND 
                                                [WebTest].IsDisable = 0 AND [WebTest].BelongsToGraph = {1}
                                            ORDER BY [AlertTime] DESC", incidentsTableName, isGraph ? "1" : "0");
                int result;
                DataTable dt = sqlCommon.Query(cmdText, out result);

                if (result >= 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        string key = item["WebTestName"].ToString() + '_' + item["ApplicationInsightName"].ToString() + '_' + item["Location"].ToString();
                        Incident incident = new Incident();
                        incident.Id = Convert.ToInt32(item["Id"]);
                        incident.WebTestName = item["WebTestName"].ToString();
                        incident.Location = item["Location"].ToString();
                        incident.AlertTime = long.Parse(item["AlertTime"].ToString());
                        incident.AlertMessage = item["AlertMessage"].ToString();
                        incident.ApplicationInsightName = item["ApplicationInsightName"].ToString();
                        if (flag.ContainsKey(key))
                        {
                            continue;
                        }
                        else
                        {
                            flag.Add(key, new Tuple<bool, Incident>(true, incident));

                        }
                    }

                }
                #endregion
                List<string> applicationInsight = AINames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                DateTime startWork = DateTime.UtcNow;
                DateTime sendMailTime = DateTime.MinValue;               
                while (true)
                {
                    DateTime start = DateTime.UtcNow;
                    if ((start - sendMailTime).TotalMinutes > 10)
                    {
                        sendmail = true;
                    }

                    if (sendmail)
                    {
                        bool hasSentMail = generateMail(flag);
                        if (hasSentMail)
                        {
                            sendMailTime = DateTime.UtcNow;
                            sendmail = false;
                        }
                    }

                    foreach (var ai in applicationInsight)
                    {
                        string applicationInsightName = ParserApplicationInsightName(ai);
                        OutputFolder = Path.Combine(isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"], applicationInsightName);

                        DirectoryInfo di = new DirectoryInfo(OutputFolder);
                        List<FileInfo> files = di.GetFiles("*.blob", SearchOption.AllDirectories).OrderBy(x => x.CreationTimeUtc).ToList();

                        int count = 0;
                        int passCount = 0;
                        int failedCount = 0;
                        foreach (var filePath in files)
                        {
                            try
                            {
                                #region Get availabilityList from each file.
                                List<Availability> availabilityList = new List<Availability>();
                                string jsonText = File.ReadAllText(filePath.FullName);

                                try
                                {
                                    Regex r = new Regex(@"\{""availability"":\[(\{.*?\})\]", RegexOptions.IgnoreCase);
                                    Match m = r.Match(jsonText);
                                    while (m.Success)
                                    {
                                        try
                                        {
                                            Availability availability = JsonConvert.DeserializeObject<Availability>(m.Groups[1].ToString(), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore, });
                                            availabilityList.Add(availability);
                                        }
                                        catch
                                        {

                                        }
                                        m = m.NextMatch();
                                    }
                                }
                                catch(Exception e)
                                {
                                    throw new Exception(filePath.FullName+Environment.NewLine +
                                        e.Message+Environment.NewLine+e.StackTrace);
                                }

                                availabilityList = availabilityList.OrderBy(x => x.testTimestamp).ToList();
                                #endregion

                                foreach (var availability in availabilityList)
                                {
                                    string key = availability.testName + '_' + applicationInsightName + '_' + availability.runLocation;
                                    #region Insert Incident table
                                    if (availability.result == "Pass")
                                    {
                                        passCount++;
                                        if (flag.ContainsKey(key))
                                        {
                                            if (flag[key].Item1)
                                            {
                                                #region Update Failed Case HealthTime and Duration
                                                Incident incident = flag[key].Item2;
                                                if (incident.Id != 0)
                                                {
                                                    long duration = availability.testTimestamp.ToUniversalTime().Ticks / 10000 - incident.AlertTime;
                                                    if (duration > 0)
                                                    {
                                                        string excuteCmdText = string.Format(@"UPDATE [dbo].[{3}]
                                                              SET [HealthTime] = {0}
                                                              ,[Duration] = {1}                                                                              
                                                              WHERE [Id] = {2}", availability.testTimestamp.ToUniversalTime().Ticks / 10000, duration, incident.Id, incidentsTableName);


                                                        if (sqlCommon.conn.State == ConnectionState.Closed)
                                                        {
                                                            sqlCommon.conn.Open();
                                                            Thread.Sleep(1);
                                                        }
                                                        int excuteResult = sqlCommon.ExecuteQuery(excuteCmdText);

                                                        if (excuteResult != -1)
                                                        {
                                                            flag.Remove(key);
                                                            DateTime ddd = DateTime.UtcNow;
                                                            log.WriteLog(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                                                            ddd.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            availability.testTimestamp,
                                                            (ddd - availability.testTimestamp).ToString(),
                                                            availability.testName,
                                                            applicationInsightName,
                                                            availability.runLocation,
                                                            availability.result,
                                                            filePath.CreationTimeUtc,
                                                            (filePath.CreationTimeUtc - availability.testTimestamp).ToString()));
                                                        }
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                    else
                                    {
                                        #region Insert Failed Case
                                        bool insertDB = false;
                                        if (flag.ContainsKey(key))
                                        {
                                            if (!flag[key].Item1)
                                            {
                                                insertDB = true;
                                            }
                                        }
                                        else
                                        {
                                            insertDB = true;
                                        }

                                        if (insertDB)
                                        {
                                            failedCount++;
                                            Incident incident = InitialIncident(availability, applicationInsightName);
                                            int dependServiceId = -1;
                                            if (serviceCaseMapping.ContainsKey(availability.testName.ToLower() + "_" + incident.ApplicationInsightName.ToLower()))
                                            {
                                                dependServiceId = serviceCaseMapping[availability.testName.ToLower() + "_" + incident.ApplicationInsightName.ToLower()];
                                            }

                                            if (dependServiceId > 0)
                                            {
                                                string excuteCmdText
                                                    = string.Format(@"INSERT INTO [dbo].[{5}]([DependServiceId],[WebTestName],[ApplicationInsightName],[AlertMessage],[AlertTime],[Location]) OUTPUT INSERTED.Id VALUES({0}, '{1}','{2}',@AlertMessage,{3},'{4}')",
                                                                dependServiceId,
                                                                incident.WebTestName,
                                                                incident.ApplicationInsightName,
                                                                incident.AlertTime,
                                                                incident.Location,
                                                                incidentsTableName);

                                                int incidentId;

                                                if (sqlCommon.conn.State == ConnectionState.Closed)
                                                {
                                                    sqlCommon.conn.Open();
                                                    Thread.Sleep(1);
                                                }
                                                bool excuteResult = sqlCommon.ExecuteQuery(excuteCmdText, out incidentId, new SqlParameter("@AlertMessage", incident.AlertMessage));
                                                if (excuteResult)
                                                {
                                                    incident.Id = incidentId;
                                                    flag[key] = new Tuple<bool, Incident>(true, incident);

                                                    DateTime ddd = DateTime.UtcNow;
                                                    log.WriteLog(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                                                            ddd.ToString("yyyy-MM-dd HH:mm:ss"),
                                                            availability.testTimestamp,
                                                            (ddd - availability.testTimestamp).ToString(),
                                                            availability.testName,
                                                            applicationInsightName,
                                                            availability.runLocation,
                                                            availability.result,
                                                            filePath.CreationTimeUtc,
                                                            (filePath.CreationTimeUtc - availability.testTimestamp).ToString()));
                                                }
                                            }

                                        }

                                        #endregion
                                    }
                                    count++;
                                    #endregion

                                }
                                File.Delete(filePath.FullName);
                            }
                            catch(Exception e)
                            {
                                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                                throw;
                            }
                            finally
                            {

                            }
                        }
                    }

                    if (sqlCommon.conn.State != ConnectionState.Closed)
                    {
                        sqlCommon.conn.Close();
                    }
                    DateTime end2 = DateTime.UtcNow;
                    TimeSpan ts2 = new TimeSpan(end2.Ticks - start.Ticks);
                    Console.WriteLine("spent time:{0}", ts2.ToString());
                    log.WriteDownloadLog(ts2.ToString());
                    Thread.Sleep(sleepTime);      
                }
            }
            catch (Exception e)
            {
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, isGraph ? "Graph AILogInsertDBService Failed" : "Dev AILogInsertDBService Failed");
                throw;
            }
            finally
            {

            }
        }
               
    }
}
