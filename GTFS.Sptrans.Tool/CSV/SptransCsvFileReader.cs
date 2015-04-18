using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFS.Sptrans.Tool.Common;

namespace GTFS.Sptrans.Tool.CSV
{
    public class SptransCsvFileReader
    {
        private readonly string _csvFileName;

        public SptransCsvFileReader(string csvFileName)
        {
            _csvFileName = csvFileName;
        }

        public enum DataColum
        {
            ColLineNumber = 0,
            ColLineDirection = 1,
            ColLineOperationDay = 2,
            ColLineName = 3,
            ColTimeDurationMorning = 4,
            ColTimeDurationNoon = 5,
            ColTimeDurationEvening = 6,
            ColCityArea = 7,
            ColCompany = 8,
            ColItinerary = 9,
            ColDepartures = 10,

        }

        public IList<SptransLineDetails> GetAll()
        {
            var lines = File.ReadAllLines(_csvFileName);

            var sptransLines = new List<SptransLineDetails>();

            foreach (var line in lines)
            {
                var dataRow = line.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                var lineDetail = new SptransLineDetails();
                lineDetail.LineNumber = RemoveDoubleQuotes(dataRow[(int)DataColum.ColLineNumber]);
                lineDetail.DetaislDirection = (DetaislDirection)Enum.Parse(typeof(DetaislDirection), RemoveDoubleQuotes(dataRow[(int)DataColum.ColLineDirection]));
                lineDetail.DetailsItineraryDay = (DetailsItineraryDay)Enum.Parse(typeof(DetailsItineraryDay), RemoveDoubleQuotes(dataRow[(int)DataColum.ColLineOperationDay]));
                lineDetail.LineName = RemoveDoubleQuotes(dataRow[(int)DataColum.ColLineName]);
                lineDetail.TimeDurationMorning = Convert.ToInt32(RemoveDoubleQuotes(dataRow[(int)DataColum.ColTimeDurationMorning]));
                lineDetail.TimeDurationNoon = Convert.ToInt32(RemoveDoubleQuotes(dataRow[(int)DataColum.ColTimeDurationNoon]));
                lineDetail.TimeDurationEvening = Convert.ToInt32(RemoveDoubleQuotes(dataRow[(int)DataColum.ColTimeDurationEvening]));
                lineDetail.CityArea = RemoveDoubleQuotes(dataRow[(int)DataColum.ColCityArea]);
                lineDetail.Company = RemoveDoubleQuotes(dataRow[(int)DataColum.ColCompany]);


                var itineraryText = RemoveDoubleQuotes(dataRow[(int)DataColum.ColItinerary]);
                var streets = itineraryText.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                lineDetail.Itinerary = streets.Select(FormatStreetNumbers).ToList();

                var departureText = RemoveDoubleQuotes(dataRow[(int)DataColum.ColDepartures]);
                var departures = departureText.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                lineDetail.Departures = departures.Select(x => x.Trim()).ToList(); ;

                sptransLines.Add(lineDetail);
            }

            return sptransLines;
        }

        private string FormatStreetNumbers(string itineraryText)
        {
            itineraryText = itineraryText.Trim().ToUpper();
            itineraryText = itineraryText.Replace(",", "");
            itineraryText = Strings.RemoveDiacritics(itineraryText);
            var tokens = itineraryText.Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
            itineraryText = tokens[0] + " , " + tokens[1];
            return itineraryText;
        }

        string RemoveDoubleQuotes(string text)
        {
            return text.Replace("\"", "");
        }


    }
}
