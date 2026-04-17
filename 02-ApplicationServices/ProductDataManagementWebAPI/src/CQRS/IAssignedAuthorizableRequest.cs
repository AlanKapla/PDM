namespace CQRS;

/// <summary>
/// Marker interface for requests that require authorization based on project membership
/// without requiring the user's ActiveTenantId to be set.
/// Used for cross-tenant operations such as "My work" views.
/// </summary>
public interface IAssignedAuthorizableRequest
{
    /// <summary>
    /// Permission required to execute the operation (e.g. PROJECT.RESOURCES.WRITE.OWN).
    /// </summary>
    string PermissionCode { get; }

    /// <summary>
    /// The project ID — sufficient for authorization without ActiveTenantId.
    /// The TenantId is resolved by AccessService from the database.
    /// </summary>
    Guid ProjectId { get; }
}
