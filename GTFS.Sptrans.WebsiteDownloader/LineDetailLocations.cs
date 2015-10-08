using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GTFS.Sptrans.WebsiteDownloader
{
    public class LineDetailLocations
    {
        private string _baseAddress;

        public LineDetailLocations(string baseAddress, string detailPagePath, string apiPath)
        {
            _baseAddress = baseAddress;
            DetailPagePath = detailPagePath;
            ApiPath = apiPath;

            FindLineNumber(apiPath);

            FindLineDetailsId(detailPagePath);
        }

        private void FindLineNumber(string apiPath)
        {
            var uri = new System.Uri(apiPath);

            LineNumber = uri.Segments.Last();
        }

        private void FindLineDetailsId(string detailPagePath)
        {
            var regex = new Regex("CdPjOID=(\\d+)",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            var m = regex.Matches(detailPagePath);

            if (m.Count <= 0)
            {
                throw new InvalidOperationException("CdPjOID parameter not found in url " + detailPagePath);
            }

            if (m[0].Groups.Count <= 0)
            {
                throw new InvalidOperationException("CdPjOID parameter not found in url " + detailPagePath);
            }

            CodeLineDetails = m[0].Groups[1].Value;


        }


        public string DetailsUrlWeekDay(DetaislDirection direction)
        {
            return DetailsUrl(DetailsDepartureDay.DetailsDeparturesWeekDay,
                DetailsItineraryDay.DetailsItineraryWeekDay,
                direction);
        }

        public string DetailsUrlSaturday(DetaislDirection direction)
        {
            return DetailsUrl(DetailsDepartureDay.DetailsDeparturesSaturday,
                DetailsItineraryDay.DetailsItinerarySaturday,
                direction);
        }

        public string DetailsUrlSunday(DetaislDirection direction)
        {
            return DetailsUrl(DetailsDepartureDay.DetailsDeparturesSunday,
                DetailsItineraryDay.DetailsItinerarySunday,
                direction);
        }

        public string DetailsUrl(DetailsDepartureDay departureDay, DetailsItineraryDay itineraryDay, DetaislDirection direction)
        {

            var url = _baseAddress + string.Format("detalheLinha.asp?TpDiaID={0}&CdPjOID={1}&TpDiaIDpar={2}&DfSenID={3}", (int)itineraryDay, CodeLineDetails, (int)departureDay, (int)direction);
            return url;
        }

        public string DetailPagePath { get; set; }
        public string ApiPath { get; set; }
        /// <summary>
        ///     Número do ID da página de detalhes
        /// </summary>
        public string CodeLineDetails { get; set; }

        /// <summary>
        ///    Número da linha 
        /// </summary>
        public string LineNumber { get; set; }
    }
}