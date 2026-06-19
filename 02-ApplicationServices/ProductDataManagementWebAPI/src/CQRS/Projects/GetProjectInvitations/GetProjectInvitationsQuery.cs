using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using MediatR;

namespace CQRS.Projects.GetProjectInvitations;

public sealed record GetProjectInvitationsQuery : IRequestQuery<IEnumerable<ProjectInvitationWeb>>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }

    public string PermissionCode => PermissionCodes.ProjectMembers;

    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
