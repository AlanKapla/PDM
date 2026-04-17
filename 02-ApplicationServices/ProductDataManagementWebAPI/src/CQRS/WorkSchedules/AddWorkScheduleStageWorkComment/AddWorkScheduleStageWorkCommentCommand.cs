using Business.Interfaces.Constants;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWorkComment
{
    public sealed record AddWorkScheduleStageWorkCommentCommand(string Content)
        : IRequestCommand<Guid>, IAssignedAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }
        public Guid WorkScheduleStageWorkId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWriteOwn;
    }
}
