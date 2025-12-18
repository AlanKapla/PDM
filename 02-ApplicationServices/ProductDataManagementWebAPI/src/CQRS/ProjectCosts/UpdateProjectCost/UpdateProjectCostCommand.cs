using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    /// <summary>
    /// Command do aktualizacji kosztu projektu
    /// </summary>
    public record UpdateProjectCostCommand : IRequestCommand<Unit>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Place { get; init; }
        public DateTime Date { get; init; }
        public string? Description { get; init; }
        public decimal? NetAmount { get; init; }
        public decimal? VatRate { get; init; }
        public decimal? GrossAmount { get; init; }
        public bool IsClosed { get; init; }
        public IFormFile? Document { get; init; }
        public bool RemoveDocument { get; init; }
    }
}
