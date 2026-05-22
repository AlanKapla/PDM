using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.ProjectCosts.ShareProjectCosts
{
    /// <summary>
    /// Command do udostępnienia wielu kosztów wybranym członkom projektu
    /// </summary>
    public sealed record ShareProjectCostsCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required List<Guid> ProjectCostIds { get; init; }

        /// <summary>
        /// Lista ID użytkowników (członków projektu), którym zostaną udostępnione koszty
        /// </summary>
        public required List<Guid> SharedWithUserIds { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesShare;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
