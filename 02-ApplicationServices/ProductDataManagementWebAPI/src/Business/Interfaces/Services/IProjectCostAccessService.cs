using Entities.Models.Costs;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Resolves write/share access checks for <see cref="ProjectCost"/> resources.
    /// </summary>
    public interface IProjectCostAccessService
    {
        /// <summary>
        /// True when the current user is tenant/project admin or owner of the cost
        /// (full edit, delete and share management).
        /// </summary>
        Task<bool> HasWriteAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken);

        /// <summary>
        /// True when the current user has at least limited write access to the cost
        /// (admin, owner, or shared with). Used to gate operations available to recipients
        /// of a shared cost (e.g. toggling acceptance).
        /// </summary>
        Task<bool> HasShareAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken);
    }
}
