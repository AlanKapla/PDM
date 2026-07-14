using Entities.Models.Costs;

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
        public Guid? CategoryId { get; init; }
        public string? CategoryName { get; init; }
        public string? CategoryColor { get; init; }
        public string? Number { get; init; }
        public DateTime? Date { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public required CostApprovalStatus ApprovalStatus { get; init; }
        public Guid? ApprovedByUserId { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public bool HasDocument { get; init; }
        public string? DocumentFileName { get; init; }
        public string? PreviewSasUrl { get; init; }
        public string? DownloadSasUrl { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
