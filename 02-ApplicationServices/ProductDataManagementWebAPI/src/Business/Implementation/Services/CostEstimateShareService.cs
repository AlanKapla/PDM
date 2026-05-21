using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;

namespace Business.Implementation.Services
{
    /// <inheritdoc />
    public sealed class CostEstimateShareService : ICostEstimateShareService
    {
        private readonly ICurrentUser currentUser;
        private readonly ICostEstimateAccessService ceAccessService;

        public CostEstimateShareService(
            ICurrentUser currentUser,
            ICostEstimateAccessService ceAccessService)
        {
            this.currentUser = currentUser;
            this.ceAccessService = ceAccessService;
        }

        public async Task ValidateOwnerOrAdminAsync(
            CostEstimate costEstimate,
            CancellationToken cancellationToken)
        {
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(
                costEstimate.TenantId, costEstimate.ProjectId, cancellationToken);

            if (costEstimate.OwnerId != currentUser.Id && !isAdmin)
            {
                throw new ForbiddenApiException(
                    "Only the owner or an admin can manage shares for this cost estimate.");
            }
        }

        public async Task InvalidateAccessCacheAsync(
            Guid costEstimateId,
            Guid projectId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            await ceAccessService.InvalidateCostEstimateAccessCacheAsync(
                tenantId, projectId, costEstimateId, cancellationToken);

            await ceAccessService.InvalidateAccessCacheAsync(
                tenantId, projectId, cancellationToken);
        }
    }
}
