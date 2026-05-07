using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Microsoft.AspNetCore.Http;

namespace CQRS.ProjectCosts.CreateProjectCost
{
    /// <summary>
    /// Command do tworzenia nowego kosztu projektu
    /// </summary>
    public record CreateProjectCostCommand : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Place { get; init; }
        public DateTime Date { get; init; }
        public string? Description { get; init; }
        public decimal? NetAmount { get; init; }
        public decimal? GrossAmount { get; init; }
        public bool IsAccepted { get; init; }
        public IFormFile? Document { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
