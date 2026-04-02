using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models
{
    public class WorkScheduleStageWork : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid WorkScheduleStageId { get; set; }

        /// <summary>
        /// ID pozycji kosztorysu, z której ten zakres pracy został zsynchronizowany.
        /// NULL gdy zakres pracy jest ręcznie utworzony przez użytkownika.
        /// </summary>
        public Guid? CostEstimateItemId { get; set; }

        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public string ColorRgb { get; set; } = default!;
        public bool IsClosed { get; set; } = false;

        public WorkScheduleStage Stage { get; set; } = default!;
        public CostEstimateItem? CostEstimateItem { get; set; }
        public ICollection<WorkScheduleStageWorkPeriod> Periods { get; set; } = new List<WorkScheduleStageWorkPeriod>();
        public ICollection<WorkScheduleStageWorkAssignment> Assignments { get; set; } = new List<WorkScheduleStageWorkAssignment>();
        public ICollection<WorkScheduleStageWorkComment> Comments { get; set; } = new List<WorkScheduleStageWorkComment>();
    }
}
