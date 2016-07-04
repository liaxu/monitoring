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

namespace WebServerLog2DB
{
    class Program
    {
        static string AINamesForDev = ConfigurationManager.AppSettings["AINamesForDev"];
        static string AINamesForGraph = ConfigurationManager.AppSettings["AINamesForGraph"];
        static string StorageName = ConfigurationManager.AppSettings["StorageName"];
        static string StorageKey = ConfigurationManager.AppSettings["StorageKey"];
        static string IsGraph = ConfigurationManager.AppSettings["IsGraph"];
        static string DTNameForGraph = ConfigurationManager.AppSettings["DTNameForGraph"];
        static string DTNameForDev = ConfigurationManager.AppSettings["DTNameForDev"];
        static string OutputFolder = ConfigurationManager.AppSettings["OutputFolder"];
        static string connStr = ConfigurationManager.AppSettings["ConnStr"];
        static string LastUpdateTime = ConfigurationManager.AppSettings["LastUpdateTime"];
        static string tableName = (IsGraph == "1") ? DTNameForGraph : DTNameForDev;
        static string AINames = (IsGraph == "1") ? AINamesForGraph : AINamesForDev;
        static string Container = (IsGraph == "1") ? ConfigurationManager.AppSettings["ContainerForGraph"] : ConfigurationManager.AppSettings["ContainerForDev"];
        static string ExportStartDate = ConfigurationManager.AppSettings["ExportStartDate"];
        static string ExportEndDate = ConfigurationManager.AppSettings["ExportEndDate"];
        static DateTime currentStartTime = Convert.ToDateTime(DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:00:00"));
        static DateTime currentEndTime = Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd HH:00:00"));
        static DateTime ExportStartDateTime = string.IsNullOrEmpty(ExportStartDate) ? currentStartTime : Convert.ToDateTime(Convert.ToDateTime(ExportStartDate).ToString("yyyy-MM-dd HH:00:00"));
        static DateTime ExportEndDateTime = string.IsNullOrEmpty(ExportEndDate) ? currentEndTime : Convert.ToDateTime(Convert.ToDateTime(ExportEndDate).ToString("yyyy-MM-dd HH:00:00"));
        
        static void Main(string[] args)
        {
            try
            {
                DateTime start = DateTime.Now;  
                //Get and insert web server log (including ip, url, actiontime and so on.) into DB per hour.
                GetThenDeleteBlobs();
                // Generate and insert PV, UV and action time into DB per hour.
                GeneratePerHourTable();
                DateTime end = DateTime.Now;
                TimeSpan s = end - start;
                Log.WriteLog("Finish all Spent time: " + s.ToString());

                if (SQLCommon.conn.State != ConnectionState.Closed)
                {
                    SQLCommon.conn.Close();
                }
            }
            catch(Exception ex)
            {
                Log.WriteLog(ex.Message, true,
                    string.Format("{0}-{1}: Download and Insert {2} tables Failed.", ExportStartDateTime.ToString("yyyy-MM-dd HH"), ExportEndDateTime.ToString("yyyy-MM-dd HH"), tableName));
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

            if (IsGraph == "1")
            {
                string cmdText = string.Format("DELETE FROM dbo.{0} WHERE [DateTime]>={1} and [DateTime]<{2}", "[UVandPVPerHourForGraph]", ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                int result = SQLCommon.ExecuteQuery(cmdText);

                Log.WriteLog(cmdText + ", COUNT:" + result);

                #region SQL for insert records per hour
                cmdText = string.Format(@"
                    INSERT INTO [UVandPVPerHourForGraph]
                    SELECT
                        [HourTime] AS 'DateTime',
		                COUNT(*) AS [PV],
                        COUNT(DISTINCT(ip)) AS [UV],
                        AVG([actionTime]) AS [ResponseTime]
                    FROM [dbo].[UVandPVForGraph]    
                    WHERE [HourTime]>={0} AND [HourTime]<{1} AND ([status]<300 or [status]>400) 
                    GROUP BY [HourTime]
                    ORDER BY [HourTime]", ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                #endregion
                result = SQLCommon.ExecuteQuery(cmdText);
                Log.WriteLog("INSERT INTO [UVandPVPerHourForGraph], COUNT:" + result);
            }
            else
            {
                string cmdText = string.Format("DELETE FROM dbo.{0} WHERE [DateTime]>={1} and [DateTime]<{2}", "[UVandPVPerHourForDev]", ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                int result = SQLCommon.ExecuteQuery(cmdText);
                Log.WriteLog(cmdText + ", COUNT:" + result);

                #region SQL for insert records per hour
                cmdText = string.Format(@"
                    INSERT INTO [UVandPVPerHourForDev]
                    SELECT
                        [HourTime] AS 'DateTime',
		                COUNT(*) AS [PV],
                        COUNT(DISTINCT(ip)) AS [UV],
                        AVG([actionTime]) AS [ResponseTime]
                    FROM [dbo].[UVandPVForDev] 
                    WHERE [HourTime]>={0} AND [HourTime]<{1} AND ([Status]<300 or [Status]>=400) 
                    GROUP BY [HourTime]
                    ORDER BY [HourTime]", ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                #endregion
                result = SQLCommon.ExecuteQuery(cmdText);

                Log.WriteLog("INSERT INTO [UVandPVPerHourForDev], COUNT:" + result);
            }
        }

        static DataTable MakeTable()
        {
            DataTable dt= new DataTable(tableName);
            // Add three column objects to the table. 
            DataColumn id = new DataColumn();
            id.DataType = System.Type.GetType("System.Int32");
            id.ColumnName = "id";
            id.AutoIncrement = true;
            dt.Columns.Add(id);

            DataColumn status = new DataColumn();
            status.DataType = System.Type.GetType("System.Int32");
            status.ColumnName = "status";
            status.AutoIncrement = true;
            dt.Columns.Add(status);

            DataColumn ip = new DataColumn();
            ip.DataType = System.Type.GetType("System.String");
            ip.ColumnName = "ip";
            dt.Columns.Add(ip);

            DataColumn url = new DataColumn();
            url.DataType = System.Type.GetType("System.String");
            url.ColumnName = "url";
            dt.Columns.Add(url);

            DataColumn refurl = new DataColumn();
            refurl.DataType = System.Type.GetType("System.String");
            refurl.ColumnName = "refurl";
            dt.Columns.Add(refurl);

            DataColumn timeString = new DataColumn();
            timeString.DataType = System.Type.GetType("System.Int64");
            timeString.ColumnName = "timeString";
            dt.Columns.Add(timeString);

            DataColumn actionTime = new DataColumn();
            actionTime.DataType = System.Type.GetType("System.Int32");
            actionTime.ColumnName = "actionTime";
            dt.Columns.Add(actionTime);

            DataColumn belongsToGraph = new DataColumn();
            belongsToGraph.DataType = System.Type.GetType("System.Boolean");
            belongsToGraph.ColumnName = "belongsToGraph";
            dt.Columns.Add(belongsToGraph);

            DataColumn dayTime = new DataColumn();
            dayTime.DataType = System.Type.GetType("System.Int64");
            dayTime.ColumnName = "dayTime";
            dt.Columns.Add(dayTime);

            DataColumn HourTime = new DataColumn();
            HourTime.DataType = System.Type.GetType("System.Int64");
            HourTime.ColumnName = "HourTime";
            dt.Columns.Add(HourTime);

            // Create an array for DataColumn objects.
            DataColumn[] keys = new DataColumn[1];
            keys[0] = id;
            dt.PrimaryKey = keys;

            return dt;
        }
        public static void ParserBlobsAndInsertDB(string text)
        {
            try
            {
                if (SQLCommon.conn.State == ConnectionState.Closed)
                {
                    SQLCommon.conn.Open();
                }

                List<Record> recordList = WebServerlogParser.Parse(text);

                DataTable tempTable = MakeTable();
                foreach (var record in recordList)
                {
                    #region Insert ResponseNew table
                    DataRow row = tempTable.NewRow();
                    row["ip"] = record.ip;
                    row["url"] = record.url;
                    row["refurl"] = record.refurl;
                    row["timeString"] = record.timeString;
                    row["HourTime"] = record.hourTime;
                    row["dayTime"] = record.dayTime;
                    if (record.status != -1)
                    {
                        row["actionTime"] = record.actionTime;
                        row["status"] = record.status;
                    }
                    else
                    {
                       
                    }
                    tempTable.Rows.Add(row);
                    tempTable.AcceptChanges();
                    #endregion

                    if (tempTable.Rows.Count == 1000)
                    {
                        bool executeSqlBulkCopyResult = SQLCommon.ExecuteSqlBulkCopy(tempTable, tableName);
                        if (executeSqlBulkCopyResult)
                        {
                            tempTable.Rows.Clear();
                        }
                    }
                }

                if (tempTable.Rows.Count > 0)
                {
                    bool executeSqlBulkCopyResult2 = SQLCommon.ExecuteSqlBulkCopy(tempTable, tableName);
                    if (executeSqlBulkCopyResult2)
                    {
                        tempTable.Rows.Clear();
                    }
                }
                if (SQLCommon.conn.State != ConnectionState.Closed)
                {
                    SQLCommon.conn.Close();
                }
            }
            catch (Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }

        static void GetThenDeleteBlobs()
        {
            try
            {
                DateTime start = DateTime.Now;
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);

                if (SQLCommon.conn.State == ConnectionState.Closed)
                {
                    SQLCommon.conn.Open();
                }
                string cmdText = string.Format("DELETE FROM dbo.{0} WHERE [timeString]>={1} and [timeString]<{2}", tableName, ExportStartDateTime.Ticks / 10000, ExportEndDateTime.Ticks / 10000);
                int deleteResult = SQLCommon.ExecuteQuery(cmdText);
                Log.WriteLog(cmdText + ", COUNT:" + deleteResult);
                List<string> applicationInsight = AINames.Split(';').ToList();
                foreach (var ai in applicationInsight)
                {
                    //If ExportDate is empty, download the files in before 1 hour, else download the files which is between export date and before 1 hour.
                    DateTime indexDate = ExportStartDateTime;
                    bool isSuccessDownload = true;
                    while (indexDate.CompareTo(ExportEndDateTime) < 0)
                    {
                        DateTime start2 = DateTime.Now;
                        Console.WriteLine(start2.ToString());

                        string prefix = ai + "/" + indexDate.ToString("yyyy") + "/" + indexDate.ToString("MM") + "/" + indexDate.ToString("dd") + "/" + indexDate.ToString("HH");
                        var blobs = container.ListBlobs(prefix, useFlatBlobListing: true).ToList();
                        Log.WriteLog("start download " + prefix);
                        foreach (ICloudBlob blob in blobs)
                        {
                            try
                            {
                                string text;
                                using (var memoryStream = new MemoryStream())
                                {
                                    blob.DownloadToStream(memoryStream);
                                    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                                    ParserBlobsAndInsertDB(text);
                                }
                            }
                            catch (Exception e)
                            {
                                isSuccessDownload = false;
                                Log.WriteLog("Failed download " + prefix);
                                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);  
                                throw;
                            }
                            finally
                            {

                            }
                        }

                        if (isSuccessDownload)
                        {
                            DateTime end2 = DateTime.Now;
                            TimeSpan s2 = end2 - start2;
                            Log.WriteLog("Finish download and insert DB: " + prefix + " Files in total: " + blobs.Count + " Spent time: " + s2.ToString());
                        }

                        indexDate = indexDate.AddHours(1);
                    }

                    if (isSuccessDownload)
                    {
                        DateTime end = DateTime.Now;
                        TimeSpan s = end - start;
                        Log.WriteLog("Finish all in" + ai + " Spent time: " + s.ToString());
                    }
                    else
                    {
                        Log.WriteLog("Failed in " + ai);
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }
    }
}
