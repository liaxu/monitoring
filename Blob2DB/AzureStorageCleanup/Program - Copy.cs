using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
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

namespace AzureStorageCleanup
{
    class Program
    {
        static string AINames = ConfigurationManager.AppSettings["AINames"];
        static string StorageName = ConfigurationManager.AppSettings["StorageName"];
        static string StorageKey = ConfigurationManager.AppSettings["StorageKey"];
        static string Container = ConfigurationManager.AppSettings["Container"];

        static string ExportDate = ConfigurationManager.AppSettings["ExportDate"];
        static string EmailTitle = "Download or Delete Azure Blob Error";

        static string OutputFolder = ConfigurationManager.AppSettings["OutputFolder"];
        static string connStr = ConfigurationManager.AppSettings["ConnStr"];
        static List<string> reponseTimeInsightNames = ConfigurationManager.AppSettings["ReponseTimeInsightName"].ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        static void Main(string[] args)
        {
            List<string> applicationInsight = AINames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<Thread> threads = new List<Thread>();
            foreach (var ai in applicationInsight)
            {
                Thread t = new Thread(new ParameterizedThreadStart(GetThenDeleteBlobs));
                t.Name = ai;
                t.Start(ai);
                threads.Add(t);
            }
            foreach (var thread in threads)
            {
                thread.Join();
            }
            ParserBlobsAndInsertDB();
        }

        static string ParserApplicationInsightName(string filePath)
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

        static Incident InitialIncident(Availability availability, string applicationInsightName)
        {
            Incident incident = new Incident();
            incident.WebTestName = availability.testName;
            incident.Location = availability.runLocation;
            incident.AlertTime = availability.testTimestamp.ToUniversalTime().Ticks/10000;
            incident.AlertMessage = availability.message;
            incident.ApplicationInsightName = applicationInsightName;
            return incident;
        }

        static DataTable MakeTable()
        {
            DataTable dt = new DataTable("NewResponseTable");

            // Add three column objects to the table. 
            DataColumn id = new DataColumn();
            id.DataType = System.Type.GetType("System.Int32");
            id.ColumnName = "Id";
            id.AutoIncrement = true;
            dt.Columns.Add(id);

            DataColumn webTestName = new DataColumn();
            webTestName.DataType = System.Type.GetType("System.String");
            webTestName.ColumnName = "WebTestName";
            dt.Columns.Add(webTestName);

            DataColumn applicationName = new DataColumn();
            applicationName.DataType = System.Type.GetType("System.String");
            applicationName.ColumnName = "ApplicationName";
            dt.Columns.Add(applicationName);
     
            DataColumn responseTime = new DataColumn();
            responseTime.DataType = System.Type.GetType("System.Int64");
            responseTime.ColumnName = "ResponseTime";
            dt.Columns.Add(responseTime);

            DataColumn location = new DataColumn();
            location.DataType = System.Type.GetType("System.String");
            location.ColumnName = "Location";
            dt.Columns.Add(location);

            DataColumn runTime = new DataColumn();
            runTime.DataType = System.Type.GetType("System.Int64");
            runTime.ColumnName = "RunTime";
            dt.Columns.Add(runTime);

            DataColumn belongsToGraph = new DataColumn();
            belongsToGraph.DataType = System.Type.GetType("System.Boolean");
            belongsToGraph.ColumnName = "BelongsToGraph";
            dt.Columns.Add(belongsToGraph);

            DataColumn isPassed = new DataColumn();
            isPassed.DataType = System.Type.GetType("System.Boolean");
            isPassed.ColumnName = "IsPassed";
            dt.Columns.Add(isPassed);  
            
            // Create an array for DataColumn objects.
            DataColumn[] keys = new DataColumn[1];
            keys[0] = id;
            dt.PrimaryKey = keys;
            
            return dt;
        }

