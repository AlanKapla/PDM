using Business.Interfaces.Model;

namespace CQRS.WorkSchedules.Shared
{
    /// <summary>
    /// Base record for every Command/Query in the WorkSchedule domain.
    /// Provides TenantId, ProjectId and the boilerplate IAuthorizableRequest implementation.
    /// </summary>
    public abstract record WorkScheduleRequestBase : IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public abstract string PermissionCode { get; }

        public virtual ResourceRef GetResource() =>
            new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
    }

    /// <summary>
    /// Base record for Commands/Queries that operate on a single existing work schedule.
    /// Adds WorkScheduleId to <see cref="WorkScheduleRequestBase"/>.
    /// </summary>
    public abstract record WorkScheduleCommandBase : WorkScheduleRequestBase
    {
        public Guid WorkScheduleId { get; init; }
    }

    /// <summary>
    /// Base record for Commands that operate on a stage within a work schedule.
    /// Adds WorkScheduleStageId to <see cref="WorkScheduleCommandBase"/>.
    /// </summary>
    public abstract record WorkScheduleStageCommandBase : WorkScheduleCommandBase
    {
        public Guid WorkScheduleStageId { get; init; }
    }

    /// <summary>
    /// Base record for Commands that operate on a stage work item within a work schedule.
    /// Adds WorkScheduleStageWorkId to <see cref="WorkScheduleCommandBase"/>.
    /// </summary>
    public abstract record WorkScheduleStageWorkCommandBase : WorkScheduleCommandBase
    {
        public Guid WorkScheduleStageWorkId { get; init; }
    }

    /// <summary>
    /// Base record for Commands authorized via project membership rather than the user's
    /// active tenant. Mirrors <see cref="WorkScheduleCommandBase"/> but implements
    /// <see cref="IAssignedAuthorizableRequest"/>.
    /// </summary>
    public abstract record WorkScheduleAssignedCommandBase : IAssignedAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid WorkScheduleId { get; init; }

        public abstract string PermissionCode { get; }
    }

    /// <summary>
    /// Assigned-auth variant for Commands that target a single stage work item.
    /// Adds WorkScheduleStageWorkId to <see cref="WorkScheduleAssignedCommandBase"/>.
    /// </summary>
    public abstract record WorkScheduleStageWorkAssignedCommandBase : WorkScheduleAssignedCommandBase
    {
        public Guid WorkScheduleStageWorkId { get; init; }
    }
}
