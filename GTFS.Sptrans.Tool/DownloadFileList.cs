using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFS.Sptrans.WebsiteDownloader;

namespace GTFS.Sptrans.Tool
{
    public class DownloadFileList
    {
        private readonly IEnumerable<LineDetailLocations> _lineDetailLocations;

        public DownloadFileList(IEnumerable<LineDetailLocations> lineDetailLocations)
        {
            _lineDetailLocations = lineDetailLocations;
        }

        public void CreteFileList(string filePath)
        {
            var fileLines = new StringBuilder();

            foreach (var lineDetailLocation in _lineDetailLocations)
            {
                var spTransToGtfsDirection = new Dictionary<DetaislDirection, int>();
                const int gtfsDirectionNormal = 0;
                const int gtfsDirectionOposite = 1;
                spTransToGtfsDirection.Add(DetaislDirection.DirectionToInitialStop, gtfsDirectionNormal);
                spTransToGtfsDirection.Add(DetaislDirection.DirectionToFinalStop, gtfsDirectionOposite);

                foreach (var sptransDirection in spTransToGtfsDirection.Keys)
                {
                    //dia util 
                    var tripName = "trip_" + lineDetailLocation.LineNumber + "-" +
                               (spTransToGtfsDirection[sptransDirection]);
                    var fileName = tripName + "_U.html";

                    var fileLine = lineDetailLocation.DetailsUrlWeekDay(sptransDirection) + " " + fileName;
                    fileLines.AppendLine(fileLine);

                    //sabado 
                    tripName = "trip_" + lineDetailLocation.LineNumber + "-" +
                               (spTransToGtfsDirection[sptransDirection]);
                    fileName = tripName + "_SAB.html";

                    fileLine = lineDetailLocation.DetailsUrlSaturday(sptransDirection) + " " + fileName;
                    fileLines.AppendLine(fileLine);


                    //domingo  
                    tripName = "trip_" + lineDetailLocation.LineNumber + "-" +
                              (spTransToGtfsDirection[sptransDirection]);
                    fileName = tripName + "_DOM.html";

                    fileLine = lineDetailLocation.DetailsUrlSunday(sptransDirection) + " " + fileName;
                    fileLines.AppendLine(fileLine);

                }

            }

            var fileText = fileLines.ToString();

            fileText = fileText.Replace("\r\n", "\n");

            System.IO.File.WriteAllText(filePath, fileText, Encoding.Default);

        }

    }
}
