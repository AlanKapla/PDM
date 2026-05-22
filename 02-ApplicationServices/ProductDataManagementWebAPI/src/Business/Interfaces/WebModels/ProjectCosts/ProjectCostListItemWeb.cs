namespace Business.Interfaces.WebModels.ProjectCosts
{
    /// <summary>
    /// Model kosztu projektu (lista oraz odpowiedź Create/Update)
    /// </summary>
    public sealed record ProjectCostListItemWeb
    {
        public required Guid Id { get; init; }
        public required Guid UserId { get; init; }
        public required string UserName { get; init; }
        public required string Name { get; init; }
        public Guid? ContractorId { get; init; }
        public string? ContractorName { get; init; }
        public string? Number { get; init; }
        public DateTime? Date { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public bool IsAccepted { get; init; }
        public bool HasDocument { get; init; }
        public string? DocumentFileName { get; init; }
        public string? PreviewSasUrl { get; init; }
        public string? DownloadSasUrl { get; init; }
        public required IReadOnlyList<Guid> SharedWithUserIds { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
