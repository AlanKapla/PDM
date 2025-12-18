using Entities.Models.Base;

namespace Entities.Models
{
    public class WorkSchedule : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public Project Project { get; set; } = default!;
        public TenantMember CreatedBy { get; set; } = default!;
        public ICollection<WorkScheduleStage> Stages { get; set; } = new List<WorkScheduleStage>();
    }
}
