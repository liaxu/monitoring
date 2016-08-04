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

namespace PageViewsLogDownloadService
{
    class DownloadLog
    {
        public void downloadLog(object isGraphObj)
        {
            bool isGraph = (bool)isGraphObj;
            Log log = new Log();
            log.ErrorLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DownloadErroLog", "error");
            log.HistoryLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DownloadSpentTimeLog", "log");
            string tempLastFinished = isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"];
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
            List<string> dataTypestoExport = ConfigurationManager.AppSettings["DataTypestoExport"].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(StorageName, StorageKey);
                CloudStorageAccount account = new CloudStorageAccount(storageCredentials, true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Container);

                while (true)
                {
                    DateTime start2 = DateTime.UtcNow;
                    foreach (var ai in applicationInsights)
                    {
                        foreach (var dataType in dataTypestoExport)
                        {
                            DateTime indexDate = ExportStartDateTime;
                            log.LastFinished = string.Format(tempLastFinished, "LastFinished", dataType);
                            string lastFile = string.Empty;
                            if (File.Exists(log.LastFinished))
                                lastFile = File.ReadAllText(log.LastFinished);

                            if (!string.IsNullOrEmpty(lastFile))
                            {
                                lastFile = lastFile.Split('#')[2].Trim();
                                indexDate = Convert.ToDateTime(lastFile).ToUniversalTime();
                            }

                            DateTimeOffset lastFileTime = new DateTimeOffset(indexDate);
                            string prefix = ai + @"/" + dataType;                            
                          
                            var blobs = container.ListBlobs(prefix, useFlatBlobListing: true).OfType<CloudBlob>()
                                .Where(temp => temp.Properties.LastModified.Value.UtcDateTime == indexDate).ToList();;
                            if (blobs.Count >= 1)
                            {
                                indexDate = indexDate.AddMinutes(1);
                            }

                            prefix = ai + @"/" + dataType + @"/" + indexDate.ToString("yyyy-MM-dd") + '/' + indexDate.ToString("HH");
                            blobs = container.ListBlobs(prefix, useFlatBlobListing: true).OfType<CloudBlob>().
                                OrderBy(temp => temp.Properties.LastModified).
                                Where(temp => temp.Properties.LastModified.Value.UtcDateTime > lastFileTime).ToList();

                            while(blobs.Count == 0)
                            {
                                indexDate = Convert.ToDateTime(indexDate.ToString("yyyy-MM-dd HH:00:00"));
                                if (DateTime.UtcNow > indexDate)
                                {
                                    log.WriteDownloadLog("no files exits" + "#" + indexDate.ToString());
                                    indexDate = indexDate.AddHours(1);
                                }

                                if (DateTime.UtcNow > indexDate)
                                {
                                    prefix = ai + @"/" + dataType + @"/" + indexDate.ToString("yyyy-MM-dd") + '/' + indexDate.ToString("HH");
                                    blobs = container.ListBlobs(prefix, useFlatBlobListing: true).OfType<CloudBlob>().
                                        OrderBy(temp => temp.Properties.LastModified).
                                        Where(temp => temp.Properties.LastModified > lastFileTime).ToList();
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (blobs.Count > 0)
                            {
                                string outputFolder = Path.Combine(isGraph ? ConfigurationManager.AppSettings["OutputFolderForGraph"] : ConfigurationManager.AppSettings["OutputFolderForDev"], dataType, indexDate.ToString("yyyy-MM-dd"), indexDate.ToString("HH"));
                            
                                if (!Directory.Exists(outputFolder))
                                {
                                    Directory.CreateDirectory(outputFolder);
                                }

                                foreach (ICloudBlob blob in blobs)
                                {
                                    try
                                    {
                                        string tempFilePath = string.Format(@"{0}\{1}", outputFolder, Path.GetFileNameWithoutExtension(blob.Name));
                                        string filePath = Path.ChangeExtension(tempFilePath, ".blob");
                                        if (File.Exists(tempFilePath))
                                        {
                                            File.Delete(tempFilePath);
                                        }

                                        if (File.Exists(filePath))
                                        {
                                            File.Delete(filePath);
                                        }
                                        blob.DownloadToFile(tempFilePath, System.IO.FileMode.OpenOrCreate);
                                        File.Move(tempFilePath, filePath);
                                        File.SetCreationTimeUtc(filePath, blob.Properties.LastModified.Value.UtcDateTime);
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
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, string.Format("{0} stoped", isGraph ? "GraphPageViewAndOthersDownloadService" : "DevPageViewAndOthersDownloadService"));
                throw;
            }
            finally
            {

            }
        }
    }
}
