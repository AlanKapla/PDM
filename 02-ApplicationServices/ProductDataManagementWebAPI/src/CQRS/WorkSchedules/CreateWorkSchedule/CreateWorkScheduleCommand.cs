using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public sealed record CreateWorkScheduleCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name,
        List<CreateStageDto>? Stages
    ) : IRequestCommand<WorkScheduleDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    public record CreateStageDto(
        string Name,
        int Order,
        List<CreateWorkDto>? Works
    );

    public record CreateWorkDto(
        string Name,
        int Order,
        string ColorRgb,
        List<CreateWorkPeriodDto>? Periods,
        List<Guid>? AssignedUserIds,
        List<CreateWorkCommentDto>? Comments
    );

    public record CreateWorkPeriodDto(
        DateTime StartDate,
        DateTime EndDate,
        bool IsClosed
    );

    public record CreateWorkCommentDto(
        string Content
    );
}
