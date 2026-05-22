namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Pozycja kosztorysu dostępna do powiązania z kosztem.
    /// Ścieżka: NazwaKosztorysu > NazwaGrupy > ... > NazwaPozycji
    /// </summary>
    public sealed record EstimateItemLinkOptionWeb
    {
        public required Guid ItemId { get; init; }
        public required string Path { get; init; }

        /// <summary>
        /// ID zakresu pracy spiętego z tą pozycją. Null gdy brak spięcia.
        /// </summary>
        public Guid? LinkedWorkId { get; init; }
    }

    /// <summary>
    /// Zakres pracy dostępny do powiązania z kosztem.
    /// Ścieżka: NazwaHarmonogramu > NazwaEtapu > ... > NazwaZakresu
    /// </summary>
    public sealed record WorkLinkOptionWeb
    {
        public required Guid WorkId { get; init; }
        public required string Path { get; init; }

        /// <summary>
        /// ID pozycji kosztorysu spiętej z tym zakresem. Null gdy brak spięcia.
        /// </summary>
        public Guid? LinkedItemId { get; init; }
    }

    /// <summary>
    /// Dostępne opcje powiązania kosztu z pozycją kosztorysu lub zakresem pracy.
    /// </summary>
    public sealed record CostLinkOptionsWeb
    {
        public required List<EstimateItemLinkOptionWeb> EstimateItems { get; init; }
        public required List<WorkLinkOptionWeb> WorkItems { get; init; }
    }
}
