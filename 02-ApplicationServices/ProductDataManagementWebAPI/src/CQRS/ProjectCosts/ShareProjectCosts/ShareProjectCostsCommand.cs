using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.ProjectCosts.ShareProjectCosts
{
    /// <summary>
    /// Command do udostępnienia wielu kosztów wybranym członkom projektu
    /// </summary>
    public record ShareProjectCostsCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public List<Guid> ProjectCostIds { get; init; } = new();
        
        /// <summary>
        /// Lista ID użytkowników (członków projektu), którym zostaną udostępnione koszty
        /// </summary>
        public List<Guid> SharedWithUserIds { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
