using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using Entities.Models.CostEstimateData;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Command do tworzenia pustego kosztorysu na podstawie szablonu
    /// Dane będą wypełniane później przez Update
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
