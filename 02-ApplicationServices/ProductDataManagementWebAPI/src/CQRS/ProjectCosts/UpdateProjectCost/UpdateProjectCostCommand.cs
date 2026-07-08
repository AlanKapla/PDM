using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.ProjectCosts.UpdateProjectCost
{
    /// <summary>
    /// Command do aktualizacji kosztu projektu
    /// </summary>
    public sealed record UpdateProjectCostCommand : IRequestCommand<ProjectCostListItemWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid CostId { get; init; }
        public required string Name { get; init; }
        public Guid? ContractorId { get; init; }
        public Guid? CategoryId { get; init; }
        public string? Number { get; init; }
        public DateTime? Date { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public IFormFile? Document { get; init; }
        public IFormFile? UpdatedDocument { get; init; }
        public bool RemoveDocument { get; init; }

        public string PermissionCode => PermissionCodes.ProjectCosts;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
