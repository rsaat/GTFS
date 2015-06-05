using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFS.Sptrans.Tool.Common;

namespace GTFS.Sptrans.Tool
{
    public class CustomizeSqliteDb
    {
        private readonly string _sptransFeedDirectory;

        private bool _testSingleTrip = false;
        private string _testTripId = @"178L-10-1";
        private string _testShapeId = "53424";

        public CustomizeSqliteDb(string sptransSqliteDbFile, string sptransFeedDirectory)
        {
            _sptransFeedDirectory = sptransFeedDirectory;
            string sqliteConnectionString = string.Format(@"Data Source={0};Version=3;", sptransSqliteDbFile);
            CreateDbConnection(sqliteConnectionString);
        }

        private void ExecuteWithTransaction(Action customization )
        {
            var trans = _sqliteHelper.BeginTransaction();
            try
            {
                customization();

                trans.Commit();
            }
            catch (Exception)
            {
                trans.Rollback();
                throw;
            }
        }

        public void ExecuteCustomizations()
        {

            ExecuteWithTransaction(() =>
            {
                var customize = new CustomizeSqliteSplitCircularLines(_sqliteHelper);

                customize.ExecuteCustomizations();
            });

            return;

           
            ChangeDatabaseStructure();

            ExecuteWithTransaction(() =>
            {
                LoadDataToMemory();
                FillShapeDistTraveledFromStopTimes();
                CheckShapeDistCalulated();
            });


            ExecuteWithTransaction(() =>
            {

                RemoveAccents();
                
                LoadDataToMemory();

                //InactivateStopsTooCloseToEachOther();

                AddGeoHashToStops();

                FillPoiCategories();

                FillPoi();

                FillStopsNearPois();

                ShapeCompressedCreateTable();

                ShapeCompressedFillTable();

            });

            ExecuteWithTransaction(() =>
            {
                var customize = new CustomizeSqliteAddDataFromSptransSite(_sqliteHelper, _sptransFeedDirectory);

                customize.ExecuteCustomizations();
            });
            
            ShrinkSqliteDatabase();

            _connection.Close();
        }

        private void ShrinkSqliteDatabase()
        {
            _sqliteHelper.ExecuteNonQuery("DROP TABLE shape");
            _sqliteHelper.ExecuteNonQuery("vacuum");
        }

        #region Shape compressed



        private void ShapeCompressedFillTable()
        {
            var sql = @"
                    select s.id,
                    s.shape_pt_sequence,
                    s.shape_pt_lat,
                    s.shape_pt_lon,
                    s.shape_dist_traveled 
                    from shape s
                    order by s.id , s.shape_pt_sequence
            ";

            var dtshape = _sqliteHelper.GetDataTable(sql);

            var qShape = from dr in dtshape.AsEnumerable()
                         select dr;
            var lookUpShapes = qShape.ToLookup(r => r["id"]);

            var shapesIds = (from dr in qShape
                             select dr["id"].ToString()).Distinct().OrderBy(x => x);

            foreach (var shapeId in shapesIds)
            {
                var shapeBytes = ConvertShapeToBytes(lookUpShapes, shapeId);

                sql = string.Format("INSERT INTO shape_compressed (FEED_ID, id,shape_data) VALUES (:FEED_ID,:id,:shape_data)");
                var parameters = new List<SQLiteParameter>();
                parameters.Add(_sqliteHelper.CreateParameter("FEED_ID", DbType.Int32, 1));
                parameters.Add(_sqliteHelper.CreateParameter("id", DbType.Int32, shapeId));
                parameters.Add(_sqliteHelper.CreateParameter("shape_data", DbType.Binary, shapeBytes));
                _sqliteHelper.ExecuteNonQuery(sql, parameters.ToArray());
            }



        }

        private static byte[] ConvertShapeToBytes(ILookup<object, DataRow> lookUpShapes, string shapeId)
        {
            var drShapesFromID = lookUpShapes[shapeId];

            var shapesCompressed = new List<ShapeCompressed>();

            foreach (var drShape in drShapesFromID.ToList().OrderBy(x => x["shape_pt_sequence"]))
            {
                shapesCompressed.Add(new ShapeCompressed((double)drShape["shape_pt_lat"],
                    (double)drShape["shape_pt_lon"],
                    (double)drShape["shape_dist_traveled"]));
            }

            var shapeBytes = new List<byte>();
            foreach (var s in shapesCompressed)
            {
                shapeBytes.AddRange(s.ToBytes());
            }
            return shapeBytes.ToArray();
        }


        void ShapeCompressedCreateTable()
        {

            var sql = "CREATE TABLE shape_compressed (FEED_ID INTEGER NOT NULL, id INTEGER NOT NULL, shape_data BLOB)";
            _sqliteHelper.ExecuteNonQuery(sql);


            sql = "CREATE UNIQUE INDEX indx_shape_compressed_id ON shape_compressed (id);";
            _sqliteHelper.ExecuteNonQuery(sql);
        }