        public static void ParserBlobsAndInsertDB()
        {
            try
            {
                string date = string.IsNullOrEmpty(ExportDate)?DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"):ExportDate;
                int length = DateTime.UtcNow.Subtract(Convert.ToDateTime(date)).Days;
                for (int i = 0; i < length; i++)
                {
                    OutputFolder = Path.Combine(ConfigurationManager.AppSettings["OutputFolder"], date);//ConfigurationManager.AppSettings["OutputFolder"].Replace("2016-02-03", date);

                    string[] files = Directory.GetFiles(OutputFolder);

                    Dictionary<string, Tuple<bool, Incident>> flag = new Dictionary<string, Tuple<bool, Incident>>();

                    Dictionary<string, int> serviceCaseMapping = new Dictionary<string, int>();
                    #region Query Incidents which HealthTime is null.
                    if (SQLCommon.conn.State == ConnectionState.Closed)
                    {
                        SQLCommon.conn.Open();
                    }
                    string cmdText = string.Format(@"SELECT [Incidents].[Id], [Incidents].[WebTestName], [Location], [AlertTime] FROM [dbo].[Incidents], [dbo].[WebTest]
                                            WHERE [HealthTime] IS NULL AND [Incidents].[WebTestName] = [WebTest].[WebTestName] AND [WebTest].IsDisable = 0
                                            ORDER BY [AlertTime] DESC");
                    int result;
                    DataTable dt = SQLCommon.Query(cmdText, out result);

                    if (result >= 0)
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            string key = item["WebTestName"].ToString() + '_' + item["Location"].ToString();
                            Incident incident = new Incident();
                            incident.Id = Convert.ToInt32(item["Id"]);
                            incident.WebTestName = item["WebTestName"].ToString();
                            incident.Location = item["Location"].ToString();
                            incident.AlertTime = long.Parse(item["AlertTime"].ToString());

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

                    #region Query DependServices.
                    cmdText = string.Format(@"SELECT [WebTestName],[DependServiceId] FROM [dbo].[WebTest]");
                    dt = SQLCommon.Query(cmdText, out result);

                    if (result >= 0)
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            string key = item["WebTestName"].ToString().ToLower();

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
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Start to parser {0}'s files",date);               
                    foreach (var filePath in files)
                    {
                        //if (filePath.Contains("graph-prod") || filePath.Contains("Graph-Prod"))
                        {
                            #region Get availabilityList from each file.
                            List<Availability> availabilityList = new List<Availability>();
                            string jsonText = File.ReadAllText(filePath);
                            //List<dynamic> incidents = JsonConvert.DeserializeObject<List<dynamic>>(jsonText);
                            //foreach (dynamic incident in incidents)
                            //{
                            //    List<Availability> inci = JsonConvert.DeserializeObject<List<Availability>>(incident["availability"].ToString(), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                            //    availabilityList.AddRange(inci);
                            //}

                            Regex r = new Regex(@"\{""availability"":\[(\{.*\})\]", RegexOptions.IgnoreCase);
                            Match m = r.Match(jsonText);
                            while (m.Success)
                            {
                                Availability availability = JsonConvert.DeserializeObject<Availability>(m.Groups[1].ToString(), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore, });
                                availabilityList.Add(availability);
                                m = m.NextMatch();
                            }

                            availabilityList = availabilityList.OrderBy(x => x.testTimestamp).ToList(); 
                            #endregion
                            string applicationInsightName = ParserApplicationInsightName(filePath);
                            DateTime start = DateTime.Now;
                            Console.WriteLine(applicationInsightName + " start time: " + start.ToString());
                            int count = 0;
                            int passCount = 0;
                            int failedCount = 0;
                            DataTable tempTable = MakeTable();
                            foreach (var availability in availabilityList)
                            {
                                string key = availability.testName + '_' + availability.runLocation;
                                if (reponseTimeInsightNames.Contains(applicationInsightName))
                                {
                                    #region Insert ResponseNew table
                                    DataRow row = tempTable.NewRow();
                                    row["BelongsToGraph"] = applicationInsightName.ToLower().Contains("Graph".ToLower()) ? 1 : 0;
                                    row["WebTestName"] = availability.testName;
                                    row["ApplicationName"] = applicationInsightName;
                                    row["ResponseTime"] = availability.durationMetric.value / 10000;
                                    row["Location"] = availability.runLocation;
                                    row["RunTime"] = availability.testTimestamp.ToUniversalTime().Ticks / 10000;
                                    row["IsPassed"] = availability.result.ToLower().Contains("Pass".ToLower()) ? 1 : 0;
                                    tempTable.Rows.Add(row);
                                    tempTable.AcceptChanges();
                                    count++;
                                    #endregion
                                }
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
                                                    string excuteCmdText = string.Format(@"UPDATE [dbo].[Incidents]
                                                              SET [HealthTime] = {0}
                                                              ,[Duration] = {1}                                                                              
                                                              WHERE [Id] = {2}", availability.testTimestamp.ToUniversalTime().Ticks / 10000, duration, incident.Id);

                                                    bool excuteResult = SQLCommon.ExecuteQuery(excuteCmdText);

                                                    if (excuteResult)
                                                    {
                                                        flag.Remove(key);
                                                    }
                                                    else
                                                    {
                                                        Log.WriteLogForParserBlobsAndInsertDB(string.Format("Update command '{0}' failed in availabilityList['{1}']", excuteCmdText, count));
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
                                        if (serviceCaseMapping.ContainsKey(availability.testName.ToLower()))
                                        {
                                            dependServiceId = serviceCaseMapping[availability.testName.ToLower()];
                                        }

                                        string excuteCmdText
                                            = string.Format(@"INSERT INTO [dbo].[Incidents]([DependServiceId],[WebTestName],[ApplicationInsightName],[AlertMessage],[AlertTime],[Location]) OUTPUT INSERTED.Id VALUES({0}, '{1}','{2}',@AlertMessage,{3},'{4}')",
                                                        dependServiceId,
                                                        incident.WebTestName,
                                                        incident.ApplicationInsightName,
                                                        incident.AlertTime,
                                                        incident.Location);

                                        if (dependServiceId < 0)
                                        {
                                            excuteCmdText = excuteCmdText.Replace(@"[DependServiceId],", "").Replace(@"VALUES(-1, ", @"VALUES (");
                                        }

                                        int incidentId;
                                        bool excuteResult = SQLCommon.ExecuteQuery(excuteCmdText, out incidentId, new SqlParameter("@AlertMessage", incident.AlertMessage));
                                        if (excuteResult)
                                        {
                                            incident.Id = incidentId;
                                            flag[key] = new Tuple<bool, Incident>(true, incident);
                                        }
                                        else
                                        {
                                            Log.WriteLogForParserBlobsAndInsertDB(string.Format("Insert command '{0}' failed in availabilityList['{1}']", excuteCmdText, count));
                                        }
                                    }

                                    #endregion
                                }
                                count++;
                                #endregion

                                if (tempTable.Rows.Count == 1000)
                                {
                                    bool executeSqlBulkCopyResult = SQLCommon.ExecuteSqlBulkCopy(tempTable, "dbo.[ResponseNew]");
                                    if (executeSqlBulkCopyResult)
                                    {
                                        tempTable.Rows.Clear();
                                    }
                                }
                            }

                            if (tempTable.Rows.Count > 0)
                            {
                                bool executeSqlBulkCopyResult2 = SQLCommon.ExecuteSqlBulkCopy(tempTable, "dbo.[ResponseNew]");
                                if (executeSqlBulkCopyResult2)
                                {
                                    tempTable.Rows.Clear();
                                }
                            }

                            DateTime end = DateTime.Now;
                            TimeSpan ts = new TimeSpan(end.Ticks - start.Ticks);
                            Console.WriteLine(string.Format(applicationInsightName + "'s spent time:{0}, Count:{1}, Passed Count:{2}, Failed Count:{3}", ts.ToString(), count, passCount, failedCount));
                       }
                    }
                    date = Convert.ToDateTime(date).AddDays(1).ToString("yyyy-MM-dd");
               }
                SQLCommon.conn.Close();
            }
            catch (Exception e)
            {
                Log.WriteLogForParserBlobsAndInsertDB(e.Message + "\r\nStackTrace: " + e.StackTrace, true, "ParserBlobsAndInsertDB Error Message");
            }
            finally
            {

            }
        }

