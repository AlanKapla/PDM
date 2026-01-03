using Business.Implementation.Model;
using Entities.Enums;

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
        Task<TenantCtxSnapshot?> GetActiveTenantSnapshotAsync(CancellationToken cancellationToken = default);
        Task<ProjectCtxSnapshot?> GetProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken = default);
    }
}
