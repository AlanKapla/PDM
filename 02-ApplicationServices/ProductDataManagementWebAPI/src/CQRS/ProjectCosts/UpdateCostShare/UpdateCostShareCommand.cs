using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.ProjectCosts.UpdateCostShare
{
    /// <summary>
    /// Command to update cost sharing - add or remove access for specific users
    /// </summary>
    public sealed record UpdateCostShareCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostId { get; init; }

        /// <summary>
        /// Lista ID użytkowników, którzy powinni mieć dostęp do kosztu
        /// Użytkownicy nie na liście zostaną usunięci z udostępnienia
        /// </summary>
        public required List<Guid> SharedWithUserIds { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
