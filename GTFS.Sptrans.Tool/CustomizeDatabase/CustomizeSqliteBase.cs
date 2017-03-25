using System.Data;
using Gtfs2Sqlite.Util;

namespace GTFS.Sptrans.Tool.CustomizeDatabase
{
    public class CustomizeSqliteBase
    {
        protected readonly SqLiteDatabaseHelper _sqliteHelper;
        protected Geolocation _geoLocation;

        public CustomizeSqliteBase(SqLiteDatabaseHelper sqliteHelper)
        {
            _sqliteHelper = sqliteHelper;
            _geoLocation = new Geolocation();
        }

        protected  DataSet GetAllTripsStopPointsWeekendSplited()
        {
            var sql = @"
            SELECT 
            t.id,
            t.shape_id,
            st.stop_id,
            st.stop_sequence,
            st.shape_dist_traveled,
            st.departure_time,
            s.stop_lat,
            s.stop_lon,
            s.stop_name 
            from  stop_time st 
            INNER JOIN trip t ON t.id_stop_time = st.trip_id 
            INNER JOIN stop s ON s.id = st.stop_id 
            {0}
            ORDER BY t.id, st.stop_sequence";

            if (false)
            {

                //sql = string.Format(sql, string.Format(@"WHERE   t.id=""{0}"" ", _testTripId));
            }
            else
            {
                sql = string.Format(sql, "");
            }

            return _sqliteHelper.GetDataSet(sql);

        }

        protected DataTable GetFrequencyDataTable(string tripId)
        {
            var sql = "SELECT * FROM frequency WHERE (trip_id='" + tripId + "')";
            var dtTrips = _sqliteHelper.GetDataTable(sql);
            return dtTrips;
        }

        protected void UpdateFrequencyDataTable(DataTable dtFrequency)
        {
            var insertCommand = _sqliteHelper.CreateInsertComand("frequency");
            _sqliteHelper.UpdateDataTable(null, insertCommand, dtFrequency);
        }


        protected DataRow CopyAsNewRow(DataRow drSource)
        {
            var drNew = drSource.Table.NewRow();
            foreach (DataColumn column in drSource.Table.Columns)
            {
                drNew[column.ColumnName] = drSource[column.ColumnName];
            }
            return drNew;
        }
    }
}
