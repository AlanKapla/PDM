using Business.Interfaces.WebModels.WorkSchedules;
using CQRS;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public record UpdateWorkScheduleCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid WorkScheduleId,
        string Name,
        List<UpdateStageDto>? Stages
    ) : IRequestCommand<WorkScheduleDetailsWeb>;

    public record UpdateStageDto(
        Guid? Id,
        string Name,
        int Order,
        List<UpdateWorkDto>? Works
    );

    public record UpdateWorkDto(
        Guid? Id,
        string Name,
        int Order,
        string ColorRgb,
        bool IsClosed,
        List<UpdateWorkPeriodDto>? Periods,
        List<Guid>? AssignedUserIds,
        List<UpdateWorkCommentDto>? Comments
    );

    public record UpdateWorkPeriodDto(
        Guid? Id,
        DateTime StartDate,
        DateTime EndDate,
        bool IsClosed
    );

    public record UpdateWorkCommentDto(
        Guid? Id,
        string Content
    );
}
