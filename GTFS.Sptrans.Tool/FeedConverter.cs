using System.IO;
using GTFS.DB;
using GTFS.DB.SQLite;
using GTFS.IO;

namespace GTFS.Sptrans.Tool
{
    class FeedConverter
    {
        public static void ConvertFeedToSqliteDb(string sptransSqliteDbFile, string sptransFeedDirectory)
        {
            if (File.Exists(sptransSqliteDbFile))
            {
                File.Delete(sptransSqliteDbFile);
            }

            string sqliteConnectionString = string.Format(@"Data Source={0};Version=3;", sptransSqliteDbFile);
            // get test db.
            var db = CreateDB(sqliteConnectionString);

            // build test feed.
            var feed = CreateSptransFeedFromTextFiles(sptransFeedDirectory);

            // add to db.
            db.AddFeed(feed);

            
        }

        private static IGTFSFeedDB CreateDB(string connectionString)
        {
            return new SQLiteGTFSFeedDB(connectionString);
        }

        private static IGTFSFeed CreateSptransFeedFromTextFiles(string sptransFeedDirectory)
        {

            // create the reader.
            var reader = new GTFSReader<GTFSFeed>(false);

            // execute the reader.
            var feed = reader.Read(new GTFSDirectorySource(new DirectoryInfo(sptransFeedDirectory)));

            return feed;
        }
    }
}