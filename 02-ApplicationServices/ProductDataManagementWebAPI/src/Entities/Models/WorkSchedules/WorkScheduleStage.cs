using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.WorkSchedules
{
    public class WorkScheduleStage : DeletableEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid WorkScheduleId { get; set; }
        public Guid? ParentStageId { get; set; }
        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Guid? CostEstimateGroupId { get; set; }

        public WorkSchedule WorkSchedule { get; set; } = default!;
        public WorkScheduleStage? ParentStage { get; set; }
        public virtual CostEstimateGroup? CostEstimateGroup { get; set; }
        public ICollection<WorkScheduleStage> ChildStages { get; set; } = new List<WorkScheduleStage>();
        public ICollection<WorkScheduleStageWork> Works { get; set; } = new List<WorkScheduleStageWork>();
    }
}
