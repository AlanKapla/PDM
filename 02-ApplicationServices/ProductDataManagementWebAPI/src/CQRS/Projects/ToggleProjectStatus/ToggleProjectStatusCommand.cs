using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Command do zmiany statusu aktywności projektu
/// </summary>
public sealed record ToggleProjectStatusCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required bool IsActive { get; init; }

    public string PermissionCode => PermissionCodes.ProjectStatusManage;

    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
