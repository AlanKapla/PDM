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
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostId { get; init; }
        public required string Name { get; init; }
        public Guid? ContractorId { get; init; }
        public string? Number { get; init; }
        public DateTime? Date { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public bool IsAccepted { get; init; }
        public IFormFile? Document { get; init; }
        public IFormFile? UpdatedDocument { get; init; }
        public bool RemoveDocument { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
