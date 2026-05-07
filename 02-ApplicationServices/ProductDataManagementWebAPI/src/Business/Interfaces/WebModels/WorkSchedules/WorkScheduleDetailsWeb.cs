using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;

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
        DateTime? PlannedStartDate,
        DateTime? PlannedEndDate,
        List<WorkScheduleStageWorkPeriodWeb> Periods,
        List<WorkScheduleStageWorkAssigneeWeb> Assignees,
        List<WorkScheduleStageWorkCommentWeb> Comments
    );

    public record WorkScheduleStageWorkPeriodWeb(
        Guid Id,
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

    public record MyWorkSchedulesItemDto(
        Guid WorkScheduleId,
        string WorkScheduleName
    );

    public record MyWorkSchedulesProjectDto(
        Guid ProjectId,
        string ProjectName,
        List<MyWorkSchedulesItemDto> WorkSchedules
    );

    public record MyWorkSchedulesTenantDto(
        Guid TenantId,
        string TenantName,
        List<MyWorkSchedulesProjectDto> Projects
    );
}

