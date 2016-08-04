using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IISLogInsertService
{
    class IISLogInsert
    {
        bool isGraph { get; set; }
        Log log { get; set; }
        string dateTimeFormat { get; set; }
        string AINames { get; set; }
        string StorageName { get; set; }
        string StorageKey { get; set; }
        string Container { get; set; }
        string OutputFolder { get; set; }
        DateTime currentStartTime { get; set; }
        DateTime ExportStartDateTime { get; set; }
        List<string> applicationInsight { get; set; }
        bool throwException { get; set; }
        //Dictionary<string, List<LastFile>> filelines { get; set; }

        List<LastFile> tempFilelines { get; set; }
        int sleepTime { get; set; }
        string tableName { get; set; }

        public IISLogInsert(bool isGraphObj)
        {
            isGraph = isGraphObj;
            log = new Log();
            log.ErrorLog = isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"];
            log.HistoryLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "HistoryLog");
            log.LastFinished = isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"];
            dateTimeFormat = "yyyy-MM-dd HH:00:00";
            AINames = isGraph ? ConfigurationManager.AppSettings["AINamesForGraph"] : ConfigurationManager.AppSettings["AINamesForDev"];
            StorageName = ConfigurationManager.AppSettings["StorageName"];
            StorageKey = ConfigurationManager.AppSettings["StorageKey"];
            Container = isGraph ? ConfigurationManager.AppSettings["ContainerForGraph"] : ConfigurationManager.AppSettings["ContainerForDev"];
            string ExportStartDate = ConfigurationManager.AppSettings["ExportStartDate"];
            tableName = isGraph ? ConfigurationManager.AppSettings["TableNameForGraph"] : ConfigurationManager.AppSettings["TableNameForDev"];
            OutputFolder = isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"];
            currentStartTime = Convert.ToDateTime(DateTime.UtcNow.ToString(dateTimeFormat));
            ExportStartDateTime = string.IsNullOrEmpty(ExportStartDate) ? currentStartTime : Convert.ToDateTime(Convert.ToDateTime(ExportStartDate).ToString(dateTimeFormat));
            applicationInsight = AINames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            throwException = true;
            //filelines = InitialFileLines();
            sleepTime = Convert.ToInt32(ConfigurationManager.AppSettings["SleepTime"]);
        }

        private DateTime parserName(string name)
        {
            string[] nameStr = name.Split('/');
            if (nameStr.Length >= 6)
            {
                DateTime dt = new DateTime(
                    Convert.ToInt32(nameStr[1]),
                    Convert.ToInt32(nameStr[2]),
                    Convert.ToInt32(nameStr[3]),
                    Convert.ToInt32(nameStr[4]), 0, 0);
                return dt;

            }
            return DateTime.MinValue;
        }

        private Dictionary<string, List<LastFile>> InitialFileLines()
        {
            Dictionary<string, List<LastFile>> tempFilelines = new Dictionary<string, List<LastFile>>();
            #region Initial filelines
            if (File.Exists(log.LastFinished))
            {
                foreach (var line in File.ReadAllLines(log.LastFinished))
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] items = line.Split('#');
                        LastFile lastFile = new LastFile(items[1].Trim(),
                            items[2].Trim(),
                            Convert.ToDateTime(items[3].Trim()).ToUniversalTime(),
                            Convert.ToInt32(items[4].Trim()),
                            Convert.ToBoolean(items[5].Trim()));
                        string prefix = lastFile.name.Split('/')[0];
                        string blobName = lastFile.name.Substring(lastFile.name.LastIndexOf('/') + 1);
                        string key = prefix;

                        DateTime dtOfLastFile = parserName(lastFile.name);
                        if (tempFilelines.ContainsKey(key))
                        {
                            LastFile existingFile = null;
                            foreach (var fileline in tempFilelines[key])
                            {
                                DateTime dt = parserName(fileline.name);

                                if (dtOfLastFile > dt || (fileline.name == lastFile.name && fileline.lastModifiedDate < lastFile.lastModifiedDate))
                                {
                                    existingFile = fileline;
                                    break;
                                }
                            }
                            if (existingFile != null)
                            {
                                tempFilelines[key].Remove(existingFile);
                            }
                            tempFilelines[key].Add(lastFile);
                        }
                        else
                        {
                            List<LastFile> list = new List<LastFile>();
                            list.Add(lastFile);
                            tempFilelines.Add(key, list);
                        }
                    }
                }
            }
            else
            {
                foreach (var item in applicationInsight)
                {
                    LastFile lastFile = new LastFile(item, ExportStartDateTime);
                    string prefix = item;

                    if (!tempFilelines.ContainsKey(prefix))
                    {
                        List<LastFile> list = new List<LastFile>();
                        list.Add(lastFile);
                        tempFilelines.Add(prefix, list);
                    }
                }
            }
            #endregion

            return tempFilelines;
        }

        private List<string> GetNewLines(ICloudBlob blob, string ai, LastFile currentLastFile, bool updateExistingFile, out LastFile newLastFile)
        {
            try
            {
                List<string> newLines = new List<string>();
                int count = 0;
                using (var stream = blob.OpenRead())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (count++ >= 2)
                            {
                                if (updateExistingFile && count <= currentLastFile.lastLine)
                                {
                                    continue;
                                }
                                else
                                {
                                    #region Parser
                                    if (line.Trim() == String.Empty)
                                        continue;

                                    string[] paramStrings = line.Split(' ');

                                    if (paramStrings.Length > 12)
                                    {
                                        if (paramStrings[12].ToLower().Contains("mirror") || paramStrings[12].ToLower().Contains("lastgood"))
                                        {
                                            continue;
                                        }
                                    }

                                    string ai_user = "";
                                    string ai_session = "";
                                    if (paramStrings.Length > 10)
                                    {
                                        string cs_user_cookie = paramStrings[10];

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
                                    }
                                    #endregion
                                    newLines.Add(line.Replace(",", "{comma}").Replace(' ', ',') + "," + ai_session + "," + ai_user);
                                }
                            }
                        }
                    }
                }
                newLastFile = new LastFile(blob.Name, "", blob.Properties.LastModified.Value.UtcDateTime, count, false);
                return newLines;
            }
            catch (Exception e)
            {
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                if (throwException)
                {
                    throw;
                }
                else
                {
                    Thread.Sleep(sleepTime);
                }
                newLastFile = currentLastFile;
                return null;
            }
            finally
            {

            }
        }

        public void DownloadAndInsertCurrentHourLog(object isGraphObj)
        {
            DateTime indexDate = Convert.ToDateTime(DateTime.UtcNow.ToString(dateTimeFormat));
            log.ErrorLog = string.Format(log.ErrorLog, "ErroLog" + indexDate.ToString("yyyyMMddHH"));
            log.LastFinished = string.Format(log.LastFinished, "LastFinished" + indexDate.ToString("yyyyMMddHH"));
            string tempTable = tableName + indexDate.ToString("_yyyyMMdd_HH");
            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);
                Dictionary<string, LastFile> lastLines = new Dictionary<string, LastFile>();
                SQLCommon sqlcommon = new SQLCommon();
                sqlcommon.CreateTable(tempTable);
                bool nextHour = false;
                bool sendWarningMailAndStop = false;
                #region Insert into tempTable
                int tempTableCount = 0;
                while (true)
                {
                    var blobs = container.ListBlobs(useFlatBlobListing: true).OfType<CloudBlob>()
                                    .Where(temp => temp.Name.Contains(indexDate.ToString("/yyyy/MM/dd/HH/"))).ToList();
                    
                    if (blobs.Count > 0)
                    {
                        #region Insert temp table
                        foreach (ICloudBlob blob in blobs)
                        {
                            try
                            {
                                DateTime blobLastModified = blob.Properties.LastModified.Value.UtcDateTime;
                                if (!lastLines.ContainsKey(blob.Name))
                                {
                                    LastFile lastFile = new LastFile(blob.Name, "", blobLastModified, 0, false);
                                    lastLines.Add(blob.Name, lastFile);
                                }

                                if (nextHour)
                                {
                                    if (blobLastModified == lastLines[blob.Name].lastModifiedDate)
                                    {
                                        lastLines[blob.Name].isFinished = true;
                                        continue;
                                    }
                                }

                                string tempBlobPath = Path.Combine(OutputFolder+"temp", blobLastModified.ToString("yyyyMMddHHmmss_") + Path.GetFileNameWithoutExtension(blob.Name) + ".tempblob");
                                if (!Directory.Exists(OutputFolder + "temp"))
                                {
                                    Directory.CreateDirectory(OutputFolder + "temp");
                                }
                                if (File.Exists(tempBlobPath))
                                {
                                    File.Delete(tempBlobPath);
                                }

                                blob.DownloadToFile(tempBlobPath, FileMode.OpenOrCreate);

                                #region Write into local file
                                int count = 0;
                                bool writeLog = false;
                                using (StreamReader reader = new StreamReader(tempBlobPath))
                                {
                                    DataTable csvData = MakeTable(tempTable);
                                    while (!reader.EndOfStream)
                                    {
                                        string line = reader.ReadLine();
                                        if (count++ < lastLines[blob.Name].lastLine)
                                        {
                                            continue;
                                        }
                                        if (count > 2)
                                        {
                                            #region Parser
                                            if (line.Trim() == String.Empty)
                                                continue;

                                            string[] columns = line.Split(' ');
                                            if (columns.Length > 12)
                                            {
                                                if (columns[12].ToLower().Contains("mirror") || columns[12].ToLower().Contains("lastgood"))
                                                {
                                                    continue;
                                                }
                                            }
                                            string ai_user = "";
                                            string ai_session = "";
                                            if (columns.Length > 10)
                                            {
                                                string cs_user_cookie = columns[10];

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
                                            }

                                            DataRow dr = csvData.NewRow();
                                            dr["ai_session"] = ai_session;
                                            dr["ai_user"] = ai_user;
                                            dr["HOST"] = columns[12];
                                            dr["IP"] = columns[8];
                                            dr["status"] = columns[13];
                                            dr["TimeStamp"] = (int)DateTimeToUnixTimestamp(Convert.ToDateTime(columns[0] + " " + columns[1]));
                                            dr["time_taken"] = string.IsNullOrEmpty(columns[18].Trim()) ? 0 : int.Parse(columns[18].Trim());
                                            string referer = columns[11];
                                            if (referer.Length > 1000)
                                            {
                                                referer = referer.Substring(0, 1000);
                                            }
                                            dr["Referer"] = referer;
                                            string uri = columns[4];
                                            if (uri.Length > 1000)
                                            {
                                                uri = uri.Substring(0, 1000);
                                            }
                                            dr["URI"] = uri;
                                            csvData.Rows.Add(dr);
                                            #endregion
                                            if (csvData.Rows.Count == 10000)
                                            {
                                                bool executeSqlBulkCopyResult = sqlcommon.ExecuteSqlBulkCopy(csvData, string.Format("dbo.[{0}]", tempTable));
                                                if (executeSqlBulkCopyResult)
                                                {
                                                    tempTableCount += csvData.Rows.Count;
                                                    csvData.Rows.Clear();
                                                    lastLines[blob.Name].lastLine = count;
                                                    lastLines[blob.Name].lastModifiedDate = blobLastModified;
                                                    lastLines[blob.Name].isFinished = false;
                                                    WriteLog(lastLines, log);
                                                    writeLog = true;
                                                }

                                            }

                                        }
                                    }

                                    if (csvData.Rows.Count > 0)
                                    {
                                        bool executeSqlBulkCopyResult = sqlcommon.ExecuteSqlBulkCopy(csvData, string.Format("dbo.[{0}]", tempTable));
                                        if (executeSqlBulkCopyResult)
                                        {
                                            tempTableCount += csvData.Rows.Count;
                                            csvData.Rows.Clear();
                                            lastLines[blob.Name].lastLine = count;
                                            lastLines[blob.Name].lastModifiedDate = blobLastModified;
                                            lastLines[blob.Name].isFinished = false;
                                            WriteLog(lastLines, log);
                                            writeLog = true;
                                        }
                                    }

                                    if (!writeLog)
                                    {
                                        lastLines[blob.Name].lastLine = count;
                                        lastLines[blob.Name].lastModifiedDate = blobLastModified;
                                        lastLines[blob.Name].isFinished = false;
                                        WriteLog(lastLines, log);
                                    }
                                }
                                #endregion

                                File.Delete(tempBlobPath);
                            }
                            catch (Exception e)
                            {
                                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                                Thread.Sleep(40000);
                            }
                            finally
                            {

                            }
                        }
                        #endregion
                    }

                    if ((DateTime.UtcNow - indexDate).TotalMinutes >= 61)
                    {
                        nextHour = true;
                    }
                    if ((DateTime.UtcNow - indexDate).TotalMinutes >= 90)
                    {
                        sendWarningMailAndStop = true;
                    }
                    if (nextHour && !lastLines.Values.Any(x => x.isFinished == false))
                    {
                        WriteLog(lastLines, log);
                        Thread.Sleep(60000);
                        break;
                    }
                    if (sendWarningMailAndStop)
                    {
                        WriteLog(lastLines, log);
                        MailSender.GenerateAndSendMail(tempTable + " spent more than 1.5 hour, stop this thread. Please check it.");
                        return;
                    }
                    Thread.Sleep(60000);
                }
                #endregion

                MoveTable(tempTable, tableName, Path.Combine(OutputFolder, indexDate.ToString("yyyy-MM-dd"), indexDate.ToString("HH") + ".csv"), log);
                log.WriteLog(indexDate.ToString(dateTimeFormat)+"#"+tempTableCount);
                sqlcommon.CloseConn();
            }
            catch (Exception e)
            {
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, string.Format("{0} failed, please check it", tempTable));
            }
            finally
            {

            }
        }

        DataTable MakeTable(string tablename)
        {
            DataTable dt = new DataTable(tablename);

            // Add three column objects to the table. 
            DataColumn Id = new DataColumn();
            Id.DataType = System.Type.GetType("System.Int32");
            Id.ColumnName = "Id";
            Id.AutoIncrement = true;
            dt.Columns.Add(Id);

            DataColumn TimeStamp = new DataColumn();
            TimeStamp.DataType = System.Type.GetType("System.Int32");
            TimeStamp.ColumnName = "TimeStamp";
            dt.Columns.Add(TimeStamp);

            DataColumn URI = new DataColumn();
            URI.DataType = System.Type.GetType("System.String");
            URI.ColumnName = "URI";
            dt.Columns.Add(URI);

            DataColumn HOST = new DataColumn();
            HOST.DataType = System.Type.GetType("System.String");
            HOST.ColumnName = "HOST";
            dt.Columns.Add(HOST);

            DataColumn Referer = new DataColumn();
            Referer.DataType = System.Type.GetType("System.String");
            Referer.ColumnName = "Referer";
            dt.Columns.Add(Referer);

            DataColumn IP = new DataColumn();
            IP.DataType = System.Type.GetType("System.String");
            IP.ColumnName = "IP";
            dt.Columns.Add(IP);

            DataColumn ai_session = new DataColumn();
            ai_session.DataType = System.Type.GetType("System.String");
            ai_session.ColumnName = "ai_session";
            dt.Columns.Add(ai_session);

            DataColumn ai_user = new DataColumn();
            ai_user.DataType = System.Type.GetType("System.String");
            ai_user.ColumnName = "ai_user";
            dt.Columns.Add(ai_user);

            DataColumn status = new DataColumn();
            status.DataType = System.Type.GetType("System.String");
            status.ColumnName = "status";
            dt.Columns.Add(status);

            DataColumn time_taken = new DataColumn();
            time_taken.DataType = System.Type.GetType("System.Int32");
            time_taken.ColumnName = "time_taken";
            dt.Columns.Add(time_taken);

            //DataColumn MD5 = new DataColumn();
            //MD5.DataType = System.Type.GetType("System.Int32");
            //MD5.ColumnName = "MD5";
            //dt.Columns.Add(MD5);

            // Create an array for DataColumn objects.
            DataColumn[] keys = new DataColumn[1];
            keys[0] = Id;
            dt.PrimaryKey = keys;

            return dt;
        }

        public void MoveTable(string tempTable, string table, string localFile, Log log)
        {
            try
            {
                DateTime start1 = DateTime.UtcNow;
                string cmdText = string.Format("SELECT TOP 1 Id FROM {0} ORDER BY Id DESC", tempTable);
                SQLCommon sqlCommon = new SQLCommon();
                int result;
                DataTable dt = sqlCommon.Query(cmdText, out result);
                Console.WriteLine("Query count of tempTable: " + (DateTime.UtcNow - start1).ToString());
                int localFileCount = 0;

                while (true)
                {
                    try
                    {
                        localFileCount = File.ReadAllLines(localFile).Length;
                        break;
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(60000);
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    int tempTableCount = Convert.ToInt32(dt.Rows[0][0]);

                    int count = localFileCount - 1 - tempTableCount;

                    if (count > 100 && count < -100)
                    {
                        log.WriteErrorLog(string.Format("Local file count:{0}, {1}'count is {2}", localFileCount, tempTable, tempTableCount));
                    }

                    cmdText =
                            string.Format(@"INSERT INTO {0} ([TimeStamp],[URI],[HOST],[Referer],[IP],[ai_session],[ai_user],[status],[time_taken])

                            SELECT [TimeStamp],[URI],[HOST],[Referer],[IP],[ai_session],[ai_user],[status],[time_taken]
                            FROM {1}", table, tempTable);
                    start1 = DateTime.UtcNow;
                    int result2 = sqlCommon.ExecuteQuery(cmdText);
                    Console.WriteLine("Move tempTable to table: " + (DateTime.UtcNow - start1).ToString());
                    if (result2 == tempTableCount)
                    {
                        start1 = DateTime.UtcNow;
                        cmdText = string.Format(@"DROP TABLE {0}", tempTable);
                        sqlCommon.ExecuteQuery(cmdText);

                        Console.WriteLine("Drop tempTable: " + (DateTime.UtcNow - start1).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Move Table from {0} to {1} Failed", tempTable, table) +
                    Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        public void WriteLog(Dictionary<string, LastFile> lastLines, Log log)
        {
            StringBuilder sb = new StringBuilder();
            int itemline = 0;
            foreach (var item in lastLines.Values)
            {
                string tempStr = item.name + "#" + item.lastModifiedDate.ToString("yyyy-MM-dd HH:mm:ss") + "#" + item.lastLine.ToString() + "#" + item.isFinished.ToString();
                if (itemline++ > 0)
                {
                    sb.AppendLine("#" + tempStr);
                }
                else
                {
                    sb.AppendLine(tempStr);
                }
            }
            log.WriteDownloadLog(sb.ToString());
        }

        public DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public int DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt32((dateTime -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
        }

    }
}