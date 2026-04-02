using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models
{
    public class WorkSchedule : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? CostEstimateId { get; set; }
        public string Name { get; set; } = default!;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public Project Project { get; set; } = default!;
        public TenantMember CreatedBy { get; set; } = default!;
        public CostEstimate? CostEstimate { get; set; }
        public ICollection<WorkScheduleStage> Stages { get; set; } = new List<WorkScheduleStage>();
    }
}
