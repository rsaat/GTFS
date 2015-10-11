using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GTFS.Sptrans.WebsiteDownloader;

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

            TripAlterTable();

            var circularTrips = GetAllCircularTripsUsingShape();

            if (true)
            {
                var dtTrips = GetBusOnlyTripDataTable();


                var qTripCircular =
                    dtTrips.AsEnumerable().Where(dr => circularTrips.Contains(dr["id"].ToString().ToUpper()));

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

                    var drTripRoute = _tripAndRouteNames[tripId];

                    var tripHeadsign =
                        drTripRoute["route_long_name"].ToString().Replace(drTripRoute["trip_headsign"].ToString(), "");

                    tripHeadsign = tripHeadsign.Replace(" -", "").Trim();

                    if (tripId.EndsWith(endWeekTrip))
                    {
                        tripId = drCircularOpositDirection["route_id"] + endWeekTrip.Replace("0", "1");
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

                UpdateTripDataTable(dtTrips);

                FillIsCircularField();
            }

            FillCutOffCircularField();

        }

        private Dictionary<string, int> _mCutOffStopSeqCircularTrip = new Dictionary<string, int>();

        private void FillCutOffCircularField()
        {
            FindCircularTripCutoffStopSequence();

            var dtTrips = GetBusOnlyTripDataTable();

            foreach (DataRow dr in dtTrips.Rows)
            {
                dr["circular_cut_stop_seq"] = 0;

                if (_mCutOffStopSeqCircularTrip.Keys.Contains(dr["id"]))
                {
                    dr["circular_cut_stop_seq"] = _mCutOffStopSeqCircularTrip[dr["id"].EmptyIfNull()];
                }
            }

            UpdateTripDataTable(dtTrips);

        }

        private void FindCircularTripCutoffStopSequence()
        {
            var circularTrips = GetAllCircularTripsUsingShape();
            var allTripStops = GetAllTripsStopPointsWeekendSplited();

            var tripsLookup = allTripStops.Tables[0].AsEnumerable().ToLookup(x => x["id"].EmptyIfNull());

            foreach (var circularTripId in circularTrips)
            {
                var tripStops = tripsLookup[circularTripId];
                var drFirst = tripStops.First();
                var firstLat = (double) drFirst["stop_lat"];
                var firstLon = (double) drFirst["stop_lon"];
                var maxDistance = 0.0;
                var maxDistanceStopSequence = 0;
                foreach (var drTripStop in tripStops)
                {
                    var distance = _geoLocation.Distance(firstLat, firstLon, (double) drTripStop["stop_lat"],
                        (double) drTripStop["stop_lon"]);

                    if (distance >= maxDistance)
                    {
                        maxDistance = distance;
                        maxDistanceStopSequence = Convert.ToInt32(drTripStop["stop_sequence"]);
                    }
                }

                if (maxDistanceStopSequence > 0)
                {
                    _mCutOffStopSeqCircularTrip.Add(circularTripId, maxDistanceStopSequence);
                }
                else
                {
                    throw new InvalidOperationException("Error finding cutoff stop sequence for circular trip " + circularTripId);
                }
            }
        }

        private void FillIsCircularField()
        {
            var circularTrips = GetAllCircularTripsUsingShape();

            var dtTrips = GetBusOnlyTripDataTable();

            foreach (DataRow dr in dtTrips.Rows)
            {
                dr["is_circular"] = 0;

                if (circularTrips.Contains(dr["id"]))
                {
                    dr["is_circular"] = 1;
                }
            }

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

        private void LoadTripAndRouteNames()
        {

            var sql =
                "select t.id, t.trip_headsign, r.route_long_name from trip t INNER JOIN route r on t.route_id=r.id";
            var ds = _sqliteHelper.GetDataSet(sql);

            _tripAndRouteNames = ds.Tables[0].AsEnumerable().ToDictionary(dr => dr["id"].ToString());

        }

        private List<string> GetAllCircularTripsUsingShape()
        {
            var sql = "select t.id, s.shape_data from trip t INNER join shape_compressed s on t.shape_id=s.id ORDER BY  t.id";
            var ds = _sqliteHelper.GetDataSet(sql);
            var circularTrips = new List<string>();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var tripId = dr["id"].EmptyIfNull();
                var shapeData = (byte[])dr["shape_data"];
                var shapePoints = GetShapePoints(shapeData);

                var firstShape = shapePoints.First();
                var lastShape = shapePoints.Last();

                var distanceFromFirstToLast = _geoLocation.Distance(firstShape.Latitude,
                    firstShape.Longitude,
                    lastShape.Latitude, lastShape.Longitude);

                if (distanceFromFirstToLast < 0.6)
                {
                    circularTrips.Add(tripId);
                }

            }

            return circularTrips;

        }


        public ICollection<ShapeCompressed> GetShapePoints(byte[] shapeData)
        {

            var shapePoints = new List<ShapeCompressed>();
            var shapePointSize = sizeof(int) * 3;
            var shapeCompressedBytes = new byte[shapePointSize];

            for (int i = 0; i < shapeData.Length; i += shapePointSize)
            {
                Array.Copy(shapeData, i, shapeCompressedBytes, 0, shapePointSize);
                var shapePoint = new ShapeCompressed(shapeCompressedBytes);
                shapePoints.Add(shapePoint);
            }

            return shapePoints;
        }


        private void TripAlterTable()
        {
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE trip ADD COLUMN  is_circular INT");
            _sqliteHelper.ExecuteNonQuery("UPDATE trip SET is_circular=0");

            _sqliteHelper.ExecuteNonQuery("ALTER TABLE trip ADD COLUMN  circular_cut_stop_seq INT");
            _sqliteHelper.ExecuteNonQuery("UPDATE trip SET circular_cut_stop_seq=0");
        }

    }
}
