namespace Business.Interfaces.WebModels.AI
{
    /// <summary>
    /// Dane wejściowe od użytkownika do generowania kosztorysu przez AI.
    /// Wszystkie pola opcjonalne poza InvestmentType.
    /// </summary>
    public sealed record AICostEstimateRequestWeb
    {
        /// <summary>Co budujesz? (wolny tekst, np. "dom jednorodzinny 150m²", "remont mieszkania")</summary>
        public string InvestmentType { get; init; } = string.Empty;

        /// <summary>Stan wykończenia: "surowy_otwarty", "surowy_zamkniety", "deweloperski", "pod_klucz"</summary>
        public string? FinishingStandard { get; init; }

        /// <summary>Szacowany budżet brutto w PLN.</summary>
        public decimal? Budget { get; init; }

        /// <summary>Powierzchnia/zakres inwestycji (np. 150 m², 500 mb)</summary>
        public decimal? Area { get; init; }

        /// <summary>Jednostka powierzchni (np. "m²", "mb", "szt")</summary>
        public string? AreaUnit { get; init; }

        /// <summary>Lokalizacja inwestycji — wpływa na koszty robocizny.</summary>
        public string? Location { get; init; }

        /// <summary>Orientacyjny rok ukończenia inwestycji.</summary>
        public int? CompletionYear { get; init; }

        /// <summary>Dodatkowe wymagania (np. "ogrzewanie podłogowe, fotowoltaika 10kW, winda")</summary>
        public string? AdditionalRequirements { get; init; }
    }
}
