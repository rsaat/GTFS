using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Data;

namespace GTFS.Sptrans.Tool
{

    public class SqLiteDatabaseHelper
    {
        private static string connString;
        private static SQLiteConnection _connection;

        public SqLiteDatabaseHelper(SQLiteConnection connection)
        {
            connString = "";
            _connection = connection;
        }

        public DataTable GetDataTable(string sql)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = new SQLiteConnection(connString))
                {
                    conn.Open();
                    SQLiteCommand mycommand = new SQLiteCommand(conn);
                    mycommand.CommandText = sql;
                    SQLiteDataReader reader = mycommand.ExecuteReader();
                    dt.Load(reader);
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return dt;
        }

        public  DataSet GetDataSet(string sql)
        {
            DataSet ds = new DataSet();

            using (SQLiteCommand cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                {

                    da.Fill(ds);
                    return ds;
                }
            }

        }

        public int ExecuteNonQuery(string sql)
        {
                using (SQLiteCommand cmd = new SQLiteCommand(_connection))
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    int rowsUpdated = cmd.ExecuteNonQuery();
                    return rowsUpdated;

                }
            

        }

        public string ExecuteScalar(string sql)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    object value = cmd.ExecuteScalar();
                    if (value != null)
                    {
                        return value.ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
            }
        }


        public IDbTransaction BeginTransaction()
        {
             return _connection.BeginTransaction();
        }


    }
}

