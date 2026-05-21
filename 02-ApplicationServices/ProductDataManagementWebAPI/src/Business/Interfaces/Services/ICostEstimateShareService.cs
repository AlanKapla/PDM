using Entities.Models.CostEstimates;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Service consolidating share-related cross-cutting logic for CostEstimate
    /// (owner/admin validation + access cache invalidation).
    /// Used by Share and UpdateShares command handlers to avoid duplication.
    /// </summary>
    public interface ICostEstimateShareService
    {
        /// <summary>
        /// Throws <see cref="Business.Interfaces.Exceptions.ForbiddenApiException"/>
        /// when the current user is neither the owner of the cost estimate nor a tenant/project admin.
        /// </summary>
        Task ValidateOwnerOrAdminAsync(
            CostEstimate costEstimate,
            CancellationToken cancellationToken);

        /// <summary>
        /// Invalidates both the per-cost-estimate access cache and the per-project access cache.
        /// Must be called after any add/remove of shares.
        /// </summary>
        Task InvalidateAccessCacheAsync(
            Guid costEstimateId,
            Guid projectId,
            Guid tenantId,
            CancellationToken cancellationToken);
    }
}
