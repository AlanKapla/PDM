using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment
{
    public sealed record UpdateWorkScheduleStageWorkCommentCommand(string Content)
        : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }
        public Guid CommentId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
