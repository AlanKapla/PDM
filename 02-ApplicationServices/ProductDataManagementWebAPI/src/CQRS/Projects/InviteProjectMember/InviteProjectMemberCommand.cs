using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using MediatR;

namespace CQRS.Projects.InviteProjectMember;

public sealed record InviteProjectMemberCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public required string Email { get; init; }
    public bool IsAdmin { get; init; }
    public IReadOnlyList<ProjectModule> Modules { get; init; } = Array.Empty<ProjectModule>();

    public string PermissionCode => PermissionCodes.ProjectMembers;

    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
