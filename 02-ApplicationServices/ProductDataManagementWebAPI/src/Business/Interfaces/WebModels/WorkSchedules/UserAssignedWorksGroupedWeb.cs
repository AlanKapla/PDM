namespace Business.Interfaces.WebModels.WorkSchedules
{
    /// <summary>
    /// User's assigned works grouped by Project
    /// </summary>
    public record UserAssignedWorksGroupedWeb(
        Guid ProjectId,
        string ProjectName,
        List<UserAssignedWorkScheduleWeb> WorkSchedules
    );

    /// <summary>
    /// Work schedule with assigned works grouped by Stage
    /// </summary>
    public record UserAssignedWorkScheduleWeb(
        Guid WorkScheduleId,
        string WorkScheduleName,
        DateTime WorkScheduleCreatedAt,
        List<UserAssignedStageWeb> Stages
    );

    /// <summary>
    /// Stage with user's assigned works
    /// </summary>
    public record UserAssignedStageWeb(
        Guid StageId,
        string StageName,
        int StageOrder,
        List<UserAssignedWorkWeb> Works
    );

    /// <summary>
    /// Individual work assigned to the user with periods
    /// </summary>
    public record UserAssignedWorkWeb(
        Guid WorkId,
        string WorkName,
        int WorkOrder,
        string ColorRgb,
        bool IsClosed,
        List<WorkScheduleStageWorkPeriodWeb> Periods,
        List<WorkScheduleStageWorkCommentWeb> Comments
    );
}
