namespace GTFS.Sptrans.Tool.CustomizeDatabase
{
    class CustomizeSqliteChangeDatabaseStructure : CustomizeSqliteBase
    {
        public CustomizeSqliteChangeDatabaseStructure(SqLiteDatabaseHelper sqliteHelper)
            : base(sqliteHelper)
        {

        }

        public void ExecuteCustomizations()
        {
            CreatePoiTables();

            _sqliteHelper.ExecuteNonQuery("ALTER TABLE stop ADD COLUMN  stop_geohash TEXT");
            CreateIndex("indx_stop_geohash", "stop_geohash", "stop");

            CreateIndexes();

            _sqliteHelper.ExecuteNonQuery("UPDATE stop_time set shape_dist_traveled=0");

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

        private void CreateIndex(string indexName, string indexField, string tableName)
        {
            var sql = string.Format("CREATE INDEX {0} ON {2} ({1})", indexName, indexField, tableName);
            _sqliteHelper.ExecuteNonQuery(sql);
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

    }
}