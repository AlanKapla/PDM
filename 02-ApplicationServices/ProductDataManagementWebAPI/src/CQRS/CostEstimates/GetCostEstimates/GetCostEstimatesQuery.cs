using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using Entities.Models;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Query to get cost estimates based on scope (All, Mine, Shared)
    /// </summary>
    public sealed record GetCostEstimatesQuery(
        Guid TenantId,
        Guid ProjectId,
        ResourceScope Scope
    ) : IRequestQuery<List<CostEstimateListItem>>, IAuthorizableRequest
    {
        public string PermissionCode => Scope switch
        {
            ResourceScope.All => PermissionCodes.ProjectResourcesReadAll,
            ResourceScope.Mine => PermissionCodes.ProjectResourcesRead,
            ResourceScope.Shared => PermissionCodes.ProjectResourcesReadShared,
            _ => throw new ArgumentOutOfRangeException(nameof(Scope))
        };
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
    
    /// <summary>
    /// Result DTO for cost estimate list item
    /// </summary>
    public record CostEstimateListItem(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string ProjectName,
        Guid TemplateId,
        string TemplateName,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        decimal? TotalNet,
        decimal? TotalGross,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName
    );
}
