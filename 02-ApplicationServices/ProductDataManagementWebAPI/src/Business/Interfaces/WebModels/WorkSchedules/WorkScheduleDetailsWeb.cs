namespace Business.Interfaces.WebModels.WorkSchedules
{
    public record WorkScheduleDetailsWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string Name,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName,
        List<WorkScheduleStageWeb> Stages
    );

    public record WorkScheduleStageWeb(
        Guid Id,
        string Name,
        int Order,
        List<WorkScheduleStageWorkWeb> Works
    );

    public record WorkScheduleStageWorkWeb(
        Guid Id,
        string Name,
        int Order,
        string ColorRgb,
        bool IsClosed,
        List<WorkScheduleStageWorkPeriodWeb> Periods,
        List<WorkScheduleStageWorkAssigneeWeb> Assignees
    );

    public record WorkScheduleStageWorkPeriodWeb(
        DateTime StartDate,
        DateTime EndDate
    );

    public record WorkScheduleStageWorkAssigneeWeb(
        Guid UserId,
        string UserName
    );
}
