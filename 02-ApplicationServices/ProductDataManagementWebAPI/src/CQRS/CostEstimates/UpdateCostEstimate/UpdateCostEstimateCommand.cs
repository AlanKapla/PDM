using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models;
using Entities.Models.CostEstimateData;
using MediatR;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Command do aktualizacji wypełnionego kosztorysu
    /// </summary>
    public sealed record UpdateCostEstimateCommand(
        string Name,
        string? Description,
        CostEstimateStatus Status,
        CostEstimateDataModel Data,
        decimal? TotalNet,
        decimal? TotalGross
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid CostEstimateId { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
