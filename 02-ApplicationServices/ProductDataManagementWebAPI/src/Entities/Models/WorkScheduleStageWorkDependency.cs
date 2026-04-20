using Entities.Models.Base;

namespace Entities.Models
{
    /// <summary>
    /// Wymagany unikalny indeks: (WorkScheduleId, PredecessorWorkId, SuccessorWorkId) — do skonfigurowania w OnModelCreating.
    /// </summary>
    public class WorkScheduleStageWorkDependency : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid WorkScheduleId { get; set; }
        public Guid PredecessorWorkId { get; set; }
        public Guid SuccessorWorkId { get; set; }
        public WorkDependencyType DependencyType { get; set; } = WorkDependencyType.FinishToStart;

        /// <summary>
        /// Lag (positive) or lead (negative) time in days applied between predecessor and successor.
        /// </summary>
        public int LagDays { get; set; } = 0;

        public WorkSchedule WorkSchedule { get; set; } = default!;
        public WorkScheduleStageWork PredecessorWork { get; set; } = default!;
        public WorkScheduleStageWork SuccessorWork { get; set; } = default!;
    }
}
