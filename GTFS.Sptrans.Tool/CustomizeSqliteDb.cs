using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS.Sptrans.Tool
{
    public class CustomizeSqliteDb
    {

        private bool _testSingleTrip = false;
        private string _testTripId = @"178L-10-1";
        private string _testShapeId = "53424";

        public CustomizeSqliteDb(string sptransSqliteDbFile)
        {
            string sqliteConnectionString = string.Format(@"Data Source={0};Version=3;", sptransSqliteDbFile);
            CreateDbConnection(sqliteConnectionString);
        }

        public void ExecuteCustomizations()
        {

            //CreateIndexes();

            LoadDataToMemory();

            var trans = _sqliteHelper.BeginTransaction();
            try
            {
                FillShapeDistTraveledFromStopTimes();

                CheckShapeDistCalulated();

                InactivateStopsTooCloseToEachOther();

                trans.Commit();
            }
            catch (Exception)
            {
                trans.Rollback();
                throw;
            }


            _connection.Close();
        }

        #region Inactivate Stops 
        /// <summary>
        ///     Remove paradas de uma viagem (trip) que são muito próximas uma da outra, ex menos de 50m, pois normalmente são paradas de terminais 
        ///     e ônibus não para em todas elas. Deixar somente a parada mais distante da parada anterior anterior.
        /// </summary>
        private void InactivateStopsTooCloseToEachOther()
        {
            ClearDropOffAndPickup();

            var allTrips = (from dr in _dsTripStopPoints.Tables[0].AsEnumerable()
                            orderby dr["id"].ToString()
                            select new { TripId = dr["id"].ToString() }).Distinct().ToArray();


            var qTripStopPoints = from dr in _dsTripStopPoints.Tables[0].AsEnumerable()
                                  select new
                                  {
                                      TripId = dr["id"].ToString(),
                                      StopSequence = Convert.ToInt32(dr["stop_sequence"]),
                                      ShapeDistTraveld = Convert.ToDouble(dr["shape_dist_traveled"], CultureInfo.InvariantCulture),
                                      StopID = dr["stop_id"].ToString(),
                                      StopName = dr["stop_name"].ToString(),
                                      StopLatitude = Convert.ToDouble(dr["stop_lat"]),
                                      StopLongitude = Convert.ToDouble(dr["stop_lon"])
                                  };

            var tripStopPointsLookup = qTripStopPoints.ToLookup(r => r.TripId);

            foreach (var trip in allTrips)
            {
                var tripStopPoints = tripStopPointsLookup[trip.TripId].ToArray();

                var gloc = new Gtfs2Sqlite.Util.Geolocation();

                bool isEvaluatingFirstStop;

                for (int i = 1; i < tripStopPoints.Length; i++)
                {
                    var stopN_1 = tripStopPoints[i - 1];
                    var stopN = tripStopPoints[i];

                    isEvaluatingFirstStop = i == 1;

                    var twoStopsSameName = String.Equals(stopN.StopName, stopN_1.StopName, StringComparison.CurrentCultureIgnoreCase);
                    var distanceBetweenStops = gloc.Distance(stopN_1.StopLatitude, stopN_1.StopLongitude, stopN.StopLatitude, stopN.StopLongitude);
                    var distanceTravelledBetweenStops = stopN.ShapeDistTraveld - stopN_1.ShapeDistTraveld;

                    var isSameStop = IsSameStop(twoStopsSameName, distanceBetweenStops, distanceTravelledBetweenStops);

                    if (i > 1)
                    {
                        if (!isSameStop)
                        {
                            isEvaluatingFirstStop = false;
                        }

                    }


                    if (isSameStop)
                    {
                        if (isEvaluatingFirstStop)
                        {
                            //disable dropoff e pickup  of StopN    
                            UpdateDropOffAndPickup(stopN.TripId, stopN.StopID, stopN.StopSequence, PickupDropOffType.NotAvailable);
                        }
                        else
                        {
                            //disable dropoff e pickup  of StopN_1 
                            UpdateDropOffAndPickup(stopN_1.TripId, stopN_1.StopID, stopN_1.StopSequence, PickupDropOffType.NotAvailable);
                        }

                    }


                }

            }

        }

        private static bool IsSameStop(bool twoStopsSameName, double distanceBetweenStops, double distanceTravelledBetweenStops)
        {
            var AreTheSameStop = false;
            var distance = distanceBetweenStops; //Math.Max(distanceBetweenStops);
            if (twoStopsSameName)
            {
                if (distance < 0.3)
                {
                    AreTheSameStop = true;
                }
            }
            else
            {
                if (distance < 0.1)
                {
                    AreTheSameStop = true;
                }
            }
            return AreTheSameStop;
        }

        private enum PickupDropOffType
        {
            RegularlyScheduled = 0,
            NotAvailable = 1
        }

        private void ClearDropOffAndPickup()
        {
            string sql;
            sql = "update stop_time set pickup_type={0} , drop_off_type={0}";
            sql = string.Format(sql, (int)PickupDropOffType.RegularlyScheduled);
            _sqliteHelper.ExecuteNonQuery(sql);

        }

        private void UpdateDropOffAndPickup(string tripId, string stopId, int stopSequence, PickupDropOffType pickupDropOffType)
        {
            string sql;
            sql = "update stop_time set pickup_type={0} , drop_off_type={0}  Where stop_id='{1}' AND trip_id='{2}' AND stop_sequence ={3} ";
            sql = string.Format(sql, (int)pickupDropOffType, stopId, tripId, stopSequence);
            var rowsUpdated = _sqliteHelper.ExecuteNonQuery(sql);
            if (rowsUpdated != 1)
            {
                throw new InvalidOperationException("error updating stop_time rowsUpdated!=1 rowsUpdated=" + rowsUpdated);
            }
        }
        
        #endregion

        #region Fill shape Distance Traveled

        private void FillShapeDistTraveledFromStopTimes()
        {

            var shapesLookup = GetShapesLookup();

            var totalCount = _dsTripStopPoints.Tables[0].Rows.Count;
            _lastShapeIndexFound = 0;
            var rowIndex = 0;
            var lastTrip = "";
            foreach (DataRow drTripStopPoint in _dsTripStopPoints.Tables[0].Rows)
            {
                rowIndex++;

                if (rowIndex % 100 == 0)
                {
                    System.Console.WriteLine("FillShapeDistTraveledFromStopTimes:" + rowIndex + " of " + totalCount);
                }

                var stopId = drTripStopPoint["stop_id"];
                var tripId = drTripStopPoint["id"];
                var departureTime = Convert.ToInt32(drTripStopPoint["departure_time"]);

                var shapeId = Convert.ToInt32(drTripStopPoint["shape_id"]);
                var shapeValues = shapesLookup[shapeId].ToArray();

                if (lastTrip != tripId.ToString())
                {
                    _lastShapeIndexFound = 0;
                    lastTrip = tripId.ToString();
                }

                var stopDistance = FindDistanceFromShapeStart(drTripStopPoint, shapeValues);

                UpdateShapeDistTraveled((string)tripId, (string)stopId, stopDistance, departureTime);

            }

        }

        private void CheckShapeDistCalulated()
        {

            var allTrips = (from dr in _dsTripStopPoints.Tables[0].AsEnumerable()
                            orderby dr["id"].ToString()
                            select new { TripId = dr["id"].ToString() }).Distinct().ToArray();


            var qTripStopPoints = from dr in _dsTripStopPoints.Tables[0].AsEnumerable()
                                  select new
                                  {
                                      TripId = dr["id"].ToString(),
                                      StopSequence = Convert.ToInt32(dr["stop_sequence"]),
                                      ShapeDistTraveld = Convert.ToDouble(dr["shape_dist_traveled"], CultureInfo.InvariantCulture),
                                      StopName = dr["stop_name"].ToString(),
                                  };

            var tripStopPointsLookup = qTripStopPoints.ToLookup(r => r.TripId);

            foreach (var trip in allTrips)
            {
                var tripStopPoints = tripStopPointsLookup[trip.TripId];

                var tripStopPointsOrderedByTripId = tripStopPoints.OrderBy(x => x.TripId).ToArray();
                var tripStopPointsOrderedByShapeDistTraveld = tripStopPoints.OrderBy(x => x.ShapeDistTraveld).ToArray();
                if (!tripStopPointsOrderedByTripId.SequenceEqual(tripStopPointsOrderedByShapeDistTraveld))
                {
                    System.Diagnostics.Debug.WriteLine("Trip:" + trip.TripId + " has invalid ShapeDistTraveld");
                }
            }

        }

        private void UpdateShapeDistTraveled(string tripID, string stopId, double stopDistance, int departureTime)
        {
            string sql;
            var invC = CultureInfo.InvariantCulture;
            sql = "update stop_time set shape_dist_traveled={0} Where stop_id='{1}' AND trip_id='{2}' AND departure_time ={3} ";
            invC = CultureInfo.InvariantCulture;
            sql = string.Format(sql, stopDistance.ToString(invC), stopId, tripID, departureTime);
            var rowsUpdated = _sqliteHelper.ExecuteNonQuery(sql);
            if (rowsUpdated != 1)
            {
                throw new InvalidOperationException("error updating stop_time rowsUpdated!=1 rowsUpdated=" + rowsUpdated);
            }
        }

        private int _lastShapeIndexFound;

        private double FindDistanceFromShapeStart(DataRow drTripStopPoint, DataRow[] shapeValues)
        {
            var gloc = new Gtfs2Sqlite.Util.Geolocation();

            var stopLatitude = Convert.ToDouble(drTripStopPoint["stop_lat"]);
            var stopLongitude = Convert.ToDouble(drTripStopPoint["stop_lon"]);


            var shapes = from drShape in shapeValues
                         select new
                         {
                             ShapeIndex = Convert.ToInt32(drShape["shape_pt_sequence"]),
                             DistanceFromShape =
                                 gloc.Distance(stopLatitude, stopLongitude, (double)drShape["shape_pt_lat"],
                                     (double)drShape["shape_pt_lon"]),
                             ShapeDistTraveled = (double)drShape["shape_dist_traveled"]
                         };

            var shapesCloseToStop = shapes.OrderBy(x => x.ShapeIndex).ToArray();
            var newIndex = -1;
            var distanceFromShapetoToStop = 0.2;
            var distancesKm = new[] { 0.2, 0.5 };

            foreach (var distance in distancesKm)
            {
                distanceFromShapetoToStop = distance;
                for (int i = _lastShapeIndexFound; i < shapes.Count(); i++)
                {
                    if (shapesCloseToStop[i].DistanceFromShape < distanceFromShapetoToStop)
                    {
                        newIndex = i;
                        goto Found;
                    }

                }
            }

        Found:

            if (newIndex < 0)
            {
                newIndex = _lastShapeIndexFound;
                var distance = shapesCloseToStop[newIndex].DistanceFromShape;
                ReportStopGtfsErrors("Large distance(km) from shape to stop " + distance, drTripStopPoint);
            }
            else
            {
                _lastShapeIndexFound = newIndex;
            }


            return shapesCloseToStop[newIndex].ShapeDistTraveled;

        }

        private void ReportStopGtfsErrors(string message, DataRow drTripStopPoint)
        {
            var text = "";
            var dr = drTripStopPoint;
            var t = TimeSpan.FromSeconds(Convert.ToDouble(dr["departure_time"]));
            var departureTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);

            var textInfo = string.Format(
                "trip_id={0} shape_id={1} stop_id={2} stop_sequence={3} stop_name={4} departure_time={5} stop_lat={6} stop_lon={7}",
                dr["id"], dr["shape_id"], dr["stop_id"], dr["stop_sequence"], dr["stop_name"], departureTime, dr["stop_lat"], dr["stop_lon"]);

            text += "Trip:" + textInfo + "\r\n" + "Error:" + message;

            System.Diagnostics.Debug.WriteLine(text);

        } 
       
        #endregion

        private void LoadDataToMemory()
        {
            _dsTripStopPoints = GetAllTripsStopPoints();
        }

        private ILookup<int, DataRow> GetShapesLookup()
        {
            var sql = "select * from shape s {0} order by s.id,s.shape_pt_sequence";

            if (_testSingleTrip)
            {
                sql = string.Format(sql, "WHERE s.id=" + _testShapeId);
            }
            else
            {
                sql = string.Format(sql, "");
            }
            var ds = _sqliteHelper.GetDataSet(sql);

            var qShapes = from dr in ds.Tables[0].AsEnumerable()
                          select dr;

            var shapesLookup = qShapes.ToLookup(r => Convert.ToInt32(r["id"]));
            return shapesLookup;
        }

        private DataSet GetAllTripsStopPoints()
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
            INNER JOIN trip t ON t.id = st.trip_id 
            INNER JOIN stop s ON s.id = st.stop_id 
            {0}
            ORDER BY st.trip_id, st.stop_sequence";

            if (_testSingleTrip)
            {

                sql = string.Format(sql, string.Format(@"WHERE   t.id=""{0}"" ", _testTripId));
            }
            else
            {
                sql = string.Format(sql, "");
            }

            return _sqliteHelper.GetDataSet(sql);

        }

        private void CreateIndexes()
        {
            CreateIndex("indx_freq_trip_id", "trip_id", "frequency");
            CreateIndex("indx_route_id", "id", "route");
            CreateIndex("indx_shape_id", "id", "shape");
            CreateIndex("indx_stop_id", "id", "stop");
            CreateIndex("indx_st_trip_id", "trip_id", "stop_time");
            CreateIndex("indx_st_stop_id", "stop_id", "stop_time");
            CreateIndex("indx_trip_id", "id", "trip");
            CreateIndex("indx_trip_route_id", "route_id", "trip");
        }

        private SQLiteConnection _connection;
        private SqLiteDatabaseHelper _sqliteHelper;
        private DataSet _dsTripStopPoints;

        private void CreateDbConnection(string sqliteConnectionString)
        {
            _connection = new SQLiteConnection(sqliteConnectionString);
            _connection.Open();
            _sqliteHelper = new SqLiteDatabaseHelper(_connection);
        }

        private void CreateIndex(string indexName, string indexField, string tableName)
        {
            var sql = string.Format("CREATE INDEX {0} ON {2} ({1})", indexName, indexField, tableName);
            ExecuteNonQuery(sql);
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }


    }
}
