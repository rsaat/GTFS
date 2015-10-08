using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GTFS.Sptrans.WebsiteDownloader
{
    /// <summary>
    ///    Constante Definindo dia partida 
    /// </summary>
    public enum DetailsDepartureDay
    {
        DetailsDeparturesWeekDay = 0,
        DetailsDeparturesSaturday = 1,
        DetailsDeparturesSunday = 2,
    }

    /// <summary>
    /// Direção da página de detalhes
    /// </summary>
    public enum DetaislDirection
    {
        /// <summary>
        ///   Direção para o ponto inicial - Equivalente direção normal 0 do GTFS
        /// </summary>
        DirectionToInitialStop = 1,

        /// <summary>
        ///   Direção para o ponto final - Equivalente direção oposta 1 do GTFS
        /// </summary>
        DirectionToFinalStop = 2
    }

    /// <summary>
    ///    Constante Itinerario dia partida 
    /// </summary>
    public enum DetailsItineraryDay
    {
        DetailsItineraryWeekDay = 0,
        DetailsItinerarySaturday = 1,
        DetailsItinerarySunday = 2,
    }

}
