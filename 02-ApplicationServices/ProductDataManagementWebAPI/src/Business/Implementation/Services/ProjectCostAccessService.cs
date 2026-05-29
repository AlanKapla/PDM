using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Costs;

namespace Business.Implementation.Services
{
    public sealed class ProjectCostAccessService : IProjectCostAccessService
    {
        private readonly ICurrentUser currentUser;

        public ProjectCostAccessService(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;
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
    }
}
