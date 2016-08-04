using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;

namespace GetServicePlanMetrics
{
    public class SQLCommon
    {
        static readonly string connStr = ConfigurationManager.AppSettings["ConnStr"];
        public SqlConnection conn = null;

        public SQLCommon()
        {
            conn = new SqlConnection(connStr);
        }

        public bool ExecuteSqlBulkCopy(DataTable dt, string destinationTableName)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
            {
                bulkCopy.DestinationTableName = destinationTableName;

                try
                {
                    // Write from the source to the destination.
                    bulkCopy.BulkCopyTimeout = 30000000;
                    bulkCopy.WriteToServer(dt);
                   
                    return true;
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
        public bool Query(string queryString, out int id, out DateTime alertTime)
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
            catch
            {
                throw;
            }
            finally
            {

            }
        }

        public DataTable Query(string queryString, out int result)
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
            catch
            {
                throw;
            }
            finally
            {

            }
        }

        public bool ExecuteQuery(string commandText, out int result, params object[] list)
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

                    result = (Int32)cmd.ExecuteScalar();
                }
                if (result >= 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                throw;
            }
            finally
            {

            }
        }

        public int ExecuteQuery(string commandText)
        {
            try
            {
                int result = -1;

                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    cmd.CommandTimeout = 30000000;
                    result = cmd.ExecuteNonQuery();
                    cmd.ResetCommandTimeout();
                }
                return result;
            }
            catch
            {
                throw;
            }
            finally
            {

            }
        }

        public string ConvertToJSON(DataTable dt)
        {
            string JSONresult;
            JSONresult = JsonConvert.SerializeObject(dt);
            return JSONresult;
        }
    }
}