namespace Business.Interfaces.WebModels.ProjectCosts
{
    /// <summary>
    /// Model udostępnionego kosztu projektu
    /// </summary>
    public record SharedProjectCostWeb
    {
        public Guid Id { get; init; }
        public Guid ProjectCostId { get; init; }
        public Guid SharedWithUserId { get; init; }
        public string SharedWithUserName { get; init; } = string.Empty;
        public Guid SharedByUserId { get; init; }
        public string SharedByUserName { get; init; } = string.Empty;
        public DateTime SharedAt { get; init; }
        
        // Cost details
        public string CostName { get; init; } = string.Empty;
        public string? CostPlace { get; init; }
        public DateTime CostDate { get; init; }
        public string? CostDescription { get; init; }
        public decimal? CostNetAmount { get; init; }
        public decimal? CostVatRate { get; init; }
        public decimal CostGrossAmount { get; init; }
        public bool CostIsClosed { get; init; }
        public bool CostHasDocument { get; init; }
        public string? CostDocumentFileName { get; init; }
        public string? PreviewSasUrl { get; init; }
        public string? DownloadSasUrl { get; init; }
    }
}
