namespace Business.Interfaces.Model
{
    public interface ICurrentUser
    {
        Guid Id { get; }
        string AzureAdB2CObjectId { get; }
        string FirstName { get; }
        string LastName { get; }
        string Email { get; }
        Guid? ActiveTenantId { get; }
        bool IsAuthenticated { get; }
        bool IsSuperAdmin { get; }
        
        string? GetClaimValue(string claimType);
        
        Task<int> GetPermissionsVersionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets tenant context snapshot for a specific tenant.
        /// Supports cross-tenant access for Tenant Admins and SuperAdmins.
        /// </summary>
        Task<TenantCtxSnapshot?> GetTenantSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets tenant context snapshot for user's active tenant.
        /// Convenience method that calls GetTenantSnapshotAsync(ActiveTenantId).
        /// </summary>
        Task<TenantCtxSnapshot?> GetActiveTenantSnapshotAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets project context snapshot for a specific project.
        /// </summary>
        Task<ProjectCtxSnapshot?> GetProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the current user is a Tenant Admin for the specified tenant.
        /// Returns false if user has no access to the tenant.
        /// </summary>
        Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the current user is a Project Admin for the specified project.
        /// Returns false if user has no access to the project.
        /// </summary>
        Task<bool> IsProjectAdminAsync(Guid projectId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the current user is either a Tenant Admin for the project's tenant OR a Project Admin for the project.
        /// Returns false if user has no access to the project.
        /// </summary>
        Task<bool> IsTenantOrProjectAdminAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default);
    }
}
