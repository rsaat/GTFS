using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;

namespace GTFS.Sptrans.WebsiteDownloader
{
    public class SptransLineDetailsParser
    {
        private readonly string _lineDetaisHtml;
        private IDocument _docHtmlParser;
        private DetaislDirection _detaislDirection;
        private DetailsDepartureDay _detailsDeparturesWeekDay;
        private DetailsItineraryDay _detailsItineraryWeekDay;
        private IList<SptransLineDetailStreet> _itinerary;
        private IList<string> _departures;

        public void ParseLineDetailHtml()
        {
            ParseLineIdentification();
            _itinerary = ParseItinerary();
            _departures = ParseDepartures();
            ParseTripDurations();
        }

        public IList<SptransLineDetailStreet> Itinerary
        {
            get { return _itinerary; }
        }

        public IList<string> Departures
        {
            get { return _departures; }
        }

        #region Line Details


        public SptransLineDetailsParser(string lineDetaisHtml, DetaislDirection detaislDirection, DetailsDepartureDay detailsDeparturesWeekDay, DetailsItineraryDay detailsItineraryWeekDay)
        {
            DetailsItineraryWeekDay = detailsItineraryWeekDay;
            _detailsDeparturesWeekDay = detailsDeparturesWeekDay;
            _detaislDirection = detaislDirection;
            _lineDetaisHtml = lineDetaisHtml;
            _docHtmlParser = DocumentBuilder.Html(_lineDetaisHtml);

        }

        public string LineNumber { get; private set; }
        public string LineName { get; private set; }
        public string CityArea { get; private set; }
        public string Company { get; private set; }

        public void ParseLineIdentification()
        {
            var allInputs = _docHtmlParser.QuerySelectorAll("input");

            LineNumber = GetValueAttributeFromInput(allInputs, "noLinha");
            LineName = GetValueAttributeFromInput(allInputs, "nomeLinha");
            CityArea = GetValueAttributeFromInput(allInputs, "areCod");
            Company = GetValueAttributeFromInput(allInputs, "empresa");

        }

        private string GetValueAttributeFromInput(IEnumerable<IElement> allInputs, string id)
        {
            return allInputs.First(e => e.GetAttribute("id").ContainsCaseIgnored(id)).GetAttribute("value");
        }

        #endregion

        #region Itinerary

        public IList<SptransLineDetailStreet> ParseItinerary()
        {
            var allDescriptions = _docHtmlParser.QuerySelectorAll("dl").Where(e => e.GetAttribute("id").EmptyIfNull().ToLower().Contains("itinerarioLinha".ToLower()));


            var allUnorderedList = allDescriptions.First().QuerySelectorAll("ul");

            string directionText;


            if (DetaislDirection == DetaislDirection.DirectionToInitialStop)
            {
                directionText = "inicial";
            }
            else
            {
                directionText = "final";
            }

            var q = from e in allUnorderedList
                    where e.Children.Any(e1 => e1.TextContent.EmptyIfNull().ToLower().Contains(directionText))
                    select e.QuerySelector("table");

            var tableItinerary = q.Cast<IHtmlTableElement>().First();

            var streetNames = new List<SptransLineDetailStreet>();

            foreach (var row in tableItinerary.Rows)
            {
                var tableData = row.QuerySelectorAll("td").ToArray();
                var streetName = tableData[0].TextContent.Trim();
                var streetNumberRange = tableData[1].TextContent.Trim();
                streetNames.Add(new SptransLineDetailStreet(streetName, streetNumberRange));
            }

            return streetNames;

        }

        #endregion

        #region Parse Trip Durations

        public void ParseTripDurations()
        {
            var allTables = _docHtmlParser.QuerySelectorAll("table").Cast<IHtmlTableElement>().ToList();

            var tablesWithTime = allTables.First(t => t.GetAttribute("id").EmptyIfNull().ToLower().Contains("tabelaTempo".ToLower()));


            var dic = new Dictionary<DetailsDepartureDay, string>();

            dic.Add(DetailsDepartureDay.DetailsDeparturesWeekDay, "segunda");
            dic.Add(DetailsDepartureDay.DetailsDeparturesSaturday, "sábado");
            dic.Add(DetailsDepartureDay.DetailsDeparturesSunday, "domingo");


            IList<string> timesFound = new List<string>();

            timesFound = TimesFound(tablesWithTime, dic[DetailsDeparturesWeekDay].ToString());

            int timeNumber = 0;
            TimeDurationMorning = 0;
            TimeDurationMidleDay = 0;
            TimeDurationAfernoon = 0;

            if (timesFound.Count <= 0)
            {
                return;
            }

            if (Int32.TryParse(timesFound[0], out timeNumber))
            {
                TimeDurationMorning = timeNumber;
            }

            if (Int32.TryParse(timesFound[1], out timeNumber))
            {
                TimeDurationMidleDay = timeNumber;
            }

            if (Int32.TryParse(timesFound[2], out timeNumber))
            {
                TimeDurationAfernoon = timeNumber;
            }


        }

