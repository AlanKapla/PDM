using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace Business.Interfaces.Services;

public interface IAccessService
{
    Task<bool> AuthorizeAsync(
        ICurrentUser user,
        string permissionCode,
        ResourceRef resource,
        ResourceScope? resourceScope = null,
        CancellationToken cancellationToken = default);

    Task<bool> AuthorizeAssignedAsync(
        ICurrentUser user,
        string permissionCode,
        Guid projectId,
        CancellationToken cancellationToken = default);
}
