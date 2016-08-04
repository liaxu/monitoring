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

namespace GetErrorAndResponseMetrics
{
    class ParserAndInsertDB
    {

        bool isGraph = false;
        string _subscriptionId = string.Empty;
        string _tenantId = string.Empty;
        string _applicationId = string.Empty;
        string _applicationPwd = string.Empty;
        string _webAppResourceGroupName = string.Empty;
        string _webAppName = string.Empty;
        string _uriFormat = string.Empty;
        string _resourceUri = string.Empty;
        string _token = string.Empty;
        string _tableName = string.Empty;
        ServerResponseError serverResponseError;
        SQLCommon sqlCommon;
        public ParserAndInsertDB(bool isGraph)
        {
            this.isGraph = isGraph;
            this._subscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];
            this._tenantId = ConfigurationManager.AppSettings["AzureADTenantId"];
            this._applicationId = ConfigurationManager.AppSettings["AzureADApplicationId"];
            this._applicationPwd = ConfigurationManager.AppSettings["AzureADApplicationPassword"];
            this._webAppResourceGroupName = isGraph ? ConfigurationManager.AppSettings["GraphResourceGroupName"] : ConfigurationManager.AppSettings["DevResourceGroupName"];
            this._webAppName = isGraph ? ConfigurationManager.AppSettings["GraphWebAppName"] : ConfigurationManager.AppSettings["DevWebAppName"];
            this._uriFormat = ConfigurationManager.AppSettings["UriFormat"];
            this._resourceUri = string.Format(_uriFormat, _subscriptionId, _webAppResourceGroupName, _webAppName);
            this.serverResponseError = new ServerResponseError();           
            sqlCommon = new SQLCommon();
        }

