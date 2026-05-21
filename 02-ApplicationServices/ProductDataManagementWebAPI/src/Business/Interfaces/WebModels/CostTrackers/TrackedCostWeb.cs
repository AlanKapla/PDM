namespace Business.Interfaces.WebModels.CostTrackers
{
    public sealed record TrackedCostWeb
    {
        public required Guid Id { get; init; }
        public Guid? CostEstimateItemId { get; init; }
        public Guid? WorkScheduleStageWorkId { get; init; }
        public required bool IsAdditional { get; init; }
        public required string Name { get; init; }
        public string? Number { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public Guid? ContractorId { get; init; }
        public string? ContractorName { get; init; }
        public DateTime? Date { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public required List<TrackedCostAttachmentWeb> Attachments { get; init; }

        /// <summary>
        /// Źródło kosztu. Zawsze ustawione na podstawie powiązań kosztu.
        /// Serializowany jako string JSON.
        /// </summary>
        public required CostSourceType SourceType { get; init; }

        // --- Kontekst harmonogramu (ScheduleWorkItem / LinkedWorkItem) ---

        /// <summary>Nazwa harmonogramu. Wypełnione gdy SourceType = ScheduleWorkItem lub LinkedWorkItem.</summary>
        public string? ScheduleName { get; init; }

        /// <summary>Nazwa etapu harmonogramu. Wypełnione gdy SourceType = ScheduleWorkItem lub LinkedWorkItem.</summary>
        public string? StageName { get; init; }

        /// <summary>Nazwa zakresu prac. Wypełnione gdy SourceType = ScheduleWorkItem lub LinkedWorkItem.</summary>
        public string? WorkItemName { get; init; }

        // --- Kontekst kosztorysu (EstimateItem / LinkedWorkItem) ---

        /// <summary>Nazwa kosztorysu. Wypełnione gdy SourceType = EstimateItem lub LinkedWorkItem.</summary>
        public string? EstimateName { get; init; }

        /// <summary>Nazwa grupy kosztorysu. Wypełnione gdy SourceType = EstimateItem lub LinkedWorkItem.</summary>
        public string? EstimateGroupName { get; init; }

        /// <summary>Nazwa pozycji kosztorysu. Wypełnione gdy SourceType = EstimateItem lub LinkedWorkItem.</summary>
        public string? EstimateItemName { get; init; }

        /// <summary>Pełna ścieżka pozycji kosztorysu np. "KosztorysA > GrupaB > PozycjaC". Gotowy string do wyświetlenia.</summary>
        public string? CostEstimateItemPath { get; init; }

        /// <summary>Pełna ścieżka zakresu pracy np. "HarmonogramA > EtapB > Praca C". Gotowy string do wyświetlenia.</summary>
        public string? WorkScheduleWorkPath { get; init; }
    }
}
