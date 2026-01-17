using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.AddProjectMember
{
    public record AddProjectMemberCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid UserId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectMembersManage;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
