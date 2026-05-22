using Business.Interfaces.Model;

namespace CQRS.CostTrackers.Shared
{
    /// <summary>
    /// Base record for every Command/Query in the CostTracker domain.
    /// Provides TenantId, ProjectId and the boilerplate IAuthorizableRequest implementation.
    /// </summary>
    public abstract record CostTrackerCommandBase : IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public abstract string PermissionCode { get; }

        public virtual ResourceRef GetResource() =>
            new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
    }
}
