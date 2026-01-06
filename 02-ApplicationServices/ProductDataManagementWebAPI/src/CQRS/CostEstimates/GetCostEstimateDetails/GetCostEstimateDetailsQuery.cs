using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using Entities.Models.CostEstimateData;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Query do pobrania szczegółów kosztorysu
    /// </summary>
    public sealed record GetCostEstimateDetailsQuery(
        Guid CostEstimateId
    ) : IRequestQuery<CostEstimateDetails>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesReadSingle;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
    
    /// <summary>
    /// Result DTO for cost estimate details
    /// </summary>
    public record CostEstimateDetails(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string ProjectName,
        Guid TemplateId,
        string TemplateName,
        string Name,
        string? Description,
        CostEstimateStatus Status,
        CostEstimateDataModel Data,
        decimal? TotalNet,
        decimal? TotalGross,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastCalculatedAt,
        Guid OwnerId,
        string OwnerName
    );
}
