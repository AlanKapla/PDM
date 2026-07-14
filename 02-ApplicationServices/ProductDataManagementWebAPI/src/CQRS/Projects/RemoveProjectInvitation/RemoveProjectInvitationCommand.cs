using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.RemoveProjectInvitation;

public sealed record RemoveProjectInvitationCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid InvitationId { get; init; }

    public string PermissionCode => PermissionCodes.ProjectMembers;

    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
