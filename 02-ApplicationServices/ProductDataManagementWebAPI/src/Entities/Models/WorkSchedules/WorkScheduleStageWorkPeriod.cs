using Entities.Models.Base;

namespace Entities.Models.WorkSchedules
{
    public class WorkScheduleStageWorkPeriod : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid WorkScheduleStageWorkId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsClosed { get; set; } = false;

        public WorkScheduleStageWork Work { get; set; } = default!;
    }
}
