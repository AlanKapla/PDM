using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.WorkItemLinks
{
    /// <summary>Łącznik między grupą kosztorysu a etapem harmonogramu.</summary>
    public class CostEstimateGroupWorkScheduleStageLink : BaseEntity
    {
        /// <summary>FK do nadrzędnego CostEstimateWorkScheduleLink. Wymagane.</summary>
        public Guid WorkScheduleLinkId { get; set; }

        /// <summary>FK do CostEstimateGroup. Nullable — link może istnieć bez grupy kosztorysu.</summary>
        public Guid? CostEstimateGroupId { get; set; }

        /// <summary>FK do WorkScheduleStage. Nullable — link może istnieć bez etapu harmonogramu.</summary>
        public Guid? WorkScheduleStageId { get; set; }

        public virtual CostEstimateWorkScheduleLink WorkScheduleLink { get; set; } = default!;
        public virtual CostEstimateGroup? CostEstimateGroup { get; set; }
        public virtual WorkScheduleStage? WorkScheduleStage { get; set; }

        /// <summary>Kolekcja łączników pozycji kosztorysu i zakresów pracy.</summary>
        public virtual ICollection<CostEstimateItemWorkScheduleStageWorkLink> WorkItemLinks { get; set; }
            = new List<CostEstimateItemWorkScheduleStageWorkLink>();
    }
}