        private IList<string> TimesFound(IHtmlTableElement tablesWithTime, string dayOfWeek)
        {
            IList<string> timesFound = new List<string>();


            var rowFound = tablesWithTime.Rows.FirstOrDefault(r => r.TextContent.ToLower().Contains(dayOfWeek.ToLower()));

            if (rowFound == null)
            {
                return timesFound;
            }

            var values = rowFound.QuerySelectorAll("td").Select(e => e.TextContent.Trim()).ToArray().Skip(1);

            if (DetaislDirection == DetaislDirection.DirectionToInitialStop)
            {
                timesFound = values.Take(3).ToList();
            }
            else
            {
                timesFound = values.Skip(3).Take(3).ToList();
            }

            return timesFound;
        }

        public int TimeDurationMorning { get; private set; }
        public int TimeDurationMidleDay { get; private set; }
        public int TimeDurationAfernoon { get; private set; }

        public DetailsItineraryDay DetailsItineraryWeekDay
        {
            get { return _detailsItineraryWeekDay; }
            set { _detailsItineraryWeekDay = value; }
        }

        public DetailsDepartureDay DetailsDeparturesWeekDay
        {
            get { return _detailsDeparturesWeekDay; }
        }

        public DetaislDirection DetaislDirection
        {
            get { return _detaislDirection; }
        }

        #endregion

        #region departure times

        public IList<string> ParseDepartures()
        {


            var textAllDepartures = ExtracAllDeparturesFromHtmlDocument();

            var regex = new Regex("\\d+\\:\\d+", RegexOptions.CultureInvariant);
            var matches = regex.Matches(textAllDepartures);
            var departures = (from Match match in matches select match.Value.Trim()).ToList();
            return departures;
        }

        private string ExtracAllDeparturesFromHtmlDocument()
        {

            var allDepartures = new StringBuilder();

            var allTables = _docHtmlParser.QuerySelectorAll("table").Cast<IHtmlTableElement>().ToList();

            var tableTimes = allTables.Where(e => e.ClassName.EmptyIfNull().ToLower().Contains("tabelahorarios"));

            if (!tableTimes.Any())
            {
                throw new InvalidOperationException("tabelahorarios class html not found.");
            }

            var table = tableTimes.First();

            var tableHeaders = table.QuerySelectorAll("th").ToArray();
            var indexDepartureTimes = Array.FindIndex(tableHeaders, e => e.TextContent.ToLower().Contains("programados"));

            if (indexDepartureTimes < 0)
            {
                throw new InvalidOperationException("programados header not found.");
            }

            var rows = table.Rows;
            foreach (var row in rows)
            {
                var rowCells = row.QuerySelectorAll("td").ToList();

                if (rowCells.Count > indexDepartureTimes)
                {
                    var lineBreaks = rowCells[indexDepartureTimes].QuerySelectorAll("br");
                    foreach (var lineBreak in lineBreaks)
                    {
                        var t = _docHtmlParser.CreateTextNode(" - ");
                        lineBreak.After(t);
                    }
                    var innerHtml = rowCells[indexDepartureTimes].InnerHtml;

                    allDepartures.AppendLine(rowCells[indexDepartureTimes].TextContent);
                }
            }

            var textAllDepartures = allDepartures.ToString();


            return textAllDepartures;
        }

        #endregion
    }


    public class SptransLineDetailStreet
    {
        public SptransLineDetailStreet(string streetName, string streetNumberRange)
        {
            StreetName = streetName;
            StreetNumberRange = streetNumberRange;
        }

        public string StreetName { get; private set; }
        public string StreetNumberRange { get; private set; }
    }

}
