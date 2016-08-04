
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;

namespace IISLogInsertService
{
    public class SQLCommon
    {
        static readonly string connStr = ConfigurationManager.AppSettings["ConnStr"];
        public SqlConnection conn = null;

        public SQLCommon()
        {
            conn = new SqlConnection(connStr);
        }

        public void OpenConn()
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
        }

        public void CloseConn()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }

        public bool CreateTable(string tableName)
        {
            OpenConn();
            string cmdText = string.Format(@" 
                       IF OBJECT_ID('{0}', 'U') IS NOT NULL
                           TRUNCATE TABLE {0}; 
                       ELSE                             
                        CREATE TABLE [dbo].{0}(
	                        [Id] [int] IDENTITY(1,1) NOT NULL,
	                        [TimeStamp] [int] NOT NULL,
	                        [URI] [varchar](1000) NULL,
	                        [HOST] [varchar](100) NULL,
	                        [Referer] [nvarchar](1000) NULL,
	                        [IP] [varchar](50) NULL,
	                        [ai_session] [varchar](100) NULL,
	                        [ai_user] [varchar](100) NULL,
	                        [status] [varchar](10) NULL,
	                        [time_taken] [int] NULL,
                            CONSTRAINT [PK_{0}_id] PRIMARY KEY CLUSTERED 
                        (
	                        [Id] ASC
                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
                        )", tableName);
            using (SqlCommand cmd = new SqlCommand(cmdText, conn))
            {
                cmd.CommandTimeout = 1800;
                try
                {
                    int result = cmd.ExecuteNonQuery();

                    CloseConn();
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

        public bool ExecuteSqlBulkCopy(DataTable dt, string destinationTableName)
        {
            OpenConn();
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
            {
                bulkCopy.DestinationTableName = destinationTableName;

                try
                {
                    // Write from the source to the destination.
                    bulkCopy.BulkCopyTimeout = 1800;
                    bulkCopy.WriteToServer(dt);

                    CloseConn();
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
            OpenConn();
            id = 0;
            alertTime = DateTime.MinValue;

            try
            {
                using (SqlCommand cmd = new SqlCommand(queryString, conn))
                {
                    cmd.CommandTimeout = 1800;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            id = Convert.ToInt32(rdr[0]);
                            alertTime = new DateTime(Convert.ToInt64(rdr[1]));

                            CloseConn();
                            return true;
                        }
                    }
                }
                CloseConn();
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
            OpenConn();
            DataTable dt = new DataTable();
            result = -1;
            try
            {
                using (SqlCommand cmd = new SqlCommand(queryString, conn))
                {
                    cmd.CommandTimeout = 1800;
                    using (SqlDataAdapter da = new SqlDataAdapter(queryString, conn))
                    {
                        SqlCommandBuilder cb = new SqlCommandBuilder(da);
                        result = da.Fill(dt);
                    }
                }
                CloseConn();
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
            OpenConn();
            result = -1;
            try
            {
                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    cmd.CommandTimeout = 1800;
                    foreach (var item in list)
                    {
                        cmd.Parameters.Add(item);
                    }

                    result = (Int32)cmd.ExecuteScalar();
                }
                
                CloseConn();
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
            OpenConn();
            try
            {
                int result = -1;

                using (SqlCommand cmd = new SqlCommand(commandText, conn))
                {
                    cmd.CommandTimeout = 1800;
                    result = cmd.ExecuteNonQuery();
                    cmd.ResetCommandTimeout();
                }

                CloseConn();
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
    }
}