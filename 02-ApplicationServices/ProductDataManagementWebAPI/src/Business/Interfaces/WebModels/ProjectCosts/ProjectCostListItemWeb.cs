namespace Business.Interfaces.WebModels.ProjectCosts
{
    /// <summary>
    /// Uproszczony model kosztu projektu dla listy
    /// </summary>
    public record ProjectCostListItemWeb
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Place { get; init; }
        public DateTime Date { get; init; }
        public string? Description { get; init; }
        public decimal? NetAmount { get; init; }
        public decimal? GrossAmount { get; init; }
        public bool IsClosed { get; init; }
        public bool HasDocument { get; init; }
        public string? DocumentFileName { get; init; }
        public string? PreviewSasUrl { get; init; }
        public string? DownloadSasUrl { get; init; }
        public List<Guid> SharedWithUserIds { get; init; } = new();
        public DateTime CreatedAt { get; init; }
    }
}
