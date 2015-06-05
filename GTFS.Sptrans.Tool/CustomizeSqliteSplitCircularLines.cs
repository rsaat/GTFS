using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS.Sptrans.Tool
{
    public class CustomizeSqliteSplitCircularLines : CustomizeSqliteBase
    {
        private Dictionary<string, DataRow> _tripAndRouteNames;

        public CustomizeSqliteSplitCircularLines(SqLiteDatabaseHelper sqliteHelper)
            : base(sqliteHelper)
        {

        }


        public void ExecuteCustomizations()
        {
            var circularTrips = GetAllCircularTrips();

            var dtTrips = GetBusOnlyTripDataTable();


            var qTripCircular = dtTrips.AsEnumerable().Where(dr => circularTrips.Contains(dr["id"].ToString().ToUpper()));

            var circularTripsRows = qTripCircular.ToList();

            LoadTripAndRouteNames();

            var endWeekTrip = "-0";
            var endSaturdayTrip = "-0-S";
            var endSundayTrip = "-0-D";

            foreach (var drTrip in circularTripsRows)
            {
                var drCircularOpositDirection = CopyAsNewRow(drTrip);

                var tripId = drTrip["id"].ToString();
                var sourceTripId = tripId;

                if (tripId.Contains("1730"))
                {
                    int i = 0;
                }

                var drTripRoute = _tripAndRouteNames[tripId];

                var tripHeadsign = drTripRoute["route_long_name"].ToString().Replace(drTripRoute["trip_headsign"].ToString(), "");

                tripHeadsign = tripHeadsign.Replace(" -", "").Trim();

                if (tripId.EndsWith(endWeekTrip))
                {
                    tripId = drCircularOpositDirection["route_id"] + endWeekTrip.Replace("0","1");
                }
                else if (tripId.EndsWith(endSaturdayTrip))
                {
                    tripId = drCircularOpositDirection["route_id"] + endSaturdayTrip.Replace("0", "1");
                }
                 else if (tripId.EndsWith(endSundayTrip))
                {
                    tripId = drCircularOpositDirection["route_id"] + endSundayTrip.Replace("0", "1");
;
                }
                
                drCircularOpositDirection["id"] = tripId;
                drCircularOpositDirection["trip_headsign"] = tripHeadsign;
                drCircularOpositDirection["direction_id"] = "1";
                dtTrips.Rows.Add(drCircularOpositDirection);


                //Adicona frequencias 
                var dtFrequency = GetFrequencyDataTable(sourceTripId);
                foreach (DataRow row in dtFrequency.AsEnumerable().ToList())
                {
                    var newRow = CopyAsNewRow(row);
                    newRow["trip_id"] = tripId;
                    dtFrequency.Rows.Add(newRow);
                }

                UpdateFrequencyDataTable(dtFrequency);


            }

            var dtChanges = dtTrips.GetChanges();

            UpdateTripDataTable(dtTrips);
        }


        private DataTable GetBusOnlyTripDataTable()
        {
            var sql = "SELECT t.* FROM trip t INNER JOIN route r ON t.route_id = r.id WHERE r.route_type=3";
            var dtTrips = _sqliteHelper.GetDataTable(sql);
            return dtTrips;
        }

        private void UpdateTripDataTable(DataTable dtTrips)
        {
            var updateCommand = _sqliteHelper.CreateUpdateComand("trip", "id");
            var insertCommand = _sqliteHelper.CreateInsertComand("trip");
            _sqliteHelper.UpdateDataTable(updateCommand, insertCommand, dtTrips);
        }

        private void LoadTripAndRouteNames ()
        {

            var sql =
                "select t.id, t.trip_headsign, r.route_long_name from trip t INNER JOIN route r on t.route_id=r.id";
            var ds= _sqliteHelper.GetDataSet(sql);

           _tripAndRouteNames = ds.Tables[0].AsEnumerable().ToDictionary(dr => dr["id"].ToString());

        }

        private List<string> GetAllCircularTrips()
        {
            //acha linhas circulares 
            var dsTripStopPoints = GetAllTripsStopPointsWeekendSplited();

            var allTrips = (from dr in dsTripStopPoints.Tables[0].AsEnumerable()
                            orderby dr["id"].ToString()
                            select dr["id"].ToString()).Distinct().ToArray();


            var q = from dr in dsTripStopPoints.Tables[0].AsEnumerable()
                    select dr;

            var tripsLookUp = q.ToLookup(x => x["id"]);

            var circularTrips = new List<string>();

            foreach (var trip in allTrips)
            {
                var drFirstStop = tripsLookUp[trip].First();
                var drLastStop = tripsLookUp[trip].Last();

                var distanceFromFirstToLast = _geoLocation.Distance((double)drFirstStop["stop_lat"],
                    (double)drFirstStop["stop_lon"],
                    (double)drLastStop["stop_lat"], (double)drLastStop["stop_lon"]);

                if (distanceFromFirstToLast < 0.6)
                {
                    circularTrips.Add(trip);
                }
            }
            return circularTrips;
        }
    }
}
