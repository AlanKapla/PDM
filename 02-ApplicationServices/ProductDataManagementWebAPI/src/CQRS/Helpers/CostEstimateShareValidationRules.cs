using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using Repositories.Repository.Interfaces;

namespace CQRS.Helpers
{
    /// <summary>
    /// Shared validation rules for cost estimate share commands.
    /// </summary>
    internal static class CostEstimateShareValidationRules
    {
        public static Task<bool> CostEstimateMustExistAsync(
            IReadRepository<CostEstimate> repository,
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken ct)
        {
            return repository.AnyAsync(
                c => c.Id == costEstimateId &&
                     c.TenantId == tenantId &&
                     c.ProjectId == projectId, ct);
        }

        public static async Task<bool> AllUsersMustBeProjectMembersAsync(
            IRepository<ProjectMember> repository,
            Guid tenantId,
            Guid projectId,
            IReadOnlyCollection<Guid> userIds,
            CancellationToken ct)
        {
            HashSet<Guid> requestedIds = userIds.ToHashSet();

            int matchCount = await repository.CountAsync(
                pm => pm.ProjectId == projectId &&
                      pm.TenantId == tenantId &&
                      requestedIds.Contains(pm.UserId), ct);

            return matchCount == requestedIds.Count;
        }
    }
}

