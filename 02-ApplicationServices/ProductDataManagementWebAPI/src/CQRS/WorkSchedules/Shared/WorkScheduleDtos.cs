using Entities.Models;

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
    /// Shared DTO for work schedule work (used in both Create and Update).
    /// TempId is a client-assigned temporary identifier for new works (no DB Id yet),
    /// used to reference the work in dependency definitions within the same request.
    /// </summary>
    public record WorkScheduleWorkDto(
        Guid? Id,
        Guid? TempId,
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

    /// <summary>
    /// Dependency between two work items within the same work schedule.
    /// Use DbId when referencing an existing work item already persisted to the database.
    /// Use TempId when referencing a new work item being created in the same request.
    /// If both are provided, DbId takes precedence.
    /// </summary>
    public record WorkScheduleWorkDependencyDto(
        Guid? PredecessorDbId,
        Guid? PredecessorTempId,
        Guid? SuccessorDbId,
        Guid? SuccessorTempId,
        WorkDependencyType DependencyType,
        int LagDays
    );

    /// <summary>
    /// DTO for a single work period used in SetWorkScheduleStageWorkPeriods (replace-all operation).
    /// </summary>
    public record WorkPeriodDto(
        DateTime StartDate,
        DateTime EndDate,
        bool IsClosed
    );

    /// <summary>
    /// DTO for a dependency used in SetWorkScheduleDependencies (replace-all operation).
    /// Uses direct work IDs (no TempId) since all works already exist in the database.
    /// </summary>
    public record WorkDependencyDto(
        Guid PredecessorWorkId,
        Guid SuccessorWorkId,
        WorkDependencyType DependencyType,
        int LagDays
    );
}
