using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using Microsoft.AspNetCore.Http;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    /// <summary>
    /// Command do tworzenia nowego kosztu projektu
    /// </summary>
    public sealed record CreateProjectCostCommand : IRequestCommand<ProjectCostListItemWeb>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public required string Name { get; init; }
        public Guid? ContractorId { get; init; }
        public string? Number { get; init; }
        public DateTime? Date { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public bool IsAccepted { get; init; }
        public IFormFile? Document { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
