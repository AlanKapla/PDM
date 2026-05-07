using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using Entities.Models.Tenants;

namespace Entities.Models.WorkSchedules
{
    public class WorkSchedule : DeletableEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByUserId { get; set; }

        public Guid? CostEstimateId { get; set; }

        public Project Project { get; set; } = default!;
        public TenantMember CreatedBy { get; set; } = default!;
        public virtual CostEstimate? CostEstimate { get; set; }
        public ICollection<WorkScheduleStage> Stages { get; set; } = new List<WorkScheduleStage>();
        public ICollection<WorkScheduleStageWorkDependency> Dependencies { get; set; } = new List<WorkScheduleStageWorkDependency>();
    }
}
