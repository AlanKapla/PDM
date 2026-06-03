namespace Business.Interfaces.WebModels.AI
{
    /// <summary>
    /// Podgląd kosztorysu wygenerowanego przez AI.
    /// Wyłącznie w pamięci — nie jest zapisywany do bazy danych.
    /// Użytkownik zatwierdza ten podgląd, po czym wysyła go do CreateCostEstimateFromAIPreview.
    /// </summary>
    public sealed record AICostEstimatePreviewWeb
    {
        /// <summary>ID szablonu (powtórzony dla walidacji po stronie klienta).</summary>
        public Guid TemplateId { get; init; }

        /// <summary>Sugerowana nazwa kosztorysu.</summary>
        public string SuggestedName { get; init; } = string.Empty;

        /// <summary>Sugerowany opis kosztorysu.</summary>
        public string? SuggestedDescription { get; init; }

        /// <summary>Grupy kosztorysu z zagnieżdżonymi pozycjami.</summary>
        public List<AIGroupPreviewWeb> Groups { get; init; } = [];

        /// <summary>Ostrzeżenia wygenerowane przez AI (np. pole pominięte bo brak danych).</summary>
        public List<string> Warnings { get; init; } = [];
    }

    public sealed record AIGroupPreviewWeb
    {
        /// <summary>Tymczasowe ID (np. "g1", "g2") — do mapowania relacji parent/child po stronie klienta.</summary>
        public string TempId { get; init; } = string.Empty;

        /// <summary>TempId grupy nadrzędnej lub null dla grup root.</summary>
        public string? ParentTempId { get; init; }

        /// <summary>Nazwa grupy.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Kolejność w ramach poziomu.</summary>
        public int Order { get; init; }

        /// <summary>Wartości pól grupy (GroupHeaderFields z szablonu).</summary>
        public List<AIFieldValueWeb> FieldValues { get; init; } = [];

        /// <summary>Pozycje kosztorysowe w tej grupie.</summary>
        public List<AIItemPreviewWeb> Items { get; init; } = [];
    }

    public sealed record AIItemPreviewWeb
    {
        /// <summary>Tymczasowe ID pozycji.</summary>
        public string TempId { get; init; } = string.Empty;

        /// <summary>Nazwa pozycji.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Kolejność w grupie.</summary>
        public int Order { get; init; }

        /// <summary>Wartości pól pozycji (SystemFields + CalculatedFields + GenericFields z szablonu).</summary>
        public List<AIFieldValueWeb> FieldValues { get; init; } = [];

        /// <summary>
        /// Komponenty (składniki) pozycji — np. Beton, Pompa do betonu, Robocizna.
        /// Gdy lista jest niepusta, pozycja główna NIE powinna mieć własnych FieldValues —
        /// wartości (Ilość, Cena) ustawia się na komponentach.
        /// </summary>
        public List<AIComponentPreviewWeb> Components { get; init; } = [];
    }

    /// <summary>
    /// Składnik (komponent) pozycji kosztorysu.
    /// Odpowiada CostEstimateItem z RelationType = Component.
    /// </summary>
    public sealed record AIComponentPreviewWeb
    {
        /// <summary>Tymczasowe ID komponentu.</summary>
        public string TempId { get; init; } = string.Empty;

        /// <summary>Nazwa komponentu (np. "Beton B25", "Robocizna – wylewanie").</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Kolejność wśród komponentów pozycji.</summary>
        public int Order { get; init; }

        /// <summary>Wartości pól komponentu (te same pola co pozycja: Ilość, Jednostka, Cena).</summary>
        public List<AIFieldValueWeb> FieldValues { get; init; } = [];
    }

    /// <summary>
    /// Wartość pojedynczego pola wygenerowana przez AI.
    /// Tylko jedno z pól wartości powinno być wypełnione — zgodnie z FieldType.
    /// </summary>
    public sealed record AIFieldValueWeb
    {
        /// <summary>ID definicji pola z szablonu (CostEstimateTemplateFieldDefinitionBase.Id).</summary>
        public Guid FieldDefinitionId { get; init; }

        public decimal? DecimalValue { get; init; }
        public string? StringValue { get; init; }
        public bool? BoolValue { get; init; }
        public DateTime? DateTimeValue { get; init; }
    }
}
