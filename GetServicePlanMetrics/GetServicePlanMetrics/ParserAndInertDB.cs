using System;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Azure;
using Microsoft.Azure.Insights.Models;
using Microsoft.Azure.Insights;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Data;
using Hyak.Common;

namespace GetServicePlanMetrics
{
    class ParserAndInsertDB
    {

        bool isGraph = false;
        string _subscriptionId = string.Empty;
        string _tenantId = string.Empty;
        string _applicationId = string.Empty;
        string _applicationPwd = string.Empty;
        string _webAppResourceGroupName = string.Empty;
        string _servicePlanName = string.Empty;
        string _uriFormat = string.Empty;
        string _resourceUri = string.Empty;
        string _token = string.Empty;
        string _tableName = string.Empty;
        CPUandMemory cpuandmemory;
        SQLCommon sqlCommon;
        public ParserAndInsertDB(bool isGraph)
        {
            this.isGraph = isGraph;
            this._subscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];
            this._tenantId = ConfigurationManager.AppSettings["AzureADTenantId"];
            this._applicationId = ConfigurationManager.AppSettings["AzureADApplicationId"];
            this._applicationPwd = ConfigurationManager.AppSettings["AzureADApplicationPassword"];
            this._webAppResourceGroupName = isGraph ? ConfigurationManager.AppSettings["GraphResourceGroupName"] : ConfigurationManager.AppSettings["DevResourceGroupName"];
            this._servicePlanName = isGraph ? ConfigurationManager.AppSettings["GraphServicePlanName"] : ConfigurationManager.AppSettings["DevServicePlanName"];
            this._uriFormat = ConfigurationManager.AppSettings["UriFormat"];
            this._resourceUri = string.Format(_uriFormat, _subscriptionId, _webAppResourceGroupName, _servicePlanName);
            this.cpuandmemory = new CPUandMemory();           
            sqlCommon = new SQLCommon();
        }

