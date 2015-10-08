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
            var createDownloadFileScript = false;

            if (createDownloadFileScript)
            {
                var downloader = new SptransWebsiteDownloader();
                downloader.CreateFileList();
                Console.WriteLine("Enviar arquivos download.sh sptrans_details_file_list.txt para fazer download no roteador. ");
                Console.WriteLine("Aperte uma tecla");
                Console.ReadKey();
                return;
            }

            var executeConversionSptransFilesToCsv = false;

            if (executeConversionSptransFilesToCsv)
            {
                var sptransFilesDirectory = @"\\Vboxsvr\d_drive\temp\sptrans\sptrans_html";
                var outfile = @"\\Vboxsvr\d_drive\temp\sptrans\sptrans_html\sptranslines.csv";

                Console.WriteLine("Reading file names ...");

                var convert = new ConvertFilesToCsv(sptransFilesDirectory, outfile);
                convert.FileConverted += convert_FileConverted;
                convert.Convert();
                return;
            }


            var convertGtsfToSqlite = false;
            var sptransSqliteDbFile = @"H:\netprojects\GTFS\GTFS.SPTrans.FeedDB\gtfssptrans.db";
            var sptransFeedDirectory = @"H:\netprojects\GTFS\GTFS.Sptrans.Feed";
            
            if (convertGtsfToSqlite)
            {
                FeedConverter.ConvertFeedToSqliteDb(sptransSqliteDbFile, sptransFeedDirectory);
            }
            
            var customizeDb = new CustomizeSqliteDb(sptransSqliteDbFile, sptransFeedDirectory);
            customizeDb.ExecuteCustomizations();

        }

        static void convert_FileConverted(object sender, FileConvertertedEventArgs e)
        {
            Console.Write("\r --> Converting " + string.Format("{0:00.0}", e.PercentComplete) + "% " + e.FileName);
        }



    }
}
