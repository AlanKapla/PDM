using Entities.Models.Costs;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Resolves write access checks for <see cref="ProjectCost"/> resources.
    /// </summary>
    public interface IProjectCostAccessService
    {
        /// <summary>
        /// True when the current user is tenant/project admin or owner of the cost
        /// (full edit and delete).
        /// </summary>
        Task<bool> HasWriteAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken);
    }
}
