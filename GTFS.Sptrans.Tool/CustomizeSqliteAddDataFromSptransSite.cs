using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GTFS.Sptrans.Tool.CSV;
using GTFS.Sptrans.Tool.Common;

namespace GTFS.Sptrans.Tool
{
    public class CustomizeSqliteAddDataFromSptransSite
    {
        private readonly SqLiteDatabaseHelper _sqliteHelper;
        private string _sptransFeedDirectory;
        private IList<SptransLineDetails> _allLinesFromCsvFile;
        private ILookup<string, SptransLineDetails> _allLinesByLineNumber;

        public CustomizeSqliteAddDataFromSptransSite(SqLiteDatabaseHelper sqliteHelper, string sptransFeedDirectory)
        {
            _sptransFeedDirectory = sptransFeedDirectory;
            _sqliteHelper = sqliteHelper;
        }

        public void ExecuteCustomizations()
        {

            TripAlterTable();

            SplitTripsWithWeekendServices();

            LoadCsvFileData();

            RouteUpdateAll();

            AddTripTimes();

            UpdateAllTripFrequencies();
        }

        private void LoadCsvFileData()
        {
            var sptransLinesCsv = Path.Combine(_sptransFeedDirectory, "sptranslines.csv");

            var reader = new SptransCsvFileReader(sptransLinesCsv);

            _allLinesFromCsvFile = reader.GetAll();

            _allLinesByLineNumber = _allLinesFromCsvFile.ToLookup(x => x.LineNumber);
        }

        #region Frequencies

        private void UpdateAllTripFrequencies()
        {
            var dtTrips = GetTripDataTable();
            var total = dtTrips.Rows.Count;
            var counter = 0;
            foreach (var drTrip in dtTrips.AsEnumerable())
            {
                var routeTrips = _allLinesByLineNumber[drTrip["route_id"].ToString()];
                if (routeTrips.Any())
                {
                    UpdateTripFrequency(drTrip);
                }

                counter++;

                Console.Write("\r {1:00000} of {2:00000} Trip:{0}", drTrip["id"], counter, total);

            }

        }

        private void UpdateTripFrequency(DataRow drTrip)
        {
            var tripDetail = FindTripDetail(drTrip);


            if (tripDetail != null)
            {
                Func<string, int> hour = (time) => int.Parse(time.Split(':')[0]);
                Func<string, int> minute = (time) => int.Parse(time.Split(':')[1]);
                var departures = (from time in tripDetail.Departures
                                  select new TimeSpan(hour(time), minute(time), 0)).ToList();

                //partidas realizadas no dia seguinte
                for (int i = 1; i < departures.Count; i++)
                {
                    if (departures[i - 1] > departures[i])
                    {
                        departures[i] = departures[i].Add(new TimeSpan(24, 0, 0));
                    }
                }

                var headWays = new List<int>();
                for (int i = 1; i < departures.Count; i++)
                {
                    headWays.Add((int)(departures[i] - departures[i - 1]).TotalSeconds);
                }

                if (departures.Count > 0)
                {
                    RemoveCurrentFrequencies(tripDetail.GtfsTripId);
                }

                int currentHeadWay = 0;
                var dtFrequency = GetFrequencyDataTable();
                DataRow drFreq = null;

                if (departures.Count == 1)
                {
                    drFreq = dtFrequency.NewRow();
                    drFreq["FEED_ID"] = 1;
                    drFreq["trip_id"] = tripDetail.GtfsTripId;
                    drFreq["exact_times"] = 0;
                    drFreq["start_time"] = departures[0].ToGtsftime();
                    drFreq["end_time"] = departures[0].ToGtsftime();
                    drFreq["headway_secs"] = 24 * 60 * 60;
                    dtFrequency.Rows.Add(drFreq);
                }
                else
                {

                    for (int depIndx = 0; depIndx < departures.Count; depIndx++)
                    {
                        var headWay = 0;

                        if (depIndx > 0)
                        {
                            headWay = headWays[depIndx - 1];
                        }

                        if (currentHeadWay != headWay)
                        {
                            drFreq = dtFrequency.NewRow();
                            drFreq["FEED_ID"] = 1;
                            drFreq["trip_id"] = tripDetail.GtfsTripId;
                            drFreq["exact_times"] = 0;
                            dtFrequency.Rows.Add(drFreq);
                            currentHeadWay = headWay;
                            drFreq["start_time"] = departures[depIndx - 1].ToGtsftime();
                            drFreq["end_time"] = departures[depIndx].ToGtsftime();
                            drFreq["headway_secs"] = currentHeadWay;
                        }
                        else
                        {
                            if (drFreq != null)
                            {
                                drFreq["end_time"] = departures[depIndx].ToGtsftime();
                            }

                        }

                    }
                }

                UpdateFrequencyDataTable(dtFrequency);

            }
        }

        private void UpdateFrequencyDataTable(DataTable dtFrequency)
        {
            var insertCommand = _sqliteHelper.CreateInsertComand("frequency");
            _sqliteHelper.UpdateDataTable(null, insertCommand, dtFrequency);
        }

        private DataTable GetFrequencyDataTable()
        {
            var sql = "SELECT * FROM frequency WHERE (1=0)";
            var dtTrips = _sqliteHelper.GetDataTable(sql);
            return dtTrips;
        }

        private void RemoveCurrentFrequencies(string tripId)
        {
            var sql = "DELETE FROM frequency WHERE (trip_id = :trip_id)";
            var parameters = new[] { new SQLiteParameter("trip_id", tripId) };
            var rowsDeleted = _sqliteHelper.ExecuteNonQuery(sql, parameters);

        }

        #endregion
        
        #region Trips

