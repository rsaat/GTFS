using System.Collections.Generic;
using GTFS.Sptrans.Tool.CSV;

namespace GTFS.Sptrans.Tool
{
    /// <summary>
    ///    Detalhes da linha da SPtrans no site 
    /// </summary>
    public class SptransLineDetails
    {
        public DetaislDirection DetaislDirection { get;set; }
        public DetailsItineraryDay DetailsItineraryDay { get;set; }
        public string LineNumber { get;set; }
        public string LineName { get;set; }
        public string CityArea { get;set; }
        public string Company { get;set; }
        public int TimeDurationMorning { get;set; }
        public int TimeDurationNoon { get;set; }
        public int TimeDurationEvening { get;set; }

        public IList<string> Departures { get; set; }

        public IList<string> Itinerary { get; set; }

        public string GtfsTripId
        {
            get
            {
                var gtfsTrip = LineNumber.ToUpper();

                var gtfsDirections = new Dictionary<DetaislDirection, string>();
                gtfsDirections.Add(DetaislDirection.DirectionToInitialStop, "0");
                gtfsDirections.Add(DetaislDirection.DirectionToFinalStop, "1");

                var gtfsDays = new Dictionary<DetailsItineraryDay, string>();
                gtfsDays.Add(DetailsItineraryDay.DetailsItineraryWeekDay,"U");
                gtfsDays.Add(DetailsItineraryDay.DetailsItinerarySaturday,"S");
                gtfsDays.Add(DetailsItineraryDay.DetailsItinerarySunday,"D");

                var dayOfWeekPart = "";
                if (this.DetailsItineraryDay!=DetailsItineraryDay.DetailsItineraryWeekDay)
                {
                    dayOfWeekPart = "-" + gtfsDays[DetailsItineraryDay];
                }

                return gtfsTrip + "-" + gtfsDirections[this.DetaislDirection] + dayOfWeekPart;
            }
        }

    }
}