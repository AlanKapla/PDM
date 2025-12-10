using Entities.Models.Base;

namespace Entities.Models
{
    public class WorkScheduleStage : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkScheduleId { get; set; }
        public string Name { get; set; } = default!;
        public int Order { get; set; }

        public WorkSchedule WorkSchedule { get; set; } = default!;
        public ICollection<WorkScheduleStageWork> Works { get; set; } = new List<WorkScheduleStageWork>();
    }
}
