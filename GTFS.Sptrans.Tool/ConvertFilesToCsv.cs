using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GTFS.Sptrans.WebsiteDownloader;

namespace GTFS.Sptrans.Tool
{
    public class FileConvertertedEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public float PercentComplete { get; set; }
    }

    public class ConvertFilesToCsv
    {

        public event EventHandler<FileConvertertedEventArgs> FileConverted;

        protected virtual void OnFileConverted(FileConvertertedEventArgs e)
        {
            var handler = FileConverted;
            if (handler != null) handler(this, e);
        }
 
        private readonly string _spTransFilePath;
        public readonly string _outCsvFileName;
        private Dictionary<string, DetailsDepartureDay> _departureDay;
        private Dictionary<string, DetailsItineraryDay> _itineraryDay;

        public ConvertFilesToCsv(string spTransFilePath, string outCsvFileName)
        {
            _outCsvFileName = outCsvFileName;
            _spTransFilePath = spTransFilePath;

            _departureDay = new Dictionary<string, DetailsDepartureDay>();
            _departureDay.Add("DOM", DetailsDepartureDay.DetailsDeparturesSunday);
            _departureDay.Add("SAB", DetailsDepartureDay.DetailsDeparturesSaturday);
            _departureDay.Add("U", DetailsDepartureDay.DetailsDeparturesWeekDay);

            _itineraryDay = new Dictionary<string, DetailsItineraryDay>();
            _itineraryDay.Add("DOM", DetailsItineraryDay.DetailsItinerarySunday);
            _itineraryDay.Add("SAB", DetailsItineraryDay.DetailsItinerarySaturday);
            _itineraryDay.Add("U", DetailsItineraryDay.DetailsItineraryWeekDay);

        }

        public void Convert()
        {

            var files = Directory.GetFiles(_spTransFilePath, "trip*.html").OrderBy(x=>x).ToArray();

            //test single file 
            //var files = new string[] {@"\\192.168.1.1\hdexterno\temp\sptrans\sptrans_html\trip_1016-10-0_DOM.html"};

            var csvFileText = new StringBuilder();
            int i = 0;
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                var tokens = fileName.Split(new char[]{'_'}, StringSplitOptions.RemoveEmptyEntries);
                var tripId = tokens[1];
                var dayOfWeek = tokens[2];

                var text = File.ReadAllText(file, Encoding.GetEncoding("iso-8859-1"));

                var detaisDirection = tripId.EndsWith("0")
                    ? DetaislDirection.DirectionToInitialStop
                    : DetaislDirection.DirectionToFinalStop;

                var _parser = new SptransLineDetailsParser(text, detaisDirection, _departureDay[dayOfWeek], _itineraryDay[dayOfWeek]);
                
                _parser.ParseLineDetailHtml();
                
                var csvRow =  ConvertSingleRow(_parser);

                if (!string.IsNullOrEmpty(csvRow))
                {
                    csvFileText.AppendLine(csvRow);    
                }
                
                i++;

                OnFileConverted( new FileConvertertedEventArgs() { FileName = fileName, PercentComplete = 100.0F*i/files.Length});

            }

            File.WriteAllText(_outCsvFileName, csvFileText.ToString(),Encoding.UTF8);

        }

        string ConvertSingleRow(SptransLineDetailsParser parser)
        {
            var csvRow = new StringBuilder();
            const string columnFormat = "\"{0}\";";

            if (parser.Departures.Count <= 0)
            {
                return "";
            }

            csvRow.AppendFormat(columnFormat, parser.LineNumber);
            csvRow.AppendFormat(columnFormat, (int)parser.DetaislDirection);
            csvRow.AppendFormat(columnFormat, (int)parser.DetailsDeparturesWeekDay);
            csvRow.AppendFormat(columnFormat, parser.LineName);
            csvRow.AppendFormat(columnFormat, parser.TimeDurationMorning);
            csvRow.AppendFormat(columnFormat, parser.TimeDurationMidleDay);
            csvRow.AppendFormat(columnFormat, parser.TimeDurationAfernoon);
            csvRow.AppendFormat(columnFormat, parser.CityArea);
            csvRow.AppendFormat(columnFormat, parser.Company);
            csvRow.AppendFormat(columnFormat, ConvertIntineraryToCsv(parser.Itinerary));
            csvRow.AppendFormat(columnFormat, ConvertDeparturesToCsv(parser.Departures));
            return csvRow.ToString();
        }

        private string ConvertDeparturesToCsv(IEnumerable<string> departures)
        {
            var csv = new StringBuilder();

            foreach (var departure in departures)
            {
                csv.AppendFormat("{0}|", departure);
            }
            return csv.ToString();
        }

        private string ConvertIntineraryToCsv(IEnumerable<SptransLineDetailStreet> itinerary)
        {
             var csv = new StringBuilder();

            foreach (var street in itinerary)
            {
                csv.AppendFormat("{0}#{1}#|", street.StreetName, street.StreetNumberRange);
            }
            return csv.ToString();
        }
    }
}

