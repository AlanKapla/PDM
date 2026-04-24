using Entities.Models.Base;
using Entities.Models.WorkItemLinks;

namespace Entities.Models
{
    public class WorkSchedule : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public Project Project { get; set; } = default!;
        public TenantMember CreatedBy { get; set; } = default!;
        public ICollection<WorkScheduleStage> Stages { get; set; } = new List<WorkScheduleStage>();
        public ICollection<WorkScheduleStageWorkDependency> Dependencies { get; set; } = new List<WorkScheduleStageWorkDependency>();
        public virtual ICollection<CostEstimateWorkScheduleLink> CostEstimateLinks { get; set; } = new List<CostEstimateWorkScheduleLink>();
    }
}