        #endregion


        #region Remove Accents

        void RemoveAccents()
        {
            RemoveAccentsStop();
            RemoveAccentsRoute();
            RemoveAccentsTrip();
        }

        private void RemoveAccentsTrip()
        {
            var sql = "select t.id,t.trip_headsign from trip t";

            var dt = _sqliteHelper.GetDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                row["trip_headsign"] = Strings.RemoveDiacritics(row["trip_headsign"].ToString().ToUpper());
            }


            sql = "UPDATE trip SET trip_headsign = :trip_headsign  WHERE id = :id ";

            var updateCommand = new SQLiteCommand(sql);
            updateCommand.Parameters.Add(new SQLiteParameter("id", DbType.String, 8, "id"));
            updateCommand.Parameters.Add(new SQLiteParameter("trip_headsign", DbType.String, 8, "trip_headsign"));


            _sqliteHelper.UpdateDataTable(updateCommand, dt);
        }

        private void RemoveAccentsRoute()
        {
            var sql = "select r.id,r.route_long_name from route r";

            var dt = _sqliteHelper.GetDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                row["route_long_name"] = Strings.RemoveDiacritics(row["route_long_name"].ToString().ToUpper());
            }


            sql = "UPDATE route SET route_long_name = :route_long_name  WHERE id = :id ";

            var updateCommand = new SQLiteCommand(sql);
            updateCommand.Parameters.Add(new SQLiteParameter("id", DbType.String, 8, "id"));
            updateCommand.Parameters.Add(new SQLiteParameter("route_long_name", DbType.String, 8, "route_long_name"));