        private SptransLineDetails FindTripDetail(DataRow drTrip)
        {
            var routeTrips = _allLinesByLineNumber[drTrip["route_id"].ToString()];

            if (routeTrips.Any())
            {
                var tripDetail = routeTrips.FirstOrDefault(r => String.Equals(r.GtfsTripId, drTrip["id"].ToString(), StringComparison.CurrentCultureIgnoreCase));

                if (tripDetail != null)
                {
                    return tripDetail;
                }
            }

            return null;
        }

        private void AddTripTimes()
        {
            //trip_time_morning , trip_time_noon , trip_time_evening
            var dtTrips = GetTripDataTable();

            foreach (var drTrip in dtTrips.AsEnumerable())
            {
                FillTripTimes(drTrip, "U__", DetailsItineraryDay.DetailsItineraryWeekDay);
                FillTripTimes(drTrip, "_S_", DetailsItineraryDay.DetailsItinerarySaturday);
                FillTripTimes(drTrip, "__D", DetailsItineraryDay.DetailsItinerarySunday);
            }


            UpdateTripDataTable(dtTrips);
        }

        private void FillTripTimes(DataRow drTrip, string serviceId, DetailsItineraryDay detailsItineraryDay)
        {
            var routeTrips = _allLinesByLineNumber[drTrip["route_id"].ToString()];
            if (routeTrips.Any())
            {
                if (drTrip["service_id"].ToString().Contains(serviceId))
                {
                    var tripDetail = routeTrips.FirstOrDefault(r => r.DetailsItineraryDay == detailsItineraryDay);

                    if (tripDetail != null)
                    {
                        drTrip["trip_time_morning"] = tripDetail.TimeDurationMorning;
                        drTrip["trip_time_noon"] = tripDetail.TimeDurationNoon;
                        drTrip["trip_time_evening"] = tripDetail.TimeDurationEvening;
                    }
                }
            }
        }

        void SplitTripsWithWeekendServices()
        {
            var dtTrips = GetTripDataTable();

            var weekendServiceIds = new string[] { "USD", "US_", "U_D" };

            var qTripWithWeekendServices = dtTrips.AsEnumerable().Where(dr => weekendServiceIds.Contains(dr["service_id"].ToString().ToUpper()));

            var tripWithWeekendServices = qTripWithWeekendServices.ToList();

            foreach (var drTrip in tripWithWeekendServices)
            {
                DataRow drNewSaturday;
                DataRow drNewSunday;

                if (drTrip["service_id"].ToString().ToUpper().Contains("S"))
                {
                    drNewSaturday = CopyAsNewRow(drTrip);
                    drNewSaturday["id"] = drTrip["id"].ToString() + "-S";
                    drNewSaturday["service_id"] = "_S_";
                    dtTrips.Rows.Add(drNewSaturday);
                }

                if (drTrip["service_id"].ToString().ToUpper().Contains("D"))
                {
                    drNewSunday = CopyAsNewRow(drTrip);
                    drNewSunday["id"] = drTrip["id"].ToString() + "-D";
                    drNewSunday["service_id"] = "__D";
                    dtTrips.Rows.Add(drNewSunday);
                }

                drTrip["service_id"] = "U__";
            }

            foreach (DataRow drTrip in dtTrips.Rows)
            {
                drTrip["id_stop_time"] = drTrip["id"].ToString().Replace("-S", "").Replace("-D", "");
            }

            UpdateTripDataTable(dtTrips);
        }

        private void UpdateTripDataTable(DataTable dtTrips)
        {
            var updateCommand = _sqliteHelper.CreateUpdateComand("trip", "id");
            var insertCommand = _sqliteHelper.CreateInsertComand("trip");
            _sqliteHelper.UpdateDataTable(updateCommand, insertCommand, dtTrips);
        }

        private DataTable GetTripDataTable()
        {
            var sql = "SELECT t.* FROM trip t INNER JOIN route r ON t.route_id = r.id WHERE r.route_type=3";
            var dtTrips = _sqliteHelper.GetDataTable(sql);
            return dtTrips;
        }

        private void TripAlterTable()
        {
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE trip ADD COLUMN  id_stop_time TEXT");
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE trip ADD COLUMN  trip_time_morning INT");
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE trip ADD COLUMN  trip_time_noon INT");
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE trip ADD COLUMN  trip_time_evening INT");
        }

        #endregion

        #region Route Table

        private void RouteUpdateAll()
        {
            var lineNumbers = _allLinesFromCsvFile.Select(l => l.LineNumber).Distinct().OrderBy(x => x);

            RouteAlterTable();

            foreach (var lineNumber in lineNumbers)
            {
                var lineDetails = _allLinesByLineNumber[lineNumber].First();

                RouteUpdate(lineDetails);
            }
        }

        private void RouteAlterTable()
        {
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE route ADD COLUMN  route_city_area INT");
            _sqliteHelper.ExecuteNonQuery("ALTER TABLE route ADD COLUMN  route_company TEXT");
        }

        private void RouteUpdate(SptransLineDetails lineDetails)
        {
            var sql = "UPDATE route SET route_city_area = :route_city_area ,  route_company = :route_company  WHERE id = :id ";
            var parameters = new List<SQLiteParameter>();
            parameters.Add(new SQLiteParameter("route_city_area", lineDetails.CityArea));
            parameters.Add(new SQLiteParameter("route_company", lineDetails.Company));
            parameters.Add(new SQLiteParameter("id", lineDetails.LineNumber));
            var resp = _sqliteHelper.ExecuteNonQuery(sql, parameters.ToArray());
        }

        #endregion

        private DataRow CopyAsNewRow(DataRow drSource)
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
