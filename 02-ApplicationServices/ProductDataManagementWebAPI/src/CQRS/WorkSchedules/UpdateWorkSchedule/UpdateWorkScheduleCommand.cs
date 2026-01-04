using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.Interfaces;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public sealed record UpdateWorkScheduleCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid WorkScheduleId,
        string Name,
        List<UpdateStageDto>? Stages
    ) : IRequestCommand<WorkScheduleDetailsWeb>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

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