        static void GetThenDeleteBlobs(object applicationInsight)
        {
            string ai = applicationInsight as string;
            DateTime start = DateTime.Now;
            Log log = new Log();
            log.LogPath = string.Format(ConfigurationManager.AppSettings["LogPath"], DateTime.Now.ToString("yyyyMMdd"), ai);

            try
            {
                log.WriteLog("start");
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);


                string date = string.IsNullOrEmpty(ExportDate) ? DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") : ExportDate;
                int length = DateTime.UtcNow.Subtract(Convert.ToDateTime(date)).Days;
                for (int i = 0; i < length; i++)
                {
                    string outputFolder = Path.Combine(ConfigurationManager.AppSettings["OutputFolder"], date);
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }

                    //foreach (var ai in applicationInsight)
                    //{
                    log.WriteLog("start download " + ai);
                    DateTime start2 = DateTime.Now;
                    Console.WriteLine(start2.ToString());

                    var blobs = container.ListBlobs(ai + @"/Availability/" + date, useFlatBlobListing: true).ToList();
                    log.WriteLog("ThreadName: " + Thread.CurrentThread.Name + ", ai: " + ai + ",outputFolder: " + outputFolder +",:blob.count: "+blobs.Count);
                    if (blobs.Count > 0)
                    {
                        //var blobs = container.ListBlobs(@"o365 api getting started experience - o365 api code samples 02_ef0dc6cf0afa4eb29438cd26e8e2e91f/Availability/" + date, useFlatBlobListing: true).ToList();
                        string filePath = string.Format(@"{0}\{1}.blob", outputFolder, ai);

                        Log.WriteLogForParserBlobsAndInsertDB("ThreadName: " + Thread.CurrentThread.Name + ", filePath: " + filePath + ",outputFolder: " + outputFolder);
                        //File.AppendAllText(filePath, "[");
                        foreach (ICloudBlob blob in blobs)
                        {
                            bool isSuccessDownload = true;
                            try
                            {
                                blob.DownloadToFile(filePath, System.IO.FileMode.Append);
                            }
                            catch (Exception e)
                            {
                                log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, "Download or Delete Azure Blob Error Message");
                                isSuccessDownload = false;
                            }
                            finally
                            {

                            }
                            //File.AppendAllText(filePath, ",");
                            //if (isSuccessDownload)
                            //{
                            //    try
                            //    {
                            //        //blob.DeleteIfExists();
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        Log(ex.Message, true);
                            //    }
                            //    finally
                            //    {

                            //    }
                            //}
                        }
                    }
                    else
                    {
                        return;
                    }
                    //File.AppendAllText(filePath, "]");
                    log.WriteLog("Finish download " + ai);
                    DateTime end2 = DateTime.Now;
                    TimeSpan s2 = end2 - start2;
                    log.WriteLog("Spent time to download " + ai + " is " + s2.ToString());
                    //}
                    DateTime end = DateTime.Now;
                    TimeSpan s = end - start;
                    log.WriteLog("Spent time to download all files is " + s.ToString());

                    date = Convert.ToDateTime(date).AddDays(1).ToString("yyyy-MM-dd");
                }

            }
            catch (Exception e)
            {
                log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, "Download or Delete Azure Blob Error Message");
            }
            finally
            {

            }
        }
    }
}
