using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Command do tworzenia kosztorysu
    /// Jeśli RootGroups jest puste - tworzy pusty kosztorys
    /// Jeśli RootGroups zawiera dane - tworzy kosztorys z pełną hierarchią
    /// </summary>
    public sealed record CreateCostEstimateCommand(
        Guid TemplateId,
        string Name,
        string? Description 
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
