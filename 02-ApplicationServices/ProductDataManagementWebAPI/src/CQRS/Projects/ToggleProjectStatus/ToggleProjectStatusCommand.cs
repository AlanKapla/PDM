using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Command do zmiany statusu aktywności projektu
/// </summary>
public record ToggleProjectStatusCommand(Guid TenantId, Guid ProjectId, bool IsActive) : IRequestCommand<Unit>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectStatusManage;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
