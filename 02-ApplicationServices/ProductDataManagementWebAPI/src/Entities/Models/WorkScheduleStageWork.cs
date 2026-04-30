using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.WorkItemLinks;

namespace Entities.Models
{
    public class WorkScheduleStageWork : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid WorkScheduleStageId { get; set; }

        /// <summary>
        /// ID pozycji kosztorysu, z której ten zakres pracy został zsynchronizowany.
        /// NULL gdy zakres pracy jest ręcznie utworzony przez użytkownika.
        /// </summary>
        public Guid? CostEstimateItemId { get; set; }

        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public string ColorRgb { get; set; } = default!;

        /// <summary>
        /// Denormalizacja z kolekcji <see cref="Periods"/> — aktualizowane przez handlery CQRS przy każdej zmianie periodów.
        /// </summary>
        public DateTime? PlannedStartDate { get; set; }

        /// <summary>
        /// Denormalizacja z kolekcji <see cref="Periods"/> — aktualizowane przez handlery CQRS przy każdej zmianie periodów.
        /// </summary>
        public DateTime? PlannedEndDate { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public WorkScheduleStage Stage { get; set; } = default!;
        public CostEstimateItem? CostEstimateItem { get; set; }
        public virtual ICollection<CostEstimateItemWorkScheduleStageWorkLink> WorkItemLinks { get; set; } = new List<CostEstimateItemWorkScheduleStageWorkLink>();
        public ICollection<WorkScheduleStageWorkPeriod> Periods { get; set; } = new List<WorkScheduleStageWorkPeriod>();
        public ICollection<WorkScheduleStageWorkAssignment> Assignments { get; set; } = new List<WorkScheduleStageWorkAssignment>();
        public ICollection<WorkScheduleStageWorkComment> Comments { get; set; } = new List<WorkScheduleStageWorkComment>();

        /// <summary>Dependencies where this work is the predecessor (this work → others)</summary>
        public ICollection<WorkScheduleStageWorkDependency> PredecessorDependencies { get; set; } = new List<WorkScheduleStageWorkDependency>();

        /// <summary>Dependencies where this work is the successor (others → this work)</summary>
        public ICollection<WorkScheduleStageWorkDependency> SuccessorDependencies { get; set; } = new List<WorkScheduleStageWorkDependency>();
    }
}
