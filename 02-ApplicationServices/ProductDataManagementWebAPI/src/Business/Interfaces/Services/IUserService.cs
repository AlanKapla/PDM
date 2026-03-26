namespace Business.Interfaces.Services
{
    public sealed class ProjectMemberUserInfo
    {
        public Guid UserId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string AzureAdB2CObjectId { get; init; } = string.Empty;
        public string? RoleCode { get; init; }
        public DateTime JoinedAt { get; init; }
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public interface IUserService
    {
        /// <summary>
        /// Returns all active project members with their user data. Cached per project (TenantId + ProjectId).
        /// </summary>
        Task<List<ProjectMemberUserInfo>> GetProjectMembersAsync(
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single project member by userId. Uses the cached list — no extra DB query.
        /// Returns null if the user is not a member of the project.
        /// </summary>
        Task<ProjectMemberUserInfo?> GetProjectMemberAsync(
            Guid tenantId,
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the project members cache for the given tenant and project.
        /// </summary>
        Task InvalidateProjectMembersCacheAsync(
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns basic user info for a tenant member who may not yet be a project member.
        /// Not cached. Use for operations like adding a user to a project.
        /// </summary>
        Task<ProjectMemberUserInfo?> GetTenantMemberInfoAsync(
            Guid tenantId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a dictionary of project members filtered by the provided user IDs.
        /// Uses the cached members list — no extra DB query.
        /// </summary>
        Task<Dictionary<Guid, ProjectMemberUserInfo>> GetProjectMembersByIdsAsync(
            Guid tenantId,
            Guid projectId,
            HashSet<Guid> userIds,
            CancellationToken cancellationToken = default);
    }
}
