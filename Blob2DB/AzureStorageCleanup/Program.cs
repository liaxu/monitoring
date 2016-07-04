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
        public static bool ISGraph = (ConfigurationManager.AppSettings["IsGraph"] == "1");

        static string AINames = ISGraph ? ConfigurationManager.AppSettings["AINamesForGraph"] : ConfigurationManager.AppSettings["AINamesForDev"];
        static string StorageName = ConfigurationManager.AppSettings["StorageName"];
        static string StorageKey = ConfigurationManager.AppSettings["StorageKey"];
        static string Container = ConfigurationManager.AppSettings["Container"];
        static string OutputFolder = ISGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"];
        static string connStr = ConfigurationManager.AppSettings["ConnStr"];
        static List<string> reponseTimeInsightNames = ISGraph ? ConfigurationManager.AppSettings["ReponseTimeInsightNameForGraph"].ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList() : ConfigurationManager.AppSettings["ReponseTimeInsightNameForDev"].ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        static string ExportStartDate = ConfigurationManager.AppSettings["ExportStartDate"];
        static string ExportEndDate = ConfigurationManager.AppSettings["ExportEndDate"];
        static DateTime currentStartTime = Convert.ToDateTime(DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:00:00"));
        static DateTime currentEndTime = Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd HH:00:00"));
        static DateTime ExportStartDateTime = string.IsNullOrEmpty(ExportStartDate) ? currentStartTime : Convert.ToDateTime(Convert.ToDateTime(ExportStartDate).ToString("yyyy-MM-dd HH:00:00"));
        static DateTime ExportEndDateTime = string.IsNullOrEmpty(ExportEndDate) ? currentEndTime : Convert.ToDateTime(Convert.ToDateTime(ExportEndDate).ToString("yyyy-MM-dd HH:00:00"));


        static string incidentsTableName = ISGraph ? "IncidentsForGraph" : "IncidentsForDev";
        static string responseTableName = ISGraph ? "ResponseForGraph" : "ResponseForDev";
        static string DependServicesFailurePerHourTableName = ISGraph ? "DependServicesFailurePerHourForGraph" : "DependServicesFailurePerHourForDev";
        static string ResponsePerHourTableName = ISGraph ? "ResponsePerHourForGraph" : "ResponsePerHourForDev";
        static void Main(string[] args)
        {
            try
            {
                DateTime start = DateTime.Now;
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

                DateTime end = DateTime.Now;
                TimeSpan s = end - start;
                Log.WriteLogForParserBlobsAndInsertDB("Finish all download Spent time: " + s.ToString());

                ParserBlobsAndInsertDB();
                end = DateTime.Now;
                s = end - start;
                Log.WriteLogForParserBlobsAndInsertDB("Finish all insert Spent time: " + s.ToString());

                GeneratePerHourTable();
                if (SQLCommon.conn.State != ConnectionState.Closed)
                {
                    SQLCommon.conn.Close();
                }
            }
            catch(Exception e)
            {
                Log.WriteLogForParserBlobsAndInsertDB(e.Message, true,
                    string.Format("{0}-{1}: Download and Insert Response and Incidents tables Failed.", ExportStartDateTime.ToString("yyyy-MM-dd HH"), ExportEndDateTime.ToString("yyyy-MM-dd HH")));
            }
            finally
            {

            }
        }

        static void GeneratePerHourTable()
        {
            if (SQLCommon.conn.State == ConnectionState.Closed)
            {
                SQLCommon.conn.Open();
            }

            string cmdText = string.Format("DELETE FROM dbo.{0} WHERE [DateTime]>={1} and [DateTime]<{2}", ResponsePerHourTableName, ExportStartDateTime.AddHours(-1).Ticks / 10000, ExportEndDateTime.Ticks / 10000);
            int resultCount = SQLCommon.ExecuteQuery(cmdText);
            Log.WriteLogForParserBlobsAndInsertDB("Delete " + ResponsePerHourTableName + ", resultCount: " + resultCount);

            #region SQL for insert records per hour
            cmdText = string.Format(@"
                    INSERT INTO [{0}]
                    SELECT CONVERT(FLOAT,[DateTime])*1000*60*60*24+59926608000000 AS 'DateTime',
	                       AVG([ResponseTime(s)]) AS [ResponseTime(s)],
	                       SUM(case [IsPassed] when 0 then 1 else 0 END) as [FailedCount],
	                       SUM(case [IsPassed] when 1 then 1 else 0 END) as [PassedCount],
	                       [WebTestName],[ApplicationName], [Location]
                    FROM
                    (
                        SELECT
		                    CAST(SUBSTRING(CONVERT(nvarchar(100),CAST(([RunTime]-59926608000000)/1000/60/60/24 AS DATETIME),121),1,13)+':00:00' AS DateTime) AS 'DateTime',
                            Cast([ResponseTime] as float)/1000 as 'ResponseTime(s)', *
	                        FROM [dbo].[{1}]    
                            WHERE [RunTime]>={2} AND [RunTime]<{3}
                    ) AS Temp  
                    GROUP BY [DateTime], [WebTestName], [ApplicationName], [Location]
                    ORDER BY [DateTime]", ResponsePerHourTableName, responseTableName, ExportStartDateTime.AddHours(-1).Ticks / 10000, ExportEndDateTime.Ticks / 10000);
            #endregion
            resultCount = SQLCommon.ExecuteQuery(cmdText);
            Log.WriteLogForParserBlobsAndInsertDB("Insert " + ResponsePerHourTableName + ", resultCount: " + resultCount);

            #region Insert DependServicesFailurePerHourTableName
//            cmdText = string.Format(@"
//                DECLARE @counter int
//                DECLARE @toTime datetime
//                DECLARE @alertNumber int
//                DECLARE @fromDate datetime
//                SET @counter = 0
//                set @toTime = CONVERT(datetime, '{1}')
//                set @fromDate = CONVERT(datetime, '{0}')
//                WHILE(convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000) < convert(decimal,convert(float,convert(datetime,@toTime))*1000*60*60*24+59926608000000))
//                BEGIN
//                    SET @counter = 0
//                    WHILE(@counter <= (select MAX(Id) FROM DependServices))
//                    BEGIN
//                        DECLARE @colVar INT
//		                DECLARE @alertTime BIGINT
//                        DECLARE @name NVARCHAR(50)
//                        select @colVar=DependServices.Id,@name=DependServices.DependServiceName from DependServices where ID=@counter
//                        SET @counter = @counter + 1
//                        if @colVar IS NOT NULL
//                        Begin
//                            select @alertNumber=count(*), @alertTime=min(t1.AlertTime) from (select DependServiceId,AlertTime,HealthTime from {2} where {2}.DependServiceId = @colVar and {2}.AlertTime > convert(decimal,convert(float,convert(datetime,DATEADD(day,-7,@fromDate)))*1000*60*60*24+59926608000000) and AlertTime < convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000)) as t1 where t1.AlertTime < convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000) and (t1.HealthTime > convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000) or t1.HealthTime = null) or (t1.AlertTime > convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000) and t1.HealthTime <= convert(decimal,convert(float,convert(datetime,DATEADD(hour,1,@fromDate)))*1000*60*60*24+59926608000000))
//                            if @alertNumber > 0
//                            Begin
//                              delete from {3} where CreateTime = convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000) and DependServiceName = @name
//                              insert into {3} (DependServiceName,CreateTime,Alert,AlertTime) values(@name,convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000),'Y',@alertTime)
//                            End
//                            else
//                            Begin
//                                delete from {3} where CreateTime = convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000) and DependServiceName = @name
//                                insert into {3} (DependServiceName,CreateTime,Alert) values(@name,convert(decimal,convert(float,convert(datetime,@fromDate))*1000*60*60*24+59926608000000),'N')
//                            End
//                        End
//                     End
//                     set @fromDate = convert(datetime,DATEADD(hour,1,@fromDate))
//                END", ExportStartDateTime.AddHours(-1).ToString("yyyy-MM-dd HH:00:00"), ExportEndDateTime.ToString("yyyy-MM-dd HH:00:00"), incidentsTableName, DependServicesFailurePerHourTableName);
//            resultCount = SQLCommon.ExecuteQuery(cmdText);

//            Log.WriteLogForParserBlobsAndInsertDB("Insert DependServicesFailurePerHourTableName, resultCount: " + resultCount);
            #endregion
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
            DataTable dt = new DataTable(responseTableName);

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
                
                DateTime indexDate = ExportStartDateTime;

                if (SQLCommon.conn.State == ConnectionState.Closed)
                {
                    SQLCommon.conn.Open();
                }

                string cmdText = string.Format("DELETE FROM dbo.[{0}] WHERE [AlertTime]>={1} and [AlertTime]<{2}",incidentsTableName, ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                int deleteResult = SQLCommon.ExecuteQuery(cmdText);
                Log.WriteLogForParserBlobsAndInsertDB("Delete " + incidentsTableName + ", resultCount: " + deleteResult);
                cmdText = string.Format("DELETE FROM dbo.[{0}] WHERE [RunTime]>={1} and [RunTime]<{2}",responseTableName, ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                deleteResult = SQLCommon.ExecuteQuery(cmdText);
                Log.WriteLogForParserBlobsAndInsertDB("Delete " + responseTableName + ", resultCount: " + deleteResult);
                
                #region Query DependServices.
                cmdText = string.Format(@"SELECT [WebTestName],[ApplicationInsightName],[DependServiceId] FROM [dbo].[WebTest] WHERE [BelongsToGraph]={0}", ISGraph ? "1" : "0");
                int result2;
                DataTable dt2 = SQLCommon.Query(cmdText, out result2);
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
                while (indexDate.CompareTo(ExportEndDateTime) < 0)                
                {
                    Console.WriteLine(indexDate.ToString());
                    OutputFolder = Path.Combine(ISGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"], indexDate.ToString("yyyy-MM-dd"), indexDate.ToString("HH"));

                    if(!Directory.Exists(OutputFolder))
                    {
                        indexDate = indexDate.AddHours(1);
                        continue;
                    }
                    string[] files = Directory.GetFiles(OutputFolder);

                    Dictionary<string, Tuple<bool, Incident>> flag = new Dictionary<string, Tuple<bool, Incident>>();

                    #region Query Incidents which HealthTime is null.

                    cmdText = string.Format(@"SELECT [{0}].[Id], [{0}].[WebTestName], [Location], [AlertTime] FROM [dbo].[{0}], [dbo].[WebTest]
                                            WHERE [HealthTime] IS NULL AND [{0}].[WebTestName] = [WebTest].[WebTestName] AND [{0}].[ApplicationInsightName] = [WebTest].[ApplicationInsightName] AND [WebTest].IsDisable = 0 AND [WebTest].BelongsToGraph = {1}
                                            ORDER BY [AlertTime] DESC",incidentsTableName, ISGraph?"1":"0");
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

                    foreach (var filePath in files)
                    {
                        #region Get availabilityList from each file.
                        List<Availability> availabilityList = new List<Availability>();
                        string jsonText = File.ReadAllText(filePath);

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
                        int count = 0;
                        int passCount = 0;
                        int failedCount = 0;
                        DataTable tempTable = MakeTable();
                        foreach (var availability in availabilityList)
                        {
                            string key = availability.testName + '_' + availability.runLocation;
                            if (reponseTimeInsightNames.Contains(applicationInsightName))
                            {
                                #region Insert TempResponseNew table

                                if (serviceCaseMapping.ContainsKey(availability.testName.ToLower() + "_" + applicationInsightName.ToLower()))
                                {
                                    DataRow row = tempTable.NewRow();
                                    row["BelongsToGraph"] = ISGraph;
                                    row["WebTestName"] = availability.testName;
                                    row["ApplicationName"] = applicationInsightName;
                                    row["ResponseTime"] = availability.durationMetric.value / 10000;
                                    row["Location"] = availability.runLocation;
                                    row["RunTime"] = availability.testTimestamp.ToUniversalTime().Ticks / 10000;
                                    row["IsPassed"] = availability.result.ToLower().Contains("Pass".ToLower()) ? 1 : 0;
                                    tempTable.Rows.Add(row);
                                    tempTable.AcceptChanges();
                                    count++;
                                }
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
                                                string excuteCmdText = string.Format(@"UPDATE [dbo].[{3}]
                                                              SET [HealthTime] = {0}
                                                              ,[Duration] = {1}                                                                              
                                                              WHERE [Id] = {2}", availability.testTimestamp.ToUniversalTime().Ticks / 10000, duration, incident.Id, incidentsTableName);

                                                int excuteResult = SQLCommon.ExecuteQuery(excuteCmdText);

                                                if (excuteResult!=-1)
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
                                    if (serviceCaseMapping.ContainsKey(availability.testName.ToLower()+"_"+incident.ApplicationInsightName.ToLower()))
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
                                    //if (dependServiceId < 0)
                                    //{
                                    //    excuteCmdText = excuteCmdText.Replace(@"[DependServiceId],", "").Replace(@"VALUES(-1, ", @"VALUES (");
                                    //}

                                    
                                }

                                #endregion
                            }
                            count++;
                            #endregion

                            if (tempTable.Rows.Count == 1000)
                            {
                                bool executeSqlBulkCopyResult = SQLCommon.ExecuteSqlBulkCopy(tempTable, string.Format("dbo.[{0}]",responseTableName));
                                if (executeSqlBulkCopyResult)
                                {
                                    tempTable.Rows.Clear();
                                }
                            }
                        }

                        if (tempTable.Rows.Count > 0)
                       {
                            bool executeSqlBulkCopyResult2 = SQLCommon.ExecuteSqlBulkCopy(tempTable, string.Format("dbo.[{0}]", responseTableName));
                            if (executeSqlBulkCopyResult2)
                            {
                                tempTable.Rows.Clear();
                            }
                        }

                        DateTime end = DateTime.Now;
                        TimeSpan ts = new TimeSpan(end.Ticks - start.Ticks);
                        Console.WriteLine(string.Format(applicationInsightName + "'s spent time:{0}, Count:{1}, Passed Count:{2}, Failed Count:{3}", ts.ToString(), count, passCount, failedCount));

                    }
                    indexDate = indexDate.AddHours(1);
               }
                if (SQLCommon.conn.State != ConnectionState.Closed)
                {
                    SQLCommon.conn.Close();
                }
            }
            catch (Exception e)
            {
                Log.WriteLogForParserBlobsAndInsertDB(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
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
            log.LogPath = string.Format(ISGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], DateTime.Now.ToString("yyyyMMdd"), ai);

            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);                

                //If ExportDate is empty, download the files in before 1 hour, else download the files which is between export date and before 1 hour.
                DateTime indexDate = ExportStartDateTime;
                bool isSuccessDownload = true;
                while(indexDate.CompareTo(ExportEndDateTime)<0)
                {
                    DateTime start2 = DateTime.Now;
                    string prefix = ai + @"/Availability/" + indexDate.ToString("yyyy-MM-dd") + '/' + indexDate.ToString("HH");
                    var blobs = container.ListBlobs(prefix, useFlatBlobListing: true).ToList();
                    if (blobs.Count > 0)
                    {
                        log.WriteLog("start download " + prefix);
                        string outputFolder = Path.Combine(ISGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"], indexDate.ToString("yyyy-MM-dd"), indexDate.ToString("HH"));
                        if (!Directory.Exists(outputFolder))
                        {
                            Directory.CreateDirectory(outputFolder);
                        }

                        string filePath = string.Format(@"{0}\{1}.blob", outputFolder, ai);
                        log.WriteLog("ThreadName: " + Thread.CurrentThread.Name + ", filePath: " + filePath + ",outputFolder: " + outputFolder+ ",:blob.count: " + blobs.Count);
                        if (File.Exists(filePath))
                        {
                           File.Delete(filePath);
                        }
                        foreach (ICloudBlob blob in blobs)
                        {
                            try
                            {
                                blob.DownloadToFile(filePath, System.IO.FileMode.Append);
                            }
                            catch (Exception e)
                            {
                                log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                                throw;
                            }
                            finally
                            {

                            }
                        }

                        if(isSuccessDownload)
                        {
                            DateTime end2 = DateTime.Now;
                            TimeSpan s2 = end2 - start2;
                            log.WriteLog("Finish download " + prefix + " Spent time: " + s2.ToString());
                        }
                        else
                        {
                            log.WriteLog("Failed download " + prefix);
                        }

                    }                    
                    indexDate = indexDate.AddHours(1);
                }

                if (isSuccessDownload)
                {
                    DateTime end = DateTime.Now;
                    TimeSpan s = end - start;
                    log.WriteLog("Finish all download " + ai + " Spent time: " + s.ToString());
                }
                else
                {
                    log.WriteLog("Failed download " + ai);
                }

            }
            catch (Exception e)
            {
                log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }
    }
}
