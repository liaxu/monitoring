using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISLogDowload
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

        public bool ExecuteSqlBulkCopy(DataTable dt, string destinationTableName)
        {

            OpenConn();

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
            {

                bulkCopy.DestinationTableName = destinationTableName;



                try
                {

                    // Write from the source to the destination.

                    bulkCopy.BulkCopyTimeout = 30000000;

                    bulkCopy.WriteToServer(dt);
                }

                catch
                {

                    throw;

                }

                finally
                {



                }

            }

            CloseConn();

            return true;

        }
    }
}
