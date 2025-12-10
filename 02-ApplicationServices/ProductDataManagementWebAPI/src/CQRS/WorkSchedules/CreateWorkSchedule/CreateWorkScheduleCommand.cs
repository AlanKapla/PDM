using Business.Interfaces.WebModels.WorkSchedules;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public record CreateWorkScheduleCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name,
        List<CreateStageDto> Stages
    ) : IRequestCommand<WorkScheduleDetailsWeb>;

    public record CreateStageDto(
        string Name,
        int Order,
        List<CreateWorkDto> Works
    );

    public record CreateWorkDto(
        string Name,
        int Order,
        string ColorRgb,
        List<CreateWorkPeriodDto> Periods,
        List<Guid> AssignedUserIds
    );

    public record CreateWorkPeriodDto(
        DateTime StartDate,
        DateTime EndDate
    );
}
