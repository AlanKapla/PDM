namespace Business.Interfaces.WebModels.AI
{
    public sealed record ParsedCostDto
    {
        /// Nazwa kosztu — co zostało zakupione (np. "Materiały budowlane")
        public string Name { get; init; } = string.Empty;

        /// Rozszerzony opis z detalami pozycji
        public string? Description { get; init; }

        /// Numer faktury/rachunku
        public string? Number { get; init; }

        /// Suma netto całego dokumentu
        public decimal? Net { get; init; }

        /// Suma brutto całego dokumentu
        public decimal? Gross { get; init; }

        /// Data wystawienia dokumentu (ISO 8601)
        public DateTime? Date { get; init; }

        /// GUID kontrahenta — wypełniony tylko gdy ContractorFound = true
        public Guid? ContractorId { get; init; }

        /// Nazwa kontrahenta wyciągnięta z dokumentu
        public string? ContractorName { get; init; }

        /// NIP kontrahenta wyciągnięty z dokumentu
        public string? ContractorNip { get; init; }

        /// Adres kontrahenta wyciągnięty z dokumentu
        public string? ContractorAddress { get; init; }

        /// Czy kontrahent znaleziony w bazie danych
        public bool ContractorFound { get; init; }

        /// Sugestia nowego kontrahenta gdy ContractorFound = false
        public SuggestedContractorDto? SuggestedContractor { get; init; }

        /// Pewność AI (0.0 – 1.0)
        public double Confidence { get; init; }

        /// Surowy tekst z dokumentu (do debugowania)
        public string? RawText { get; init; }
    }

    public sealed record SuggestedContractorDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Nip { get; init; }
        public string? Address { get; init; }
    }
}