            _sqliteHelper.UpdateDataTable(updateCommand, dt);
        }

        private void RemoveAccentsStop()
        {
            var sql = "select s.id,s.stop_name,s.stop_desc from stop s";

            var dt = _sqliteHelper.GetDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                row["stop_name"] = Strings.RemoveDiacritics(row["stop_name"].ToString().ToUpper());
                row["stop_desc"] = Strings.RemoveDiacritics(row["stop_desc"].ToString().ToUpper());
            }


            sql = "UPDATE stop SET stop_name = :stop_name,stop_desc=:stop_desc  WHERE id = :id ";

            var updateCommand = new SQLiteCommand(sql);
            updateCommand.Parameters.Add(new SQLiteParameter("id", DbType.Int64, 8, "id"));
            updateCommand.Parameters.Add(new SQLiteParameter("stop_name", DbType.String, 8, "stop_name"));
            updateCommand.Parameters.Add(new SQLiteParameter("stop_desc", DbType.String, 8, "stop_desc"));

            _sqliteHelper.UpdateDataTable(updateCommand, dt);
        }

        #endregion

        #region Poi Update

        private void FillStopsNearPois()
        {
            var dtPois = GetAllPois();
            var dtBusStops = GetAllBusStops();
            var gloc = new Gtfs2Sqlite.Util.Geolocation();

            foreach (DataRow drPoi in dtPois.Rows)
            {
                var qDistancePoiStops = from DataRow dr in dtBusStops.AsEnumerable()
                                        select new
                                        {
                                            StopId = dr["stop_id"].ToString(),
                                            DistanceStopPoi = gloc.Distance((double)drPoi["poi_lat"], (double)drPoi["poi_lon"], (double)dr["stop_lat"], (double)dr["stop_lon"])
                                        };

                var qStopsNearPoi = qDistancePoiStops.Where(x => x.DistanceStopPoi < 0.6).OrderBy(x => x.DistanceStopPoi).ToList();

                foreach (var stop in qStopsNearPoi)
                {
                    var sql = string.Format("INSERT INTO poi_stop (poi_id_fk, stop_id_fk) VALUES ({0},'{1}')", drPoi["poi_id"], stop.StopId);
                    _sqliteHelper.ExecuteNonQuery(sql);
                }


            }

        }

        private DataTable GetAllPois()
        {
            var sql = @"SELECT p.poi_id,p.poi_lat,p.poi_lon,p.poi_geohash from poi p";

            return _sqliteHelper.GetDataTable(sql);
        }

        private DataTable GetAllBusStops()
        {
            var sql = @"SELECT DISTINCT  st.stop_id,
                        s.stop_name,s.stop_lat,s.stop_lon,s.stop_geohash
                        from  stop_time st 
                        INNER JOIN trip t ON t.id = st.trip_id 
                        INNER JOIN stop s ON s.id = st.stop_id 
                        INNER JOIN route r ON r.id=t.route_id
                        WHERE NOT (r.route_type=1 or r.route_type=2) 
                        ORDER BY st.stop_id";

            return _sqliteHelper.GetDataTable(sql);

        }

        private void FillPoi()
        {
            var sql = @"SELECT r.route_short_name, 
                               st.stop_id,s.stop_name,
                               s.stop_lat,s.stop_lon,s.stop_geohash,r.route_type,pc.poi_cat_id
                        FROM  stop_time st 
                        INNER JOIN trip t ON t.id = st.trip_id 
                        INNER JOIN stop s ON s.id = st.stop_id 
                        INNER JOIN route r ON r.id=t.route_id
                        INNER JOIN poi_category pc ON pc.poi_cat_name=r.route_short_name
                        WHERE (r.route_type=1 or r.route_type=2) AND t.direction_id=1
                        ORDER BY  st.trip_id, st.stop_sequence"; //Subway and Train types 
            var dt = _sqliteHelper.GetDataSet(sql).Tables[0];
            int i = 0;
            foreach (DataRow drRoute in dt.Rows)
            {
                i++;
                sql = string.Format("INSERT INTO poi (poi_id, poi_name, poi_lat, poi_lon, poi_geohash, poi_cat_id_fk)" +
                                    " VALUES({0}, '{1}', {2}, {3}, '{4}', {5})",
                                    i,
                                    drRoute["route_short_name"] + "-" + drRoute["stop_name"],
                                    (drRoute["stop_lat"]).ToString().Replace(",", "."),
                                    (drRoute["stop_lon"]).ToString().Replace(",", "."),
                                    drRoute["stop_geohash"],
                                    drRoute["poi_cat_id"]
                                    );
                _sqliteHelper.ExecuteNonQuery(sql);
            }

        }

        private void FillPoiCategories()
        {
            var sql = @"SELECT r.route_short_name 
                        FROM route r
                        WHERE (r.route_type=1 or r.route_type=2)
                        ORDER BY r.route_short_name"; //Subway and Train types 
            var dt = _sqliteHelper.GetDataTable(sql);
            int i = 0;
            foreach (DataRow drRoute in dt.Rows)
            {
                i++;
                sql = string.Format("INSERT INTO poi_category (poi_cat_id, poi_cat_name) VALUES ({0},'{1}')", i, drRoute["route_short_name"]);
                _sqliteHelper.ExecuteNonQuery(sql);
            }

        }

        private void CreatePoiTables()
        {
            var sql = "CREATE TABLE poi (poi_id INTEGER PRIMARY KEY,poi_cat_id_fk INTEGER, poi_name TEXT, poi_lat REAL, poi_lon REAL, poi_geohash TEXT)";
            _sqliteHelper.ExecuteNonQuery(sql);
            CreateIndex("indx_poi_geohash", "poi_geohash", "poi");
            CreateIndex("indx_poi_cat_id_fk", "poi_cat_id_fk", "poi");

            sql = "CREATE TABLE poi_stop (poi_id_fk INTEGER, stop_id_fk TEXT)";
            _sqliteHelper.ExecuteNonQuery(sql);
            CreateIndex("indx_poi_stop_poi_id_fk", "poi_id_fk", "poi_stop");
            CreateIndex("indx_stop_id_fk", "stop_id_fk", "poi_stop");

            sql = "CREATE TABLE poi_category (poi_cat_id INTEGER PRIMARY KEY, poi_cat_name TEXT)";
            _sqliteHelper.ExecuteNonQuery(sql);
        }

        #endregion

        private void ChangeDatabaseStructure()
        {
            CreatePoiTables();

            _sqliteHelper.ExecuteNonQuery("ALTER TABLE stop ADD COLUMN  stop_geohash TEXT");
            CreateIndex("indx_stop_geohash", "stop_geohash", "stop");

            CreateIndexes();

            _sqliteHelper.ExecuteNonQuery("UPDATE stop_time set shape_dist_traveled=0");
        }

        private void AddGeoHashToStops()
        {
            var sql = "SELECT s.id,s.stop_lat,s.stop_lon from stop s";
            var dt = _sqliteHelper.GetDataTable(sql);


            foreach (DataRow dr in dt.Rows)
            {
                var stopId = Convert.ToDouble(dr["id"]);
                var latitude = Convert.ToDouble(dr["stop_lat"]);
                var longitude = Convert.ToDouble(dr["stop_lon"]);

                var stopGeohash = Gtfs2Sqlite.Util.Geohash.Encode(latitude, longitude);

                sql = "update stop set stop_geohash='{0}' Where id='{1}'";
                sql = string.Format(sql, stopGeohash, stopId);
                var rowsUpdated = _sqliteHelper.ExecuteNonQuery(sql);

                if (rowsUpdated != 1)
                {
                    throw new InvalidOperationException("error updatind geohash. Rows Updated <> 1. " + rowsUpdated);
                }

            }
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
            var distanceTolerancesToFindShapePoint = new double[] { 0.05, 0.1, 0.2, 0.5 };

            foreach (var distance in distanceTolerancesToFindShapePoint)
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
            _sqliteHelper.ExecuteNonQuery(sql);
        }
    }
}
