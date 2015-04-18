using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
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

            var conn = _connection;
            SQLiteCommand mycommand = new SQLiteCommand(conn);
            mycommand.CommandText = sql;
            SQLiteDataReader reader = mycommand.ExecuteReader();
            dt.Load(reader);
            reader.Close();
            return dt;
        }


        public SQLiteCommand CreateUpdateComand(string tableName , string indexColumnName)
        {
            var columnNames = GetColumnNames(tableName);

            var sqlUpdate = "UPDATE " + tableName + " SET {0}  WHERE {1} ";

            var sqlColumnsSet = "";
            for (int i = 0; i < columnNames.Count; i++)
            {
                if (sqlColumnsSet!="")
                {
                    sqlColumnsSet += " , "; 
                }

                sqlColumnsSet += columnNames[i] + "= :" + columnNames[i];

            }

            var sql = string.Format(sqlUpdate, sqlColumnsSet, indexColumnName + "=:" + indexColumnName + "1");

            var updateComand = new SQLiteCommand(sql);

            foreach (var columnName in columnNames)
            {
                updateComand.Parameters.Add(new SQLiteParameter(columnName, DbType.String, 1000,  columnName));
            }

            var indexParameter = new SQLiteParameter(indexColumnName + "1", DbType.String, 1000, indexColumnName);
            
            updateComand.Parameters.Add(indexParameter);

            return updateComand;

        }
        
        public SQLiteCommand CreateInsertComand(string tableName)
        {
            var columnNames = GetColumnNames(tableName);

            var columnsNames = CreateColumnList(columnNames,false);
            var columnsValues = CreateColumnList(columnNames,true);


            const string sqlInsert = @"INSERT INTO {0} ({1})
                                       VALUES ({2})";

            var sql = string.Format(sqlInsert,tableName, columnsNames, columnsValues);

            var insertComand = new SQLiteCommand(sql);

            foreach (var columnName in columnNames)
            {
                insertComand.Parameters.Add(new SQLiteParameter(columnName, DbType.String, 1000, columnName));  
            }

            return insertComand;

        }

        private string CreateColumnList(IEnumerable<string> columnNames , bool isParameterList)
        {
            var columnsText = "";

            foreach (var columnName in columnNames)
            {
                if (columnsText != "")
                {
                    columnsText += " , ";
                }

                if (isParameterList)
                {
                    columnsText += ":";
                }

                columnsText += columnName;
            }
            return columnsText;
        }

        private List<string> GetColumnNames(string tableName)
        {
            var sql = "SELECT * FROM " + tableName + " WHERE (1=0)";
            var columnNames = new List<string>();
            foreach (DataColumn col in GetDataTable(sql).Columns)
            {
                columnNames.Add(col.ColumnName);
            }
            return columnNames;
        }

        



        public int UpdateDataTable(SQLiteCommand updateCommand, DataTable dt)
        {
            return UpdateDataTable(updateCommand, null, dt);
        }

        public int UpdateDataTable(SQLiteCommand updateCommand, SQLiteCommand insertCommand, DataTable dt)
        {
            int rowsUpdated = 0;

               
                using (var da = new SQLiteDataAdapter())

                {
                    if (updateCommand != null)
                    {
                        updateCommand.Connection = _connection;
                        da.UpdateCommand = updateCommand;
                    }

                    if (insertCommand!=null)
                    {
                        insertCommand.Connection = _connection;
                        da.InsertCommand = insertCommand;     
                    }
                    rowsUpdated = da.Update(dt);
                }
            

            return rowsUpdated; 

        }


        public DataSet GetDataSet(string sql)
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

        public int ExecuteNonQuery(string sql, SQLiteParameter[] parameters )
        {
            using (SQLiteCommand cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddRange(parameters);
                int rowsUpdated = cmd.ExecuteNonQuery();
                return rowsUpdated;

            }
        }

        public SQLiteParameter CreateParameter(string name, DbType dbType, object value)
        {
            var param = new SQLiteParameter(name, dbType);
            param.Value = value;

            return param;

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

