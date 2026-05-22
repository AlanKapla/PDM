using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Costs;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class ProjectCostAccessService : IProjectCostAccessService
    {
        private readonly ICurrentUser currentUser;
        private readonly IReadRepository<SharedProjectCost> sharedProjectCostRepo;

        public ProjectCostAccessService(
            ICurrentUser currentUser,
            IReadRepository<SharedProjectCost> sharedProjectCostRepo)
        {
            this.currentUser = currentUser;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
        }

        public async Task<bool> HasWriteAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            if (cost.UserId == currentUserId)
            {
                return true;
            }

            return await currentUser.IsTenantOrProjectAdminAsync(
                cost.TenantId, cost.ProjectId, cancellationToken);
        }

        public async Task<bool> HasShareAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            if (await HasWriteAccessAsync(cost, currentUserId, cancellationToken))
            {
                return true;
            }

            SharedProjectCost? share = await sharedProjectCostRepo.GetFirstBySearch(
                spc => spc.ProjectCostId == cost.Id
                    && spc.SharedWithUserId == currentUserId,
                cancellationToken);

            return share is not null;
        }
    }
}
