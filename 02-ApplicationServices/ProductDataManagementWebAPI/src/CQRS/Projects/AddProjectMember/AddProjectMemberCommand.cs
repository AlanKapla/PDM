using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using MediatR;

namespace CQRS.Projects.AddProjectMember
{
    public sealed record AddProjectMemberCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid UserId { get; init; }
        public IReadOnlyList<ProjectModule> Modules { get; init; } = Array.Empty<ProjectModule>();

        public string PermissionCode => PermissionCodes.ProjectMembers;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
