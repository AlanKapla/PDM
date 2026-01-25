using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models;
using MediatR;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Command do tworzenia kosztorysu
    /// Jeśli RootGroups jest puste - tworzy pusty kosztorys
    /// Jeśli RootGroups zawiera dane - tworzy kosztorys z pełną hierarchią
    /// </summary>
    public sealed record CreateCostEstimateCommand(
        Guid TemplateId,
        Guid TemplateVersionId,
        Guid SelectedCurrencyId,  // Waluta wybrana z dostępnych w template
        string Name,
        string? Description // null lub pusta lista = pusty kosztorys
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
