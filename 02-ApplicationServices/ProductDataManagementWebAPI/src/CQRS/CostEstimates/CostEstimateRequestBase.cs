using Business.Interfaces.Model;

namespace CQRS.CostEstimates
{
    /// <summary>
    /// Base record for every Command/Query in the CostEstimate domain.
    /// Provides TenantId, ProjectId and the boilerplate IAuthorizableRequest implementation.
    /// </summary>
    public abstract record CostEstimateRequestBase : IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public abstract string PermissionCode { get; }

        public virtual ResourceRef GetResource() =>
            new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
    }

    /// <summary>
    /// Base record for Commands/Queries that operate on a single existing cost estimate.
    /// Adds CostEstimateId to <see cref="CostEstimateRequestBase"/>.
    /// </summary>
    public abstract record CostEstimateCommandBase : CostEstimateRequestBase
    {
        public Guid CostEstimateId { get; init; }
    }
}