        public void GetMetricsAndInsertDB(object isPerHourObj)
        {
            bool isPerHour = (bool)isPerHourObj;
            Log log = new Log();
            log.ErrorLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "InsertDBErrorLog", "error");
            //log.HistoryLog = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DBLog", "log");
            //log.LastFinished = string.Format(isGraph ? ConfigurationManager.AppSettings["LogPathForGraph"] : ConfigurationManager.AppSettings["LogPathForDev"], "DBLastFinishedLog", "log");
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
                        this._tableName = ConfigurationManager.AppSettings["GraphPerDay"];
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
                        this._tableName = ConfigurationManager.AppSettings["DevPerDay"];
                    }

                }
                #endregion

                _token = GetAccessToken();

                SQLCommon sqlCommon = new SQLCommon();
                QueryLastItemInDB();

                while (true)
                {
                    var creds = new TokenCloudCredentials(_subscriptionId, _token);
                    string filter = "";//"(name.value eq 'CpuPercentage' or name.value eq 'MemoryPercentage')";
                    DateTime timestamp = DateTime.UtcNow;

                    #region Initial timestamp
                    if (this.serverResponseError.TimeStamp > 0)
                    {
                        timestamp = UnixTimeStampToDateTime(this.serverResponseError.TimeStamp);
                    }
                    else
                    {
                        if (isPerHour)
                        {
                            timestamp = Convert.ToDateTime(DateTime.UtcNow.AddDays(-29).ToString("yyyy-MM-dd 00:00:00"));
                        }
                        else
                        {
                            timestamp = Convert.ToDateTime(DateTime.UtcNow.AddDays(-89).ToString("yyyy-MM-dd 00:00:00"));
                        }
                    }
                    #endregion
                    MetricListResponse metricList = null;
                    try
                    {
                        metricList = GetResourceMetrics(creds, _resourceUri, filter, timestamp, isPerHour);
                    }
                    catch(Exception e)
                    {
                        log.WriteErrorLog(this._resourceUri + "\r\nErrorMessage:" + e.Message + "\r\nStackTrace: " + e.StackTrace, true, isGraph ? "Graph GetErrorAndResponseMetrics Failed, but continue" : "Dev GetErrorAndResponseMetrics Failed, but continue");
                        Thread.Sleep(50000);
                        continue;
                    }

                    if (metricList != null && metricList.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Dictionary<int, ServerResponseError> list = ParserMetricValues(metricList);
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
                                if (item.TimeStamp > this.serverResponseError.TimeStamp)
                                {
                                    DataRow row = tempTable.NewRow();
                                    row["TimeStamp"] = item.TimeStamp;
                                    row["ResponseTime"] = item.ResponseTime;
                                    row["HTTP4XX"] = item.HTTP4XX;
                                    row["HTTP5XX"] = item.HTTP5XX;
                                    row["HTTP2XX"] = item.HTTP2XX;
                                    row["HTTP3XX"] = item.HTTP3XX;
                                    row["HTTP401"] = item.HTTP401;
                                    row["HTTP403"] = item.HTTP403;
                                    row["HTTP404"] = item.HTTP404;
                                    row["HTTP406"] = item.HTTP406;
                                    row["Requests"] = item.Requests;
                                    tempTable.Rows.Add(row);
                                    tempTable.AcceptChanges();
                                }
                                else if (item.TimeStamp == this.serverResponseError.TimeStamp)
                                {
                                    string excuteCmdText = string.Format(@"UPDATE [dbo].[{0}]
                                                              SET [ResponseTime] = {1}
                                                                ,[HTTP4XX] = {2}   
                                                                ,[HTTP5XX] = {3}   
                                                                ,[HTTP2XX] = {4} 
                                                                ,[HTTP3XX] = {5} 
                                                                ,[HTTP401] = {6} 
                                                                ,[HTTP403] = {7} 
                                                                ,[HTTP404] = {8} 
                                                                ,[HTTP406] = {9} 
                                                                ,[Requests] = {10}                                                                           
                                                              WHERE [TimeStamp] = {11}",
                                                              this._tableName, item.ResponseTime, item.HTTP4XX, item.HTTP5XX, item.HTTP2XX,
                                                              item.HTTP3XX,item.HTTP401,item.HTTP403,item.HTTP404,item.HTTP406,item.Requests,
                                                              this.serverResponseError.TimeStamp.ToString());
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

                            this.serverResponseError.TimeStamp = list.Keys.Max();
                            this.serverResponseError.ResponseTime = list[this.serverResponseError.TimeStamp].ResponseTime;
                            this.serverResponseError.HTTP4XX = list[this.serverResponseError.TimeStamp].HTTP4XX;
                            this.serverResponseError.HTTP5XX = list[this.serverResponseError.TimeStamp].HTTP5XX;
                            this.serverResponseError.HTTP2XX = list[this.serverResponseError.TimeStamp].HTTP2XX;
                            this.serverResponseError.HTTP3XX = list[this.serverResponseError.TimeStamp].HTTP3XX;
                            this.serverResponseError.HTTP401 = list[this.serverResponseError.TimeStamp].HTTP401;
                            this.serverResponseError.HTTP403 = list[this.serverResponseError.TimeStamp].HTTP403;
                            this.serverResponseError.HTTP404 = list[this.serverResponseError.TimeStamp].HTTP404;
                            this.serverResponseError.HTTP406 = list[this.serverResponseError.TimeStamp].HTTP406;
                            this.serverResponseError.Requests = list[this.serverResponseError.TimeStamp].Requests;

                            //log.WriteDownloadLog(
                            //    UnixTimeStampToDateTime(this.serverResponseError.TimeStamp).ToShortTimeString() + "," + 
                            //    this.serverResponseError.ResponseTime + "," + 
                            //    this.serverResponseError.HTTP4XX + ","+
                            //    this.serverResponseError.HTTP5XX + "," +
                            //    this.serverResponseError.HTTP2XX + "," +
                            //    this.serverResponseError.HTTP3XX + "," +
                            //    this.serverResponseError.HTTP401 + "," +
                            //    this.serverResponseError.HTTP403 + "," +
                            //    this.serverResponseError.HTTP404 + "," +
                            //    this.serverResponseError.HTTP406 + "," +
                            //    this.serverResponseError.Requests + "," +
                            //    DateTime.UtcNow.ToString() + "," + 
                            //    (DateTime.UtcNow - UnixTimeStampToDateTime(this.serverResponseError.TimeStamp)).ToString());
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
                log.WriteErrorLog(this._resourceUri+"\r\nErrorMessage:"+e.Message + "\r\nStackTrace: " + e.StackTrace, true, isGraph ? "Graph GetErrorAndResponseMetrics Failed" : "Dev GetErrorAndResponseMetrics Failed");
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
                        this.serverResponseError.TimeStamp = Convert.ToInt32(item["TimeStamp"]);
                        this.serverResponseError.ResponseTime = Convert.ToInt32(item["ResponseTime"]);
                        this.serverResponseError.HTTP4XX = Convert.ToInt32(item["HTTP4XX"]);
                        this.serverResponseError.HTTP5XX = Convert.ToInt32(item["HTTP5XX"]);
                        this.serverResponseError.HTTP2XX = Convert.ToInt32(item["HTTP2XX"]);
                        this.serverResponseError.HTTP3XX = Convert.ToInt32(item["HTTP3XX"]);
                        this.serverResponseError.HTTP401 = Convert.ToInt32(item["HTTP401"]);
                        this.serverResponseError.HTTP403 = Convert.ToInt32(item["HTTP403"]);
                        this.serverResponseError.HTTP404 = Convert.ToInt32(item["HTTP404"]);
                        this.serverResponseError.HTTP406 = Convert.ToInt32(item["HTTP406"]);
                        this.serverResponseError.Requests = Convert.ToInt32(item["Requests"]);
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

            DataColumn responseTime = new DataColumn();
            responseTime.DataType = System.Type.GetType("System.Int32");
            responseTime.ColumnName = "ResponseTime";
            dt.Columns.Add(responseTime);

            DataColumn count4XX = new DataColumn();
            count4XX.DataType = System.Type.GetType("System.Int32");
            count4XX.ColumnName = "HTTP4XX";
            dt.Columns.Add(count4XX);

            DataColumn count5XX = new DataColumn();
            count5XX.DataType = System.Type.GetType("System.Int32");
            count5XX.ColumnName = "HTTP5XX";
            dt.Columns.Add(count5XX);

            DataColumn http406 = new DataColumn();
            http406.DataType = System.Type.GetType("System.Int32");
            http406.ColumnName = "HTTP406";
            dt.Columns.Add(http406);

            DataColumn http404 = new DataColumn();
            http404.DataType = System.Type.GetType("System.Int32");
            http404.ColumnName = "HTTP404";
            dt.Columns.Add(http404);

            DataColumn http403 = new DataColumn();
            http403.DataType = System.Type.GetType("System.Int32");
            http403.ColumnName = "HTTP403";
            dt.Columns.Add(http403);

            DataColumn http401 = new DataColumn();
            http401.DataType = System.Type.GetType("System.Int32");
            http401.ColumnName = "HTTP401";
            dt.Columns.Add(http401);

            DataColumn http2XX = new DataColumn();
            http2XX.DataType = System.Type.GetType("System.Int32");
            http2XX.ColumnName = "HTTP2XX";
            dt.Columns.Add(http2XX);

            DataColumn http3XX = new DataColumn();
            http3XX.DataType = System.Type.GetType("System.Int32");
            http3XX.ColumnName = "HTTP3XX";
            dt.Columns.Add(http3XX);

            DataColumn requests = new DataColumn();
            requests.DataType = System.Type.GetType("System.Int32");
            requests.ColumnName = "Requests";
            dt.Columns.Add(requests);       
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

        private Dictionary<int, ServerResponseError> ParserMetricValues(MetricListResponse metricList)
        {
            Dictionary<int, ServerResponseError> list = new Dictionary<int, ServerResponseError>();
            foreach (Metric m in metricList.MetricCollection.Value)
            {
                if(m.Name.Value.Equals("CpuTime")||
                    m.Name.Value.Equals("BytesReceived")||
                    m.Name.Value.Equals("BytesSent")||
                    m.Name.Value.Equals("MemoryWoringSet")||
                    m.Name.Value.Equals("AverageMemoryWorkingSet"))
                {
                    continue;
                }

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
                                case "AverageResponseTime":
                                    list[timestamp].ResponseTime =
                                        Convert.ToInt32(Math.Round(metricValue.Average.Value * 1000, MidpointRounding.AwayFromZero));
                                    break;
                                case "Http4xx":
                                    list[timestamp].HTTP4XX = temp;
                                    break;
                                case "Http5xx":
                                    list[timestamp].HTTP5XX = temp;
                                    break;
                                case "Http2xx":
                                    list[timestamp].HTTP2XX = temp;
                                    break;
                                case "Http3xx":
                                    list[timestamp].HTTP3XX = temp;
                                    break;
                                case "Http401":
                                    list[timestamp].HTTP401 = temp;
                                    break;
                                case "Http403":
                                    list[timestamp].HTTP403 = temp;
                                    break;
                                case "Http404":
                                    list[timestamp].HTTP404 = temp;
                                    break;
                                case "Http406":
                                    list[timestamp].HTTP406 = temp;
                                    break;
                                case "Requests":
                                    list[timestamp].Requests = temp;
                                    break;
                            }
                        }
                        else
                        {
                            ServerResponseError tempInstance = new ServerResponseError();
                            tempInstance.TimeStamp = timestamp;

                            switch (m.Name.Value)
                            {
                                case "AverageResponseTime":
                                    tempInstance.ResponseTime = 
                                        Convert.ToInt32(Math.Round(metricValue.Average.Value*1000, MidpointRounding.AwayFromZero));
                                    break;
                                case "Http4xx":
                                    tempInstance.HTTP4XX = temp;
                                    break;
                                case "Http5xx":
                                    tempInstance.HTTP5XX = temp;
                                    break;
                                case "Http2xx":
                                    tempInstance.HTTP2XX = temp;
                                    break;
                                case "Http3xx":
                                    tempInstance.HTTP3XX = temp;
                                    break;
                                case "Http401":
                                    tempInstance.HTTP401 = temp;
                                    break;
                                case "Http403":
                                    tempInstance.HTTP403 = temp;
                                    break;
                                case "Http404":
                                    tempInstance.HTTP404 = temp;
                                    break;
                                case "Http406":
                                    tempInstance.HTTP406 = temp;
                                    break;
                                case "Requests":
                                    tempInstance.Requests = temp;
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

        private MetricListResponse GetResourceMetrics(TokenCloudCredentials credentials, string resourceUri, string filter, DateTime startTime, bool isPerHour)
        {
            string duration = isPerHour ? "PT1H" : "P1D";
            var dateTimeFormat = "yyyy-MM-ddTHH:mmZ";
            string start = startTime.ToString(dateTimeFormat);

            string end = DateTime.UtcNow.ToString(dateTimeFormat);

            if(isPerHour)
            {
                if ((DateTime.UtcNow - startTime).Days >= 30)
                {
                    start = DateTime.UtcNow.AddDays(-29).ToString(dateTimeFormat);             
                }
            }
            else
            {
                if((DateTime.UtcNow-startTime).Days>=90)
                {
                    start = DateTime.UtcNow.AddDays(-89).ToString(dateTimeFormat);                    
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

        private MetricDefinitionListResponse GetAvailableMetricDefinitions(TokenCloudCredentials credentials, string resourceUri)
        {
            using (var client = new InsightsClient(credentials))
            {
                return client.MetricDefinitionOperations.GetMetricDefinitions(resourceUri, null);
            }
        }
    }
}
