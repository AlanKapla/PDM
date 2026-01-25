using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;
using Entities.Models;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Command do aktualizacji kosztorysu z pełną hierarchią
    /// Zastępuje wszystkie grupy i pozycje nowymi danymi
    /// </summary>
    public sealed record UpdateCostEstimateCommand(
        string Name,
        string? Description,
        CostEstimateStatus Status,
        List<CostEstimateGroupDto> RootGroups
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid CostEstimateId { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
