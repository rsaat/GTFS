using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFS.DB;
using GTFS.DB.SQLite;
using GTFS.IO;

namespace GTFS.Sptrans.Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            var sptransSqliteDbFile = @"H:\netprojects\GTFS\GTFS.SPTrans.FeedDB\gtfssptrans.db";
            var sptransFeedDirectory = @"H:\netprojects\GTFS\GTFS.Sptrans.Feed";
     
            //FeedConverter.ConvertFeedToSqliteDb(sptransSqliteDbFile, sptransFeedDirectory);

            var customizeDb = new CustomizeSqliteDb(sptransSqliteDbFile, sptransFeedDirectory);
            customizeDb.ExecuteCustomizations();

        }
    }
}
