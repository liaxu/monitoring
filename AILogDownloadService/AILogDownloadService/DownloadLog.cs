using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;

namespace AILogDownloadService
{
    class DownloadLog
    {
        public void downloadLog(object isGraphObj)
        {
            bool isGraph = (bool)isGraphObj;
            Log log = new Log();
            log.ErrorLog=string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DownloadErroLog", "error");
            log.HistoryLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DownloadSpentTimeLog", "log");
            string AINames = isGraph ? ConfigurationManager.AppSettings["AINamesForGraph"] : ConfigurationManager.AppSettings["AINamesForDev"];
            string StorageName = ConfigurationManager.AppSettings["StorageName"];
            string StorageKey = ConfigurationManager.AppSettings["StorageKey"];
            string Container = ConfigurationManager.AppSettings["Container"];
            string OutputFolder = isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"];

            string ExportStartDate = ConfigurationManager.AppSettings["ExportStartDate"];
            string ExportEndDate = ConfigurationManager.AppSettings["ExportEndDate"];
            DateTime currentStartTime = Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd HH:00:00"));
            DateTime ExportStartDateTime = string.IsNullOrEmpty(ExportStartDate) ? currentStartTime : Convert.ToDateTime(Convert.ToDateTime(ExportStartDate).ToString("yyyy-MM-dd HH:00:00"));
            int sleepTime = Convert.ToInt32(ConfigurationManager.AppSettings["SleepTime"]);
            List<string> applicationInsights = AINames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);

                DateTime indexDate = ExportStartDateTime;
                while (true)
                {
                    DateTime start2 = DateTime.UtcNow;
                    foreach (var ai in applicationInsights)
                    {
                        log.LastFinished = string.Format(isGraph? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "LastFinished", ai);
                        string lastFile = string.Empty;
                        if (File.Exists(log.LastFinished))
                            lastFile = File.ReadAllText(log.LastFinished);

                        if (!string.IsNullOrEmpty(lastFile))
                        {
                            lastFile = lastFile.Split('#')[2].Trim();
                            indexDate = Convert.ToDateTime(lastFile).ToUniversalTime();
                        }

                        DateTimeOffset lastFileTime = new DateTimeOffset(indexDate);

                        string outputFolder = Path.Combine(isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"], ParserApplicationInsightName(ai), indexDate.ToString("yyyy-MM-dd"), indexDate.ToString("HH"));
                        string prefix = ai + @"/Availability/" + indexDate.ToString("yyyy-MM-dd") + '/' + indexDate.ToString("HH");
                        var blobs = container.ListBlobs(prefix, useFlatBlobListing: true).OfType<CloudBlob>().OrderBy(temp => temp.Properties.LastModified).Where(temp => temp.Properties.LastModified > lastFileTime).ToList();
                        if (blobs.Count == 0)
                        {

                            prefix = ai + @"/Availability/" + indexDate.AddHours(1).ToString("yyyy-MM-dd") + '/' + indexDate.AddHours(1).ToString("HH");
                            blobs = container.ListBlobs(prefix, useFlatBlobListing: true).OfType<CloudBlob>().OrderBy(temp => temp.Properties.LastModified).Where(temp => temp.Properties.LastModified > lastFileTime).ToList();
                            outputFolder = Path.Combine(isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"], ParserApplicationInsightName(ai), indexDate.AddHours(1).ToString("yyyy-MM-dd"), indexDate.AddHours(1).ToString("HH"));
                        }

                        if (!Directory.Exists(outputFolder))
                        {
                            Directory.CreateDirectory(outputFolder);
                        }
                        if (blobs.Count > 0)
                        {
                            foreach (ICloudBlob blob in blobs)
                            {
                                try
                                {
                                    string tempFilePath = string.Format(@"{0}\{1}", outputFolder, Path.GetFileNameWithoutExtension(blob.Name));
                                    string filePath = Path.ChangeExtension(tempFilePath, ".blob");
                                    if (File.Exists(filePath))
                                    {
                                        File.Delete(filePath);
                                    }
                                    blob.DownloadToFile(tempFilePath, System.IO.FileMode.OpenOrCreate);
                                    File.Move(tempFilePath, filePath);
                                    File.SetCreationTime(filePath, blob.Properties.LastModified.Value.UtcDateTime);
                                    log.WriteDownloadLog(filePath + "#" + blob.Properties.LastModified.ToString());
                                }
                                catch (Exception e)
                                {
                                    log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                                    throw;
                                }
                                finally
                                {

                                }
                            }

                        }
                    }

                    DateTime end2 = DateTime.UtcNow;
                    TimeSpan s2 = end2 - start2;
                    log.WriteLog("Finish download spent time: " + s2.ToString());
                    Console.WriteLine(end2.ToString() + " Finish download spent time: " + s2.ToString());
                    Thread.Sleep(sleepTime);
                }
            }
            catch (Exception e)
            {
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, string.Format("{0} stoped", isGraph?"GraphAILogDownloadService": "DevAILogDownloadService"));
                throw;
            }
            finally
            {

            }
        }

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
    }
}
