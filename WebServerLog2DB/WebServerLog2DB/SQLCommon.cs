using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;

namespace WebServerLog2DB
{
    public static class SQLCommon
    {
        static string connStr = ConfigurationManager.AppSettings["ConnStr"];
        public static SqlConnection conn = new SqlConnection(connStr);

        public static bool ExecuteSqlBulkCopy(DataTable dt, string destinationTableName)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
            {
                bulkCopy.DestinationTableName = destinationTableName;

                try
                {
                    bulkCopy.BulkCopyTimeout = 3000;
                    // Write from the source to the destination.
                    bulkCopy.WriteToServer(dt);
                    
                    return true;
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
        public static bool Query(string queryString, out int id, out DateTime alertTime)
        {
            id = 0;
            alertTime = DateTime.MinValue;

            try
            {
                using (SqlCommand cmd = new SqlCommand(queryString, conn))
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            id = Convert.ToInt32(rdr[0]);
                            alertTime = new DateTime(Convert.ToInt64(rdr[1]));

                            return true;
                        }
                    }
                }

                return false;
            }
            catch (System.Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }

        public static DataTable Query(string queryString, out int result)
        {
            DataTable dt = new DataTable();
            result = -1;
            try
            {
                using (SqlCommand cmd = new SqlCommand(queryString, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(queryString, conn))
                    {
                        SqlCommandBuilder cb = new SqlCommandBuilder(da);
                        result = da.Fill(dt);
                    }
                }
                return dt;
            }
            catch (System.Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }

        public static bool ExecuteQuery(string commandText, out int result, params object[] list)
        {
            result = -1;
            try
            {
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    foreach (var item in list)
                    {
                        cmd.Parameters.Add(item);
                    }

                    cmd.CommandTimeout = 3000;
                    result = (Int32)cmd.ExecuteScalar();
                    cmd.ResetCommandTimeout();
                }
                if (result >= 0)
                {
                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }

        public static int ExecuteQuery(string commandText)
        {
            try
            {
                int result = -1;

                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    cmd.CommandTimeout = 3000000;
                    result = cmd.ExecuteNonQuery();
                    cmd.ResetCommandTimeout();
                }

                return result;
            }
            catch (System.Exception e)
            {
                Log.WriteLog(e.Message + "\r\nStackTrace: " + e.StackTrace);
                throw;
            }
            finally
            {

            }
        }

        public static string ConvertToJSON(DataTable dt)
        {
            string JSONresult;
            JSONresult = JsonConvert.SerializeObject(dt);
            return JSONresult;
        }
    }
}