using Entities.Models;

namespace Business.Interfaces.WebModels.WorkSchedules
{
    public record WorkScheduleDetailsWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        Guid? CostEstimateId,
        string Name,
        DateTime CreatedAt,
        Guid CreatedByUserId,
        string CreatedByUserName,
        List<WorkScheduleStageWeb> Stages,
        List<WorkScheduleWorkDependencyWeb> Dependencies
    );

    public record WorkScheduleStageWeb(
        Guid Id,
        string Name,
        int Order,
        Guid? ParentStageId,
        Guid? CostEstimateGroupId,
        List<WorkScheduleStageWorkWeb> Works,
        List<WorkScheduleStageWeb> ChildStages
    );

    public record WorkScheduleStageWorkWeb(
        Guid Id,
        string Name,
        int Order,
        string ColorRgb,
        bool IsClosed,
        List<WorkScheduleStageWorkPeriodWeb> Periods,
        List<WorkScheduleStageWorkAssigneeWeb> Assignees,
        List<WorkScheduleStageWorkCommentWeb> Comments
    );

    public record WorkScheduleStageWorkPeriodWeb(
        DateTime StartDate,
        DateTime EndDate,
        bool IsClosed
    );

    public record WorkScheduleStageWorkAssigneeWeb(
        Guid UserId,
        string UserName
    );

    public record WorkScheduleStageWorkCommentWeb(
        Guid Id,
        string Content,
        Guid CreatedByUserId,
        string CreatedByUserName,
        DateTime CreatedAt
    );

    public record WorkScheduleWorkDependencyWeb(
        Guid Id,
        Guid PredecessorWorkId,
        Guid SuccessorWorkId,
        WorkDependencyType DependencyType,
        int LagDays
    );
}

