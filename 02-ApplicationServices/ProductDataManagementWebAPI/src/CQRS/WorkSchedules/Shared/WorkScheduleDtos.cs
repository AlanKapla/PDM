namespace CQRS.WorkSchedules.Shared
{
    /// <summary>
    /// Shared DTO for work schedule stage (used in both Create and Update)
    /// </summary>
    public record WorkScheduleStageDto(
        Guid? Id,
        string Name,
        int Order,
        List<WorkScheduleWorkDto>? Works,
        List<WorkScheduleStageDto>? Children
    );

    /// <summary>
    /// Shared DTO for work schedule work (used in both Create and Update)
    /// </summary>
    public record WorkScheduleWorkDto(
        Guid? Id,
        string Name,
        int Order,
        string ColorRgb,
        bool IsClosed,
        List<WorkScheduleWorkPeriodDto>? Periods,
        List<Guid>? AssignedUserIds,
        List<WorkScheduleWorkCommentDto>? Comments
    );

    /// <summary>
    /// Shared DTO for work schedule work period (used in both Create and Update)
    /// </summary>
    public record WorkScheduleWorkPeriodDto(
        Guid? Id,
        DateTime StartDate,
        DateTime EndDate,
        bool IsClosed
    );

    /// <summary>
    /// Shared DTO for work schedule work comment (used in both Create and Update)
    /// </summary>
    public record WorkScheduleWorkCommentDto(
        Guid? Id,
        string Content
    );
}
