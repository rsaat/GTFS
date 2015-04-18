namespace GTFS.Sptrans.Tool.CSV
{
    /// <summary>
    /// Dire��o da p�gina de detalhes
    /// </summary>
    public enum DetaislDirection
    {
        /// <summary>
        ///   Dire��o para o ponto inicial - Equivalente dire��o normal 0 do GTFS
        /// </summary>
        DirectionToInitialStop = 1,

        /// <summary>
        ///   Dire��o para o ponto final - Equivalente dire��o oposta 1 do GTFS
        /// </summary>
        DirectionToFinalStop = 2
    }
}