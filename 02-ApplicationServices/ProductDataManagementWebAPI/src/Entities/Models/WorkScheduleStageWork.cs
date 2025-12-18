using Entities.Models.Base;

namespace Entities.Models
{
    public class WorkScheduleStageWork : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkScheduleStageId { get; set; }
        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public string ColorRgb { get; set; } = default!;
        public bool IsClosed { get; set; } = false;

        public WorkScheduleStage Stage { get; set; } = default!;
        public ICollection<WorkScheduleStageWorkPeriod> Periods { get; set; } = new List<WorkScheduleStageWorkPeriod>();
        public ICollection<WorkScheduleStageWorkAssignment> Assignments { get; set; } = new List<WorkScheduleStageWorkAssignment>();
    }
}
