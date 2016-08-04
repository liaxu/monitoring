using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IISLogDownloadService
{
    class IISLogDownload
    {
        public void downloadLog(object isGraphObj)
        {
            #region Initial
            bool isGraph = (bool)isGraphObj;
            Log log = new Log();
            string logPath = isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"];
            log.HistoryLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DownloadSpentTimeLog");

            string dateTimeFormat = "yyyy-MM-dd HH:00:00";
            string AINames = isGraph ? ConfigurationManager.AppSettings["AINamesForGraph"] : ConfigurationManager.AppSettings["AINamesForDev"];
            string StorageName = ConfigurationManager.AppSettings["StorageName"];
            string StorageKey = ConfigurationManager.AppSettings["StorageKey"];
            string Container = isGraph ? ConfigurationManager.AppSettings["ContainerForGraph"] : ConfigurationManager.AppSettings["ContainerForDev"];
            string OutputFolder = isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"];

            string ExportStartDate = ConfigurationManager.AppSettings["ExportStartDate"];
            string ExportEndDate = ConfigurationManager.AppSettings["ExportEndDate"];
            DateTime currentStartTime = Convert.ToDateTime(DateTime.UtcNow.ToString(dateTimeFormat));
            DateTime ExportStartDateTime = string.IsNullOrEmpty(ExportStartDate) ? currentStartTime : Convert.ToDateTime(Convert.ToDateTime(ExportStartDate).ToString(dateTimeFormat));
            List<string> applicationInsight = AINames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            bool throwException = true;
            DateTime indexDate = ExportStartDateTime;
            Dictionary<string, LastFile> filelines = new Dictionary<string, LastFile>();
            #endregion
            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);

               Dictionary<string, LastFile> lastLines = new Dictionary<string, LastFile>();
               #region Find the file which is lasted modifed.
                if (Directory.Exists(OutputFolder))
                {
                    DirectoryInfo dir = new DirectoryInfo(OutputFolder);
                    IEnumerable<FileInfo> files = dir.GetFiles("*.csv", SearchOption.AllDirectories).OrderByDescending(x => x.LastWriteTimeUtc).Take(1);
                    if (files.Count() > 0)
                    {
                        foreach (FileInfo file in files)
                        {
                            indexDate = Convert.ToDateTime(file.Directory.Name + " " + Path.GetFileNameWithoutExtension(file.Name) + ":00:00");
                            break;
                        }
                    }
                }
               #endregion
                                
                while (true)
                {
                    lastLines = new Dictionary<string, LastFile>();
                    log.LastFinished = string.Format(logPath, "LastFinished" + indexDate.ToString("yyyyMMddHH"));

                    if (File.Exists(log.LastFinished))
                    {
                        foreach (var line in File.ReadAllLines(log.LastFinished))
                        {
                            if (!string.IsNullOrEmpty(line.Trim()))
                            {
                                string[] tempInfo = line.Trim().Split('#');
                                if (indexDate.ToString("yyyyMMddHH") == Convert.ToDateTime(tempInfo[2]).ToString("yyyyMMddHH"))
                                {
                                    LastFile tempLastFile = new LastFile(tempInfo[1].Trim(), "", Convert.ToDateTime(tempInfo[2]), 0, Convert.ToBoolean(tempInfo[4]));
                                    lastLines.Add(tempInfo[1].Trim(), tempLastFile);
                                }
                            }
                        }
                    }

                    string nowHour = DateTime.UtcNow.ToString(dateTimeFormat);
                    string indexDateHour = indexDate.ToString(dateTimeFormat);
                    bool nextHour = (DateTime.UtcNow - Convert.ToDateTime(indexDateHour)).TotalMinutes > 61;
                    var blobs = container.ListBlobs(useFlatBlobListing: true).OfType<CloudBlob>()
                                   .Where(temp => temp.Name.Contains(indexDate.ToString("/yyyy/MM/dd/HH/"))).ToList();

                    foreach (var blob in blobs)
                    {
                        DateTime blobLastModified = blob.Properties.LastModified.Value.UtcDateTime;
                        if (lastLines.ContainsKey(blob.Name))
                        {
                            if (nextHour && lastLines[blob.Name].lastModifiedDate >= blobLastModified)
                            {
                                lastLines[blob.Name].isFinished = true;
                            }
                            else
                            {
                                lastLines[blob.Name].isFinished = false;
                            }
                        }
                        else
                        {
                            lastLines.Add(blob.Name, new LastFile(blob.Name,Convert.ToDateTime(blobLastModified.ToString(dateTimeFormat))));
                        }
                    }

                    WriteLog(lastLines, log);

                    if (blobs.Count==0)
                    {
                        if (indexDateHour == nowHour)
                        {
                            Thread.Sleep(60000);
                            continue;
                        }
                        else if(nextHour)
                        {
                            indexDate = Convert.ToDateTime(indexDateHour).AddHours(1);
                            continue;
                        }
                    }

                    if (nextHour && lastLines.Count() > 0 && !lastLines.Values.Any(x => x.isFinished == false))
                    {
                        indexDate = Convert.ToDateTime(indexDateHour).AddHours(1);
                        continue;
                    }


                    if (blobs.Count > 0)
                    {
                        string outputFolder = Path.Combine(OutputFolder, indexDate.ToString("yyyy-MM-dd"));
                        string outputFolderTemp = Path.Combine(OutputFolder, DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"));

                        if (Directory.Exists(outputFolderTemp))
                        {
                            Directory.Delete(outputFolderTemp, true);
                        }

                        if (!Directory.Exists(outputFolder))
                        {
                            Directory.CreateDirectory(outputFolder);
                        }

                        string tempBlobPath = Path.Combine(outputFolder, indexDate.ToString("HH") + ".tempblob");
                        string tempFilePath = Path.Combine(outputFolder, indexDate.ToString("HH") + ".tempcsv");
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }

                        File.WriteAllText(tempFilePath, "date,time,s-sitename,cs-method,cs-uri-stem,cs-uri-query,s-port,cs-username,c-ip,cs(User-Agent),cs(Cookie),cs(Referer),cs-host,sc-status,sc-substatus,sc-win32-status,sc-bytes,cs-bytes,time-taken,ai_session,ai_user\r\n");
                        bool iswritten = false;
                        foreach (ICloudBlob blob in blobs)
                        {
                            try
                            {
                                DateTime blobLastModified = blob.Properties.LastModified.Value.UtcDateTime;
                                if (!lastLines.ContainsKey(blob.Name))
                                {
                                    LastFile tempLastFile = new LastFile(blob.Name,Convert.ToDateTime(blobLastModified.ToString(dateTimeFormat)));
                                    lastLines.Add(blob.Name, tempLastFile);
                                }

                                if (File.Exists(tempBlobPath))
                                {
                                    File.Delete(tempBlobPath);
                                }
                                blob.DownloadToFile(tempBlobPath, FileMode.OpenOrCreate);

                                #region Write into local file
                                int count = 0;
                                using (StreamReader reader = new StreamReader(tempBlobPath))
                                {
                                    FileStream fs = new FileStream(tempFilePath, FileMode.OpenOrCreate);
                                    fs.Close();
                                    using (StreamWriter outfile = new StreamWriter(tempFilePath, true, Encoding.Default))
                                    {
                                        while (!reader.EndOfStream)
                                        {
                                            string line = reader.ReadLine();
                                            if (count++ >= 2)
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

                                                outfile.WriteLine(line.Replace(",", "{comma}").Replace(' ', ',') + "," + ai_session + "," + ai_user);
                                                iswritten = true;
                                            }
                                        }
                                        outfile.Flush();
                                        outfile.Close();                                        
                                    }
                                }
                                lastLines[blob.Name].lastModifiedDate = blobLastModified;
                                File.Delete(tempBlobPath);
                            }
                            catch (Exception e)
                            {
                                log.ErrorLog = string.Format(logPath, "ErroLog" + indexDate.ToString("yyyyMMddHH"));
                                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                                if (throwException)
                                {
                                    throw;
                                }
                                else
                                {
                                    Thread.Sleep(40000);
                                }
                            }
                            finally
                            {

                            }
                            #endregion
                        }

                        if (iswritten)
                        {
                            string filePath = Path.ChangeExtension(tempFilePath, ".csv");
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }
                            File.Move(tempFilePath, filePath);
                            File.SetLastWriteTimeUtc(filePath, lastLines.Values.Max(x => x.lastModifiedDate));
                        }
                        else
                        {
                            File.Delete(tempFilePath);
                        }

                        log.LastFinished = string.Format(logPath, "LastFinished" + indexDate.ToString("yyyyMMddHH"));
                        WriteLog(lastLines, log);
                    }
                    Thread.Sleep(60000);
                }
            }
            catch (Exception e)
            {
                log.ErrorLog = string.Format(logPath, "ErroLog" + indexDate.ToString("yyyyMMddHH"));
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, string.Format("{0} stopped", isGraph ? "Graph IISLogDownloadService" : "Dev IISLogDownloadService"));
                throw;
            }
            finally
            {

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
    }
}