        public void GetMetricsAndInsertDB(object isPerHourObj)
        {
            bool isPerHour = (bool)isPerHourObj;
            Log log = new Log();
            log.ErrorLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "InsertDBErrorLog", "error");
            try
            {
                #region Initial tablename
                if (isGraph)
                {
                    if (isPerHour)
                    {
                        this._tableName = ConfigurationManager.AppSettings["GraphPerHour"];
                    }
                    else
                    {
                        this._tableName = ConfigurationManager.AppSettings["GraphPerMinute"];
                    }
                }
                else
                {
                    if (isPerHour)
                    {
                        this._tableName = ConfigurationManager.AppSettings["DevPerHour"];
                    }
                    else
                    {
                        this._tableName = ConfigurationManager.AppSettings["DevPerMinute"];
                    }
                } 
                #endregion

                _token = GetAccessToken();

                SQLCommon sqlCommon = new SQLCommon();
                QueryLastItemInDB();

                while (true)
                {
                    string filter = "(name.value eq 'CpuPercentage' or name.value eq 'MemoryPercentage')";

                    DateTime timestamp = DateTime.UtcNow;

                    #region Initial timestamp
                    if (this.cpuandmemory.TimeStamp > 0)
                    {
                        timestamp = UnixTimeStampToDateTime(this.cpuandmemory.TimeStamp);
                    }
                    else
                    {
                        if (isPerHour)
                        {
                            timestamp = Convert.ToDateTime(DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd HH:00:00"));//7 days
                        }
                        else
                        {
                            timestamp = DateTime.UtcNow.AddMinutes(-1020);//max is 1024
                        }
                    } 
                    #endregion
                    MetricListResponse metricList = null;
                    try
                    {
                        var creds = new TokenCloudCredentials(_subscriptionId, _token);
                        metricList = GetResourceMetrics(creds, _resourceUri, filter, timestamp, isPerHour);
                    }
                    catch(Exception e)
                    {
                        log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, isGraph ? "Graph GetServicePlanMetrics Failed, but continue" : "Dev GetServicePlanMetrics Failed, but continue");
                    }
                    if (metricList != null && metricList.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Dictionary<int, CPUandMemory> list = ParserMetricValues(metricList);
                        if (list.Count > 0)
                        {
                            log.LastFinished = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DBLastFinishedLog", isPerHour ? "perHourlog" : "perMinutelog");
                            
                            #region Insert or update DB
                            DataTable tempTable = MakeTable();
                            if (sqlCommon.conn.State == ConnectionState.Closed)
                            {
                                sqlCommon.conn.Open();
                            }

                            foreach (var item in list.Values)
                            {
                                if (item.TimeStamp > this.cpuandmemory.TimeStamp)
                                {
                                    DataRow row = tempTable.NewRow();
                                    row["TimeStamp"] = item.TimeStamp;
                                    row["CPUPercentage"] = item.CPUPercentage;
                                    row["MemoryPercentage"] = item.MemoryPercentage;

                                    tempTable.Rows.Add(row);
                                    tempTable.AcceptChanges();
                                }
                                else if (item.TimeStamp == this.cpuandmemory.TimeStamp)
                                {
                                    string excuteCmdText = string.Format(@"UPDATE [dbo].[{0}]
                                                              SET [CPUPercentage] = {1}
                                                              ,[MemoryPercentage] = {2}                                                                              
                                                              WHERE [TimeStamp] = {3}", this._tableName, item.CPUPercentage, item.MemoryPercentage, this.cpuandmemory.TimeStamp.ToString());
                                    sqlCommon.ExecuteQuery(excuteCmdText);

                                }
                            }

                            if (tempTable.Rows.Count > 0)
                            {
                                bool executeSqlBulkCopyResult = sqlCommon.ExecuteSqlBulkCopy(tempTable, string.Format("dbo.[{0}]", this._tableName));
                                if (executeSqlBulkCopyResult)
                                {
                                    tempTable.Rows.Clear();
                                }
                            } 
                            #endregion

                            this.cpuandmemory.TimeStamp = list.Keys.Max();
                            this.cpuandmemory.CPUPercentage = list[this.cpuandmemory.TimeStamp].CPUPercentage;
                            this.cpuandmemory.MemoryPercentage = list[this.cpuandmemory.TimeStamp].MemoryPercentage;

                           // log.WriteDownloadLog(UnixTimeStampToDateTime(this.cpuandmemory.TimeStamp).ToShortTimeString() + "," + this.cpuandmemory.CPUPercentage + "," + this.cpuandmemory.MemoryPercentage + ","
                           //     + DateTime.UtcNow.ToString() + "," + (DateTime.UtcNow - UnixTimeStampToDateTime(this.cpuandmemory.TimeStamp)).ToString());
                        }
                    }
                    if (sqlCommon.conn.State == ConnectionState.Open)
                    {
                        sqlCommon.conn.Close();
                    }
                    Thread.Sleep(50000);
                }
            }            
            catch (Exception e)
            {
                log.WriteErrorLog(e.Message + "\r\nStackTrace: " + e.StackTrace, true, isGraph ? "Graph GetServicePlanMetrics Failed" : "Dev GetServicePlanMetrics Failed");
                throw;
            }
            finally
            {

            }
        }
        public void QueryLastItemInDB()
        {
            try
            {
                if (sqlCommon.conn.State == ConnectionState.Closed)
                {
                    sqlCommon.conn.Open();
                }

                string cmdText = string.Format("Select top 1 * from dbo.{0} order by TimeStamp desc", this._tableName);

                int result;
                DataTable dt = sqlCommon.Query(cmdText, out result);

                if (result >= 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        this.cpuandmemory.TimeStamp = Convert.ToInt32(item["TimeStamp"]);
                        this.cpuandmemory.MemoryPercentage = Convert.ToInt32(item["MemoryPercentage"]);
                        this.cpuandmemory.CPUPercentage = Convert.ToInt32(item["CPUPercentage"]);
                        break;
                    }
                }

                if (sqlCommon.conn.State == ConnectionState.Open)
                {
                    sqlCommon.conn.Close();
                }
            }
            catch
            {
                throw;
            }
            finally
            {

            }
        }
        public DataTable MakeTable()
        {
            DataTable dt = new DataTable(this._tableName);

            DataColumn timestamp = new DataColumn();
            timestamp.DataType = System.Type.GetType("System.Int32");
            timestamp.ColumnName = "TimeStamp";
            dt.Columns.Add(timestamp);

            DataColumn cpuPercentage = new DataColumn();
            cpuPercentage.DataType = System.Type.GetType("System.Int32");
            cpuPercentage.ColumnName = "CPUPercentage";
            dt.Columns.Add(cpuPercentage);

            DataColumn memoryPercentage = new DataColumn();
            memoryPercentage.DataType = System.Type.GetType("System.Int32");
            memoryPercentage.ColumnName = "MemoryPercentage";
            dt.Columns.Add(memoryPercentage);
       
            return dt;
        }

        public DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public int DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt32((TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds);
        }

        private string GetAccessToken()
        {
            var authenticationContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", this._tenantId));
            var credential = new ClientCredential(clientId: _applicationId, clientSecret: _applicationPwd);
            var result = authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            string token = result.Result.AccessToken;

            return token;
        }

        private Dictionary<int, CPUandMemory> ParserMetricValues(MetricListResponse metricList)
        {
            Dictionary<int, CPUandMemory> list = new Dictionary<int, CPUandMemory>();
            foreach (Metric m in metricList.MetricCollection.Value)
            {
                foreach (MetricValue metricValue in m.MetricValues)
                {
                    int timestamp = DateTimeToUnixTimestamp(metricValue.Timestamp);

                    if (metricValue.Average.HasValue)
                    {
                        int temp = Convert.ToInt32(Math.Round(metricValue.Average.Value, MidpointRounding.AwayFromZero));
                        if (list.ContainsKey(timestamp))
                        {
                            switch (m.Name.Value)
                            {
                                case "CpuPercentage":
                                    list[timestamp].CPUPercentage = temp;
                                    break;
                                case "MemoryPercentage":
                                    list[timestamp].MemoryPercentage = temp;
                                    break;
                            }
                        }
                        else
                        {
                            CPUandMemory tempInstance = new CPUandMemory();
                            tempInstance.TimeStamp = timestamp;

                            switch (m.Name.Value)
                            {
                                case "CpuPercentage":
                                    tempInstance.CPUPercentage = temp;
                                    break;
                                case "MemoryPercentage":
                                    tempInstance.MemoryPercentage = temp;
                                    break;
                            }

                            list.Add(timestamp, tempInstance);
                        }
                    }
                }
            }

            return list;
        }

        private void PrintMetricDefinitions(MetricDefinitionListResponse definitions)
        {
            foreach (var d in definitions.MetricDefinitionCollection.Value)
            {
                Console.WriteLine("Metric: {0}", d.Name.Value);

                Console.WriteLine("    Time Grains");
                foreach (var x in d.MetricAvailabilities)
                {
                    Console.WriteLine("        {0}", x.TimeGrain);
                }

                Console.WriteLine();
            }
        }

        private MetricListResponse GetResourceMetrics(TokenCloudCredentials credentials, string resourceUri, string filter, TimeSpan period, string duration)
        {
            var dateTimeFormat = "yyyy-MM-ddTHH:mmZ";

            string start = DateTime.UtcNow.Subtract(period).ToString("yyyy-MM-ddT00:00Z");
            string end = DateTime.UtcNow.ToString(dateTimeFormat);

            // TODO: Make this more robust.
            StringBuilder sb = new StringBuilder(filter);

            if (!string.IsNullOrEmpty(filter))
            {
                sb.Append(" and ");
            }
            sb.AppendFormat("startTime eq {0} and endTime eq {1}", start, end);
            sb.AppendFormat(" and timeGrain eq duration'{0}'", duration);
            using (var client = new InsightsClient(credentials))
            {
                try
                {
                    return client.MetricOperations.GetMetrics(resourceUri, sb.ToString());
                }
                catch (CloudException e)
                {
                    if (e.Error.Code.Contains("Authentication"))
                    {
                        _token = GetAccessToken();
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                }

            }
        }

        private MetricListResponse GetResourceMetrics(TokenCloudCredentials credentials, string resourceUri, string filter, DateTime startTime, bool isPerHour)
        {
            string duration = isPerHour ? "PT1H" : "PT1M";
            var dateTimeFormat = "yyyy-MM-ddTHH:mmZ";
            string start = startTime.ToString(dateTimeFormat);

            string end = DateTime.UtcNow.ToString(dateTimeFormat);

            if(isPerHour)
            {
                if ((DateTime.UtcNow - startTime).Days >= 7)
                {
                    end = startTime.AddDays(7).ToString(dateTimeFormat);
                }
            }
            else
            {
                if((DateTime.UtcNow-startTime).Hours>=17)
                {
                    start = DateTime.UtcNow.AddHours(-17).ToString(dateTimeFormat);                    
                }
            }

            // TODO: Make this more robust.
            StringBuilder sb = new StringBuilder(filter);

            if (!string.IsNullOrEmpty(filter))
            {
                sb.Append(" and ");
            }
            sb.AppendFormat("startTime eq {0} and endTime eq {1}", start, end);
            sb.AppendFormat(" and timeGrain eq duration'{0}'", duration);
            using (var client = new InsightsClient(credentials))
            {
                try
                {
                    return client.MetricOperations.GetMetrics(resourceUri, sb.ToString());
                }
                catch (CloudException e)
                {
                    if (e.Error.Code.Contains("Authentication"))
                    {
                        _token = GetAccessToken();
                    }

                    return null;
                }
                catch
                {
                    throw;
                }
                finally
                {
                }
            }
        }

        private MetricDefinitionListResponse GetAvailableMetricDefinitions(TokenCloudCredentials credentials, string resourceUri)
        {
            using (var client = new InsightsClient(credentials))
            {
                return client.MetricDefinitionOperations.GetMetricDefinitions(resourceUri, null);
            }
        }
    }
}
