namespace GTFS.Sptrans.Tool.CSV
{
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
}