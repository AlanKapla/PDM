namespace CQRS;

/// <summary>
/// Marker interface for requests that require the caller to be a SuperAdmin.
/// Handled by <see cref="Behaviours.SuperAdminBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface ISuperAdminRequest;
