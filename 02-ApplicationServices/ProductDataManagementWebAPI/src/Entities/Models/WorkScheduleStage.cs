using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models
{
    public class WorkScheduleStage : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid WorkScheduleId { get; set; }
        public Guid? ParentStageId { get; set; }
        public Guid? CostEstimateGroupId { get; set; }
        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public WorkSchedule WorkSchedule { get; set; } = default!;
        public WorkScheduleStage? ParentStage { get; set; }
        public CostEstimateGroup? CostEstimateGroup { get; set; }
        public ICollection<WorkScheduleStage> ChildStages { get; set; } = new List<WorkScheduleStage>();
        public ICollection<WorkScheduleStageWork> Works { get; set; } = new List<WorkScheduleStageWork>();
    }
}
