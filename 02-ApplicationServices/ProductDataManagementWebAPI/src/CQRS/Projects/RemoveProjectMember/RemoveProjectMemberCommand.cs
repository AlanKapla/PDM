using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.RemoveProjectMember
{
    public sealed record RemoveProjectMemberCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid UserId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
